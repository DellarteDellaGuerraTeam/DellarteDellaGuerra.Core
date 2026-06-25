using System;
using System.Linq;
using System.Reflection;
using System.Xml;
using Bannerlord.ExpandedTemplate.API;
using Bannerlord.UIExtenderEx;
using DellarteDellaGuerra.DisableNativeBehaviour.MissionBehaviours;
using DellarteDellaGuerra.DisplayCompilingShaders;
using DellarteDellaGuerra.MainMenu;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Domain.DisplayCompilingShaders;
using DellarteDellaGuerra.Firearm;
using DellarteDellaGuerra.Infrastructure.Configuration.Providers;
using DellarteDellaGuerra.Infrastructure.Events;
using DellarteDellaGuerra.Infrastructure.MbObjects;
using DellarteDellaGuerra.Infrastructure.SiegeEngines;
using DellarteDellaGuerra.Infrastructure.Utils;
using DellarteDellaGuerra.Integration.DI;
using DellarteDellaGuerra.Integration.Music.Patches;
using DellarteDellaGuerra.Integration.PrivateWars.Patches;
using HarmonyLib;
using DellarteDellaGuerra.Integration.ExpandedTemplateApi.Logging;
using DellarteDellaGuerra.Integration.SiegeEngines;
using DellarteDellaGuerra.Integration.SiegeEngines.Campaign;
using DellarteDellaGuerra.Integration.SiegeEngines.Mission;
using DellarteDellaGuerra.Domain.Levy.Port;
using DellarteDellaGuerra.Domain.PrivateWars.Port;
using DellarteDellaGuerra.Domain.Titles.Port;
using DellarteDellaGuerra.Domain.Titles;
using DellarteDellaGuerra.PrivateWars.Api.Campaign;
using DellarteDellaGuerra.PrivateWars.Api.GameModels;
using DellarteDellaGuerra.Infrastructure.Titles;
using DellarteDellaGuerra.Integration.Titles;
using DellarteDellaGuerra.Integration.Titles.UI;
using DellarteDellaGuerra.Titles.Api;
using DellarteDellaGuerra.Levy.Api;
using DellarteDellaGuerra.Titles.Api.Campaign;
using DellarteDellaGuerra.Titles.Api.GameModels;
using DellarteDellaGuerra.Tournament.Api;
using DellarteDellaGuerra.Utils;
using Harmony.DependencyInjection;
using Harmony.DependencyInjection.Patches;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using ILogger = DellarteDellaGuerra.Domain.Common.Logging.Port.ILogger;

namespace DellarteDellaGuerra.Integration
{
    public class SubModule : MBSubModuleBase
    {
        private readonly ILogger _logger;
        private IServiceProvider _serviceProvider;
        private UIExtender? _uiExtender;
        // Guard: EncounterGameMenuBehavior patches applied exactly once, deferred until
        // InitializeGameStarter where GameTexts._gameTextManager is guaranteed non-null.
        private static bool _encounterMenuPatchesApplied;

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
            // Deferred: EncounterGameMenuBehavior patches must not be applied at OnSubModuleLoad
            // because that type's static initializer calls GameTexts.FindText and GameTexts is not
            // ready until Game.Initialize() runs (which fires before InitializeGameStarter).
            // The guard ensures we apply exactly once even across campaign reloads.
            if (!_encounterMenuPatchesApplied)
            {
                _encounterMenuPatchesApplied = true;
                var menuHarmony = new HarmonyLib.Harmony("com.dadg.private-wars-menus");
                IPatch[] menuPatches =
                [
                    new BesiegeMenuConditionPatch(),
                    new ContinueSiegeMenuConditionPatch(),
                    new ArmyAttackMenuConditionPatch(),
                    new VillageHostileActionConditionPatch(),
                    new VillageRaidConditionPatch(),
                ];
                foreach (var p in menuPatches)
                    menuHarmony.Patch(p.TargetMethod, postfix: new HarmonyMethod(p.PatchMethod));
            }

            if (game.GameType is not Campaign || starterObject is not CampaignGameStarter campaignGameStarter) return;

            campaignGameStarter.AddModel(_serviceProvider.GetRequiredService<DadgTournamentModel>());

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

            // Private-war siege-retention scoring (drive is explicit; see DadgTargetScoreCalculatingModel)
            campaignGameStarter.AddModel(_serviceProvider.GetRequiredService<DadgTargetScoreCalculatingModel>());
            // Keep a private-war enemy out of the belligerent's army candidate pool (design §4.1)
            campaignGameStarter.AddModel(_serviceProvider.GetRequiredService<DadgArmyManagementCalculationModel>());
            // Block player entry into a same-kingdom private-war rival's town/castle (design §4.1)
            campaignGameStarter.AddModel(_serviceProvider.GetRequiredService<DadgSettlementAccessModel>());
            // Re-include garrison/militia/feud-lord defenders in a same-kingdom siege assault (design §4.1, Gate 1)
            campaignGameStarter.AddModel(_serviceProvider.GetRequiredService<DadgEncounterModel>());
            // Force a same-kingdom private-war field meeting into a real battle (design §4.3 — breaks the friendly-chat loop)
            campaignGameStarter.AddModel(_serviceProvider.GetRequiredService<DadgEncounterGameMenuModel>());

            // Feudal title campaign behaviours
            campaignGameStarter.AddBehavior(_serviceProvider.GetRequiredService<FeudalTitleCampaignBehavior>());
            campaignGameStarter.AddBehavior(_serviceProvider.GetRequiredService<InternalConflictCampaignBehavior>());
            campaignGameStarter.AddBehavior(_serviceProvider.GetRequiredService<FeudalTitleSwapBehavior>());
            campaignGameStarter.AddBehavior(_serviceProvider.GetRequiredService<LevyCampaignBehavior>());
            campaignGameStarter.AddBehavior(_serviceProvider.GetRequiredService<PrivateWarCampaignBehavior>());

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
                _serviceProvider.GetRequiredService<IAccumulateTensionUseCase>(),
                _serviceProvider.GetRequiredService<IPrivateWarRepository>(),
                _serviceProvider.GetRequiredService<IPrivateWarHostility>());

            // Initialise the static service locator used by the feudal UI
            // (encyclopedia mixins and the hierarchy screen are created by the game's UI
            // machinery, not by the DI container)
            FeudalUiServices.Initialise(
                _serviceProvider.GetRequiredService<ITitleRepository>(),
                _serviceProvider.GetRequiredService<IFeudalStructure>(),
                _serviceProvider.GetRequiredService<IGetSuzerainUseCase>(),
                _serviceProvider.GetRequiredService<IGetDirectVassalsUseCase>(),
                _serviceProvider.GetRequiredService<IBuildFeudalMapUseCase>(),
                _serviceProvider.GetRequiredService<ILevyRepository>());

            CompilingShaderNotifier.Init(_serviceProvider.GetRequiredService<DisplayShaderNumber>());
            game.AddGameHandler<CompilingShaderNotifier>();
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
            mission.AddMissionBehavior(_serviceProvider.GetRequiredService<RemoveSiegeTowerSpawnersMissionLogic>());
        }

        public override void RegisterSubModuleObjects(bool isSavedCmapaign)
        {
            foreach (var provider in _serviceProvider.GetServices<IMBObjectProvider<SkillObject>>())
                MBObjectManager.Instance.RegisterPresumedObject(provider.GetMbObject());
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
