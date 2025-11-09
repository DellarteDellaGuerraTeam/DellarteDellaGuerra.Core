using System.Reflection;
using System.Xml;
using Bannerlord.ExpandedTemplate.API;
using DellarteDellaGuerra.CharacterCreation;
using DellarteDellaGuerra.DisableNativeBehaviour.MissionBehaviours;
using DellarteDellaGuerra.DisplayCompilingShaders;
using DellarteDellaGuerra.DisplayCompilingShaders.Providers;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Domain.DisplayCompilingShaders;
using DellarteDellaGuerra.Domain.Tournament;
using DellarteDellaGuerra.Firearm;
using DellarteDellaGuerra.Firearm.Patches;
using DellarteDellaGuerra.Firearm.Reload;
using DellarteDellaGuerra.Infrastructure.Configuration.Providers;
using DellarteDellaGuerra.Infrastructure.DisplayCompilingShaders.Providers;
using DellarteDellaGuerra.Infrastructure.ExpandedTemplateApi.Logging;
using DellarteDellaGuerra.Infrastructure.Logging;
using DellarteDellaGuerra.Infrastructure.Patches;
using DellarteDellaGuerra.Infrastructure.Poc.Patches;
using DellarteDellaGuerra.Infrastructure.Steam.Patches;
using DellarteDellaGuerra.Infrastructure.Utils;
using DellarteDellaGuerra.MainMenu;
using DellarteDellaGuerra.RemoveOrphanChildren.MissionBehaviours;
using DellarteDellaGuerra.Tournament.Api;
using DellarteDellaGuerra.Tournament.Spi;
using DellarteDellaGuerra.Tournament.Spi.Mapper;
using DellarteDellaGuerra.Utils;
using NLog;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using ILogger = DellarteDellaGuerra.Domain.Common.Logging.Port.ILogger;
using Module = TaleWorlds.MountAndBlade.Module;

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
        private DadgTournamentModel _dadgTournamentModel;

        public SubModule()
        {
            _loggerFactory = new LoggerFactory(new LoggerConfigPathProvider());
            _campaignBehaviourDisabler = new CampaignBehaviourDisabler();
            _dadgConfigWatcher = new DadgConfigWatcher(_loggerFactory);
            _harmonyPatcher = new HarmonyPatcher(_loggerFactory);
            _logger = _loggerFactory.CreateLogger<SubModule>();

            new BannerlordExpandedTemplateApi()
                .UseLoggerFactory(new ExpandedTemplateLoggerFactory(_loggerFactory))
                .Bind();      
            
            InitialisePocIntegration();
            InitialiseSteamIntegration();
        }

        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            InfoPrinter.Display("DADG loaded");

            new DadgCampaignStartButtonAdder().AddDadgCampaignStartButton(
                Module
                    .CurrentModule);

        }

        // load the harmony patches once as soon as possible before reaching the main menu 
        protected override void OnSubModuleLoad()
        {
            _harmonyPatcher.PatchAll();
        }

        protected override void OnSubModuleUnloaded()
        {
            LogManager.Shutdown();
            _dadgConfigWatcher.Destroy();
        }

        protected override void InitializeGameStarter(Game game, IGameStarter starterObject)
        {
            if (game.GameType is not Campaign || starterObject is not CampaignGameStarter campaignGameStarter) return;

            HandleTournamentModelDependencies();
            campaignGameStarter.AddModel(_dadgTournamentModel);

            HandleDisplayCompilingShadersDependencies();
            CompilingShaderNotifier.Init(_displayShaderNumber);
            game.AddGameHandler<CompilingShaderNotifier>();

            campaignGameStarter.AddBehavior(new DadgCharacterCreationCampaignBehavior());
            campaignGameStarter.AddBehavior(new NobleOrphanChildrenCampaignBehaviour());
        }

        public override void OnGameInitializationFinished(Game game)
        {
            if (game.GameType is not Campaign) return;
        
            _campaignBehaviourDisabler.Disable(Campaign.Current.CampaignBehaviorManager);
            SetCampaignStartingDate();
            LoadDadgBattleScenes();
        }

        public override void OnMissionBehaviorInitialize(Mission mission)
        {
            base.OnMissionBehaviorInitialize(mission);
            mission.AddMissionBehavior(new FirearmReloadMissionLogic(_loggerFactory,
                new InMemoryWeaponEntityRepository()));
            mission.AddMissionBehavior(new FirearmSmokeMissionLogic(_loggerFactory));
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

        private void HandleTournamentModelDependencies()
        {
            var itemRepository = new ItemRepository(new ItemTierMapper(_loggerFactory));
            var getTournamentRewardUseCase = new GetTournamentRewardUseCase(itemRepository, new TroopRepository(),
                new TownRepository(),
                new RandomProvider(), new HighestTownProsperityProvider());
            _dadgTournamentModel = new DadgTournamentModel(getTournamentRewardUseCase);
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
