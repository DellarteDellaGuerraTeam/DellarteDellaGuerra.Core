using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;
using Bannerlord.Cannons.Api;
using Bannerlord.ExpandedTemplate.API;
using DellarteDellaGuerra.DisableNativeBehaviour.MissionBehaviours;
using DellarteDellaGuerra.DisplayCompilingShaders;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Domain.DisplayCompilingShaders;
using DellarteDellaGuerra.Domain.SiegeEngines;
using DellarteDellaGuerra.Firearm;
using DellarteDellaGuerra.Infrastructure.Cannon.Campaign;
using DellarteDellaGuerra.Infrastructure.Cannon.Mission.Battle;
using DellarteDellaGuerra.Infrastructure.Cannon.Mission.Siege.UI;
using DellarteDellaGuerra.Infrastructure.Configuration.Providers;
using DellarteDellaGuerra.Infrastructure.DI;
using DellarteDellaGuerra.Infrastructure.Events;
using DellarteDellaGuerra.Infrastructure.ExpandedTemplateApi.Logging;
using DellarteDellaGuerra.Infrastructure.SiegeEngines;
using DellarteDellaGuerra.Infrastructure.Utils;
using DellarteDellaGuerra.RemoveOrphanChildren.MissionBehaviours;
using DellarteDellaGuerra.Tournament.Api;
using DellarteDellaGuerra.Utils;
using Harmony.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.Core;
using TaleWorlds.DotNet;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using ILogger = DellarteDellaGuerra.Domain.Common.Logging.Port.ILogger;

namespace DellarteDellaGuerra.Infrastructure
{
    public class SubModule : MBSubModuleBase
    {
        private readonly ILogger _logger;
        private IServiceProvider _serviceProvider;

        public SubModule()
        {
            _serviceProvider = new DadgServiceContainer().Build();

            var loggerFactory = _serviceProvider.GetRequiredService<ILoggerFactory>();
            _logger = loggerFactory.CreateLogger<SubModule>();

            new BannerlordExpandedTemplateApi()
                .UseLoggerFactory(new ExpandedTemplateLoggerFactory(loggerFactory))
                .Bind();

            _serviceProvider.GetRequiredService<SiegeEngineIconRegistrationUseCase>()
                .RegisterSiegeEngineIcons();

            CannonSystemInitialiser.Initialise();
        }

        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            InfoPrinter.Display("DADG loaded");
        }

        protected override void OnSubModuleLoad()
        {
            _serviceProvider.GetRequiredService<OnSubModuleLoadEventPubSub>().Publish();

            _serviceProvider.GetRequiredService<IHarmonyPatcher>().ApplyPatches();

            Managed.AddTypes(GetDadgReferencedAssemblyTypes());
        }

        private Dictionary<string, Type> GetDadgReferencedAssemblyTypes()
        {
            return Assembly
                .GetExecutingAssembly()
                .GetReferencedAssemblies()
                .Where(a => a.Name.StartsWith(ModuleIdHelper.GetModuleIdPrefix()))
                .Select(Assembly.Load)
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type =>
                    type.IsClass &&
                    !type.IsAbstract &&
                    !type.IsGenericTypeDefinition &&
                    type.GetConstructor(Type.EmptyTypes) != null)
                .GroupBy(t => t.Name)
                .ToDictionary(
                    grouping => grouping.Key,
                    grouping => grouping.Last()
                );
        }

        protected override void OnSubModuleUnloaded()
        {
            LogManager.Shutdown();
            _serviceProvider.GetRequiredService<DadgConfigWatcher>().Destroy();
        }

        protected override void InitializeGameStarter(Game game, IGameStarter starterObject)
        {
            if (game.GameType is not Campaign || starterObject is not CampaignGameStarter campaignGameStarter) return;

            campaignGameStarter.AddModel(_serviceProvider.GetRequiredService<DadgTournamentModel>());

            var loggerFactory = _serviceProvider.GetRequiredService<ILoggerFactory>();
            var getDefaultSiegeEngine = _serviceProvider.GetRequiredService<GetDefaultSiegeEngine>();
            campaignGameStarter.AddModel(new DadgSiegeStrategyActionModel(
                campaignGameStarter.Models.OfType<DefaultSiegeStrategyActionModel>().Last(),
                MBObjectManager.Instance, loggerFactory, getDefaultSiegeEngine));
            campaignGameStarter.AddModel(new DadgSiegeEngineAvailabilityModel(
                campaignGameStarter.Models.OfType<SiegeEventModel>().Last(),
                loggerFactory, getDefaultSiegeEngine));
            campaignGameStarter.AddModel(new DadgSiegeEventModel(
                campaignGameStarter.Models.OfType<SiegeEventModel>().Last(),
                getDefaultSiegeEngine));

            CompilingShaderNotifier.Init(_serviceProvider.GetRequiredService<DisplayShaderNumber>());
            game.AddGameHandler<CompilingShaderNotifier>();

            campaignGameStarter.AddBehavior(new NobleOrphanChildrenCampaignBehaviour());
        }

        public override void OnGameInitializationFinished(Game game)
        {
            if (game.GameType is not Campaign) return;
            _serviceProvider.GetRequiredService<CampaignBehaviourDisabler>()
                .Disable(Campaign.Current.CampaignBehaviorManager);
            SetCampaignStartingDate();
            LoadDadgBattleScenes();
        }

        public override void OnBeforeMissionBehaviorInitialize(Mission mission)
        {
            base.OnBeforeMissionBehaviorInitialize(mission);
            mission.AddMissionBehavior(_serviceProvider.GetRequiredService<FirearmReloadMissionLogic>());
            mission.AddMissionBehavior(_serviceProvider.GetRequiredService<FirearmSmokeMissionLogic>());
            mission.AddMissionBehavior(_serviceProvider.GetRequiredService<CannonTeamMissionLogic>());
        }

        public override void RegisterSubModuleObjects(bool isSavedCmapaign)
        {
            _serviceProvider.GetRequiredService<FirearmSkill>().Initialise();
        }

        private void SetCampaignStartingDate()
        {
            CampaignTime startTime = CampaignTime.Years(1471) + CampaignTime.Weeks(4) + CampaignTime.Days(1);
            typeof(CampaignData).GetField("CampaignStartTime",BindingFlags.Static|BindingFlags.Public)?.SetValue(null,startTime);
        }

        private void LoadDadgBattleScenes()
        {
            var battleScenesFilePath = ResourceLocator.GetBattleScenesFilePath();
            if (battleScenesFilePath is null)
            {
                _logger.Warn($"Could not find DADG battle scenes at {battleScenesFilePath}. Using SandBox's battle scenes instead");
                return;
            }

            try
            {
                // Overrides sandbox battle scenes
                GameSceneDataManager.Instance?.LoadSPBattleScenes(battleScenesFilePath);   
            } catch (XmlException e)
            {
                _logger.Error($"Failed to load {battleScenesFilePath}: {e}");
            }
        }

    }
}
