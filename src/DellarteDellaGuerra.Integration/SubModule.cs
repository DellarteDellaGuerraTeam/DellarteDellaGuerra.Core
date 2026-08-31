using System;
using System.Linq;
using System.Xml;
using Bannerlord.ExpandedTemplate.API;
using Bannerlord.UIExtenderEx;
using DellarteDellaGuerra.DisableNativeBehaviour.MissionBehaviours;
using DellarteDellaGuerra.DisplayCompilingShaders;
using DellarteDellaGuerra.MainMenu;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Domain.DisplayCompilingShaders;
using DellarteDellaGuerra.Firearm;
using DellarteDellaGuerra.Heraldry;
using DellarteDellaGuerra.Infrastructure.Configuration.Providers;
using DellarteDellaGuerra.Infrastructure.Campaign;
using DellarteDellaGuerra.Infrastructure.Events;
using DellarteDellaGuerra.Infrastructure.MbObjects;
using DellarteDellaGuerra.Infrastructure.SiegeEngines;
using DellarteDellaGuerra.Infrastructure.Utils;
using DellarteDellaGuerra.Integration.CampaignTime;
using DellarteDellaGuerra.Integration.DI;
using DellarteDellaGuerra.Integration.Music.Patches;
using DellarteDellaGuerra.Integration.PrivateWars;
using HarmonyLib;
using DellarteDellaGuerra.Integration.ExpandedTemplateApi.Logging;
using DellarteDellaGuerra.Integration.Music.Patches;
using HarmonyLib;
using DellarteDellaGuerra.Integration.ExpandedTemplateApi.Logging;
using DellarteDellaGuerra.Integration.Initialisation;
using DellarteDellaGuerra.Integration.SiegeEngines;
using DellarteDellaGuerra.Integration.SiegeEngines.Campaign;
using DellarteDellaGuerra.Integration.SiegeEngines.Mission;
using Bannerlord.PrivateWars.Api;
using DellarteDellaGuerra.Domain.Titles.Port;
using DellarteDellaGuerra.Domain.Titles;
using DellarteDellaGuerra.Infrastructure.Titles;
using DellarteDellaGuerra.Integration.Titles;
using DellarteDellaGuerra.Integration.Titles.UI;
using DellarteDellaGuerra.Titles.Api;
using DellarteDellaGuerra.Titles.Api.Campaign;
using DellarteDellaGuerra.Titles.Api.GameModels;
using DellarteDellaGuerra.Domain.Tournament.Jousting.Port;
using DellarteDellaGuerra.Tournament.Api;
using DellarteDellaGuerra.Tournament.Jousting.Api.Campaign;
using DellarteDellaGuerra.Utils;
using Harmony.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using ILogger = DellarteDellaGuerra.Domain.Common.Logging.Port.ILogger;
using TournamentSettlementAccessModel = DellarteDellaGuerra.Tournament.Api.DadgSettlementAccessModel;

namespace DellarteDellaGuerra.Integration
{
    public class SubModule : MBSubModuleBase
    {
        private readonly ILogger _logger;
        private IServiceProvider _serviceProvider;
        private UIExtender? _uiExtender;

        public SubModule()
        {
            // Applied before DI is built: this patch must intercept PsaiCore.LoadSoundtrackFromProjectFile
            // which fires during game startup before OnSubModuleLoad. Applying it here ensures it is
            // in place regardless of when the DI container is constructed.
            var earlyHarmony = new HarmonyLib.Harmony("com.dadg.early");
            var musicPatch = new MBMusicManagerInitializePatch();
            earlyHarmony.Patch(musicPatch.TargetMethod, prefix: new HarmonyMethod(musicPatch.PatchMethod));


            var loggerFactory = new LoggingContainer().Build().GetRequiredService<ILoggerFactory>();
            _logger = loggerFactory.CreateLogger<SubModule>();

            new BannerlordExpandedTemplateApi()
                .UseLoggerFactory(new ExpandedTemplateLoggerFactory(loggerFactory))
                .Bind();
        }

        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            InfoPrinter.Display("DADG loaded");
            var currentModule = TaleWorlds.MountAndBlade.Module.CurrentModule;
            _serviceProvider.GetRequiredService<DadgCampaignStartButtonAdder>().AddDadgCampaignStartButton(currentModule);
            _serviceProvider.GetRequiredService<VanillaCampaignButtonsRemover>().RemoveVanillaCampaignOptions(currentModule);
        }

        protected override void OnSubModuleLoad()
        {
            _serviceProvider = new DadgServiceContainer().Build();
            _serviceProvider.GetRequiredService<IEventPublisher<SubModuleLoadEvent>>()
                .Publish(new SubModuleLoadEvent());

            _serviceProvider.GetRequiredService<DadgScriptComponentRegistrar>()
                .RegisterLoadedDadgTypes();

            _serviceProvider.GetRequiredService<IHarmonyPatcher>().ApplyPatches();

            // UIExtenderEx discovers the feudal encyclopedia mixins and prefab
            // patches in this assembly by attribute
            _uiExtender = UIExtender.Create("DellarteDellaGuerra.Core");
            _uiExtender.Register(typeof(SubModule).Assembly);
            _uiExtender.Enable();
        }

        protected override void OnSubModuleUnloaded()
        {
            LogManager.Shutdown();
            _serviceProvider.GetRequiredService<DadgConfigWatcher>().Destroy();
        }

        protected override void InitializeGameStarter(Game game, IGameStarter starterObject)
        {
            if (game.GameType is not Campaign || starterObject is not CampaignGameStarter campaignGameStarter) return;

            campaignGameStarter.AddModel(ActivatorUtilities.CreateInstance<FemaleNonCombatantHeroCreationModel>(
                _serviceProvider,
                campaignGameStarter.Models.OfType<HeroCreationModel>().Last()));
            campaignGameStarter.AddModel(_serviceProvider.GetRequiredService<DadgCampaignTimeModel>());
            // Supply the private-war mechanism with DADG's feudal hierarchy and configured nameplate
            // tints. The Bannerlord.PrivateWars module has self-wired its own behaviours, models, and
            // patches by this point; DADG only feeds it the two consumer-provided collaborators.
            var privateWarsApi = _serviceProvider.GetRequiredService<IPrivateWarsApi>();
            privateWarsApi.SetSuzerainProvider(new FeudalHierarchyAdapter(
                _serviceProvider.GetRequiredService<IGetSuzerainUseCase>()));
            var privateWarConfig = _serviceProvider.GetRequiredService<DadgConfigWatcher>().Config?.PrivateWarConfig;
            privateWarsApi.SetNameplateColors(
                privateWarConfig?.PrivateWarEnemyNameplateColorArgb,
                privateWarConfig?.PrivateWarAllyNameplateColorArgb);

            campaignGameStarter.AddModel(_serviceProvider.GetRequiredService<DadgTournamentModel>());
            var joustRequirementsProvider = _serviceProvider.GetRequiredService<IJoustRequirementsProvider>();
            SettlementAccessModel settlementAccessModel = new TournamentSettlementAccessModel(
                campaignGameStarter.Models.OfType<SettlementAccessModel>().Last(),
                joustRequirementsProvider);
            campaignGameStarter.AddModel(settlementAccessModel);

            var loggerFactory = _serviceProvider.GetRequiredService<ILoggerFactory>();
            campaignGameStarter.AddModel(new DadgSiegeStrategyActionModel(
                _serviceProvider.GetService<ICannonRepository>(),
                loggerFactory, MBObjectManager.Instance,
                campaignGameStarter.Models.OfType<DefaultSiegeStrategyActionModel>().Last()));
            campaignGameStarter.AddModel(ActivatorUtilities.CreateInstance<DadgSiegeEventModel>(
                _serviceProvider,
                campaignGameStarter.Models.OfType<SiegeEventModel>().Last()));

            // Feudal title models (each replaces the vanilla default)
            campaignGameStarter.AddModel(_serviceProvider.GetRequiredService<DadgDiplomacyModel>());
            campaignGameStarter.AddModel(_serviceProvider.GetRequiredService<DadgClanPoliticsModel>());
            campaignGameStarter.AddModel(_serviceProvider.GetRequiredService<DadgSettlementLoyaltyModel>());

            // Feudal title campaign behaviours
            campaignGameStarter.AddBehavior(_serviceProvider.GetRequiredService<FeudalTitleCampaignBehavior>());
            campaignGameStarter.AddBehavior(_serviceProvider.GetRequiredService<InternalConflictCampaignBehavior>());
            campaignGameStarter.AddBehavior(_serviceProvider.GetRequiredService<FeudalTitleSwapBehavior>());

            // Initialise the static service locator used by KingdomDecision subclasses
            FeudalServices.Initialise(
                _serviceProvider.GetRequiredService<ITitleRepository>(),
                _serviceProvider.GetRequiredService<IClaimRepository>(),
                _serviceProvider.GetRequiredService<ITensionRepository>(),
                _serviceProvider.GetRequiredService<IFeudalStructure>(),
                _serviceProvider.GetRequiredService<IAssignTitleUseCase>(),
                _serviceProvider.GetRequiredService<IGetSuzerainUseCase>(),
                _serviceProvider.GetRequiredService<IEvaluateClaimUseCase>(),
                _serviceProvider.GetRequiredService<IComputeFeudalSupportUseCase>(),
                _serviceProvider.GetRequiredService<IComputeInfluenceTierBonusUseCase>(),
                _serviceProvider.GetRequiredService<IAccumulateTensionUseCase>());

            // Initialise the static service locator used by the feudal UI
            // (encyclopedia mixins and the hierarchy screen are created by the game's UI
            // machinery, not by the DI container)
            FeudalUiServices.Initialise(
                _serviceProvider.GetRequiredService<ITitleRepository>(),
                _serviceProvider.GetRequiredService<IFeudalStructure>(),
                _serviceProvider.GetRequiredService<IGetSuzerainUseCase>(),
                _serviceProvider.GetRequiredService<IGetDirectVassalsUseCase>(),
                _serviceProvider.GetRequiredService<IBuildFeudalMapUseCase>());

            CompilingShaderNotifier.Init(_serviceProvider.GetRequiredService<DisplayShaderNumber>());
            game.AddGameHandler<CompilingShaderNotifier>();

            campaignGameStarter.AddBehavior(new JoustTournamentCampaignBehavior(joustRequirementsProvider));
        }

        public override void OnGameInitializationFinished(Game game)
        {
            if (game.GameType is not Campaign) return;
            _serviceProvider.GetRequiredService<CampaignBehaviourDisabler>()
                .Disable(Campaign.Current.CampaignBehaviorManager);
            LoadDadgBattleScenes();
        }

        public override void OnBeforeMissionBehaviorInitialize(Mission mission)
        {
            base.OnBeforeMissionBehaviorInitialize(mission);
            mission.AddMissionBehavior(_serviceProvider.GetRequiredService<FirearmReloadMissionLogic>());
            mission.AddMissionBehavior(_serviceProvider.GetRequiredService<FirearmSmokeMissionLogic>());
            mission.AddMissionBehavior(_serviceProvider.GetRequiredService<RemoveSiegeTowerSpawnersMissionLogic>());
            mission.AddMissionBehavior(_serviceProvider.GetRequiredService<BannerSurcoatMissionLogic>());
            mission.AddMissionBehavior(_serviceProvider.GetRequiredService<TournamentRecapBannerMissionLogic>());
        }

        public override void RegisterSubModuleObjects(bool isSavedCmapaign)
        {
            foreach (var provider in _serviceProvider.GetServices<IMBObjectProvider<SkillObject>>())
                MBObjectManager.Instance.RegisterPresumedObject(provider.GetMbObject());
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
