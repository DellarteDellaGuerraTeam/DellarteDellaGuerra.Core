using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;
using Bannerlord.Cannons.Api;
using Bannerlord.ExpandedTemplate.API;
using DellarteDellaGuerra.DisableNativeBehaviour.MissionBehaviours;
using DellarteDellaGuerra.DisplayCompilingShaders;
using DellarteDellaGuerra.DisplayCompilingShaders.Providers;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Domain.DisplayCompilingShaders;
using DellarteDellaGuerra.Domain.SiegeEngines;
using DellarteDellaGuerra.Domain.Tournament.Reward;
using DellarteDellaGuerra.Firearm;
using DellarteDellaGuerra.Firearm.Patches;
using DellarteDellaGuerra.Firearm.Reload;
using DellarteDellaGuerra.Infrastructure.Cannon.Campaign;
using DellarteDellaGuerra.Infrastructure.Cannon.Campaign.UI;
using DellarteDellaGuerra.Infrastructure.Cannon.Infra.Repo;
using DellarteDellaGuerra.Infrastructure.Cannon.Mission.Battle;
using DellarteDellaGuerra.Infrastructure.Cannon.Mission.Siege.UI;
using DellarteDellaGuerra.Infrastructure.Cannon.Util.UI;
using DellarteDellaGuerra.Infrastructure.Configuration.Providers;
using DellarteDellaGuerra.Infrastructure.DisplayCompilingShaders.Providers;
using DellarteDellaGuerra.Infrastructure.Events;
using DellarteDellaGuerra.Infrastructure.ExpandedTemplateApi.Logging;
using DellarteDellaGuerra.Infrastructure.Logging;
using DellarteDellaGuerra.Infrastructure.Patches;
using DellarteDellaGuerra.Infrastructure.Poc.Patches;
using DellarteDellaGuerra.Infrastructure.SiegeEngines;
using DellarteDellaGuerra.Infrastructure.SiegeEngines.Port;
using DellarteDellaGuerra.Infrastructure.Steam.Patches;
using DellarteDellaGuerra.Infrastructure.Utils;
using DellarteDellaGuerra.RemoveOrphanChildren.MissionBehaviours;
using DellarteDellaGuerra.Tournament.Api;
using DellarteDellaGuerra.Tournament.Reward.Spi;
using DellarteDellaGuerra.Tournament.Reward.Spi.Mapper;
using DellarteDellaGuerra.Utils;
using NLog;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.Core;
using TaleWorlds.DotNet;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using ILogger = DellarteDellaGuerra.Domain.Common.Logging.Port.ILogger;

namespace DellarteDellaGuerra.Infrastructure
{
    public class SubModule : MBSubModuleBase
    {
        private readonly ILogger _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly CampaignBehaviourDisabler _campaignBehaviourDisabler;
        private readonly DadgConfigWatcher _dadgConfigWatcher;
        private readonly HarmonyPatcher _harmonyPatcher;

        private DisplayShaderNumber _displayShaderNumber;
        private readonly OnSubModuleLoadEventPubSub _onSubModuleLoadEventPubSub;

        private ICannonPrefabProvider _cannonPrefabProvider;
        private ICannonAvailabilityProvider _cannonAvailabilityProvider;

        public SubModule()
        {
            _loggerFactory = new LoggerFactory(new LoggerConfigPathProvider());
            _campaignBehaviourDisabler = new CampaignBehaviourDisabler();
            _dadgConfigWatcher = new DadgConfigWatcher(_loggerFactory);
            _harmonyPatcher = new HarmonyPatcher(_loggerFactory);
            _logger = _loggerFactory.CreateLogger<SubModule>();
            _onSubModuleLoadEventPubSub = new OnSubModuleLoadEventPubSub();

            new BannerlordExpandedTemplateApi()
                .UseLoggerFactory(new ExpandedTemplateLoggerFactory(_loggerFactory))
                .Bind();

            InitialiseCannonFeature();
            InitialisePocIntegration();
            InitialiseSteamIntegration();
        }

        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            InfoPrinter.Display("DADG loaded");
        }

        // load the harmony patches once as soon as possible before reaching the main menu
        protected override void OnSubModuleLoad()
        {
            _onSubModuleLoadEventPubSub.Publish();
            _harmonyPatcher.PatchAll();

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
            _dadgConfigWatcher.Destroy();
        }

        protected override void InitializeGameStarter(Game game, IGameStarter starterObject)
        {
            if (game.GameType is not Campaign || starterObject is not CampaignGameStarter campaignGameStarter) return;

            HandleTournamentModelDependencies(campaignGameStarter);
            InitialiseSiegeEngineLogic(campaignGameStarter);

            HandleDisplayCompilingShadersDependencies();
            CompilingShaderNotifier.Init(_displayShaderNumber);
            game.AddGameHandler<CompilingShaderNotifier>();

            campaignGameStarter.AddBehavior(new NobleOrphanChildrenCampaignBehaviour());
        }

        public override void OnGameInitializationFinished(Game game)
        {
            if (game.GameType is not Campaign) return;
            _campaignBehaviourDisabler.Disable(Campaign.Current.CampaignBehaviorManager);
            SetCampaignStartingDate();
            LoadDadgBattleScenes();
        }

        public override void OnBeforeMissionBehaviorInitialize(Mission mission)
        {
            base.OnBeforeMissionBehaviorInitialize(mission);
            mission.AddMissionBehavior(new FirearmReloadMissionLogic(_loggerFactory,
                new InMemoryWeaponEntityRepository()));
            mission.AddMissionBehavior(new FirearmSmokeMissionLogic(_loggerFactory));
            mission.AddMissionBehavior(new CannonTeamMissionLogic());
        }

        public override void RegisterSubModuleObjects(bool isSavedCmapaign)
        {
            InitSkills();
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

        #region DisplayCompilingShaders
        private void HandleDisplayCompilingShadersDependencies()
        {
            var compilingShaderDisplayer = new CompilingShaderDisplayer();
            var compilingShaderNotifierConfig = new CompilingShaderNotifierConfig(_dadgConfigWatcher);
            var compilingShaderNumberProvider = new CompilingShaderNumberProvider();
            _displayShaderNumber = new DisplayShaderNumber(compilingShaderNotifierConfig, compilingShaderNumberProvider,
                compilingShaderDisplayer);
        }
        #endregion

        #region POCIntegration

        private void InitialisePocIntegration()
        {
            new PocConfigReaderOverriderPatch(_harmonyPatcher, _loggerFactory);
        }

        #endregion

        #region SteamIntegration

        private void InitialiseSteamIntegration()
        {
            new FixSettlementFilePathPatch(_harmonyPatcher, _loggerFactory);
            new FixSettlementDistanceCacheFilePathPatch(_harmonyPatcher, _loggerFactory);
        }

        #endregion

        #region Tournament

        private void HandleTournamentModelDependencies(CampaignGameStarter campaignGameStarter)
        {
            var itemRepository = new ItemRepository(new ItemTierMapper(_loggerFactory));
var getTournamentRewardUseCase = new GetTournamentRewardUseCase(itemRepository, new TroopRepository(),
                new TownRepository(),
                new RandomProvider(), new HighestTownProsperityProvider());
            campaignGameStarter.AddModel(new DadgTournamentModel(getTournamentRewardUseCase));
        }

        #endregion

        #region Cannons

        private void InitialiseCannonFeature()
        {
            // Single shared registry — all cannon subsystems use this instance
            var cannonRegistry = new CannonRegistry();

            // Load cannon types from config (falls back to default Falconet if missing)
            var configuration = new XmlCannonConfiguration(
                ResourceLocator.GetConfigurationFilePath("cannons.xml") ?? string.Empty);
            foreach (var properties in configuration.LoadCannonProperties())
                cannonRegistry.RegisterCannonType(new ConfigurableCannonType(properties), new GenericCannonFactory(properties.Id));

            _cannonPrefabProvider = new CannonPrefabProvider(cannonRegistry);
            var iconProvider = new CannonIconProvider(cannonRegistry);
            _cannonAvailabilityProvider = new CannonAvailabilityProvider(cannonRegistry);

            var deploymentIconRepo = new DeploymentSiegeEngineIconRepository(iconProvider);
            var mapIconRepo = new MapSiegeEngineIconRepository(cannonRegistry);
            var prefabRepo = new PrefabSiegeEngineRepository(cannonRegistry);

            var brushStyleExtender = new BrushStyleExtender(_loggerFactory,
                UIResourceManager.BrushFactory,
                UIResourceManager.SpriteData);
            var campaignMapEnricher = new CampaignMapSiegeEngineDeploymentIconEnricher(brushStyleExtender);
            var deploymentEnricher = new SiegeEngineDeploymentIconEnricher(brushStyleExtender);

            var iconUseCase = new SiegeEngineIconRegistrationUseCase(_onSubModuleLoadEventPubSub,
                deploymentEnricher, campaignMapEnricher, deploymentIconRepo);
            iconUseCase.RegisterSiegeEngineIcons();

            _harmonyPatcher.AddPatch(new OrderSiegeMachineItemButtonWidgetPatch(deploymentIconRepo));
            _harmonyPatcher.AddPatch(new MapSiegePOIBrushWidgetManualPatch(mapIconRepo, UIResourceManager.SpriteData));
            _harmonyPatcher.AddPatch(new MapSiegePOIVMPatch(mapIconRepo));

            var prefabCachePatches = new CampaignMapSiegePrefabEntityCachePatches(prefabRepo);
            prefabCachePatches.GetPatches().ToList().ForEach(patch => _harmonyPatcher.AddPatch(patch));

            OrderSiegeMachineVM_GetSiegeTypePatch.SetRegistry(cannonRegistry);

            CannonSystemInitialiser.Initialise();
        }

        /// Must be called after cannon initialisation since the logic requires cannons
        public void InitialiseSiegeEngineLogic(CampaignGameStarter campaignGameStarter)
        {
            var getDefaultSiegeEngine = new GetDefaultSiegeEngine();

            campaignGameStarter.AddModel(new DadgSiegeStrategyActionModel(
                campaignGameStarter.Models.OfType<DefaultSiegeStrategyActionModel>().Last(),
                MBObjectManager.Instance, _loggerFactory, getDefaultSiegeEngine));

            campaignGameStarter.AddModel(new DadgSiegeEventModel(
                campaignGameStarter.Models.OfType<SiegeEventModel>().Last(),
                _cannonPrefabProvider, _cannonAvailabilityProvider, _loggerFactory));
        }

        #endregion

        private static void InitSkills()
        {
            var firearmSkill = new FirearmSkill();
            firearmSkill.Initialise();

            AddFirearmSkillAsRelevantSkillPatch.SetFirearmSkill(firearmSkill);
        }
    }
}
