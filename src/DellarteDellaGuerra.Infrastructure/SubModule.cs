using System.Reflection;
using System.Xml;
using DellarteDellaGuerra.DisableNativeBehaviour.MissionBehaviours;
using DellarteDellaGuerra.DisplayCompilingShaders;
using DellarteDellaGuerra.DisplayCompilingShaders.Providers;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Domain.DisplayCompilingShaders;
using DellarteDellaGuerra.Domain.DisplayCompilingShaders.Ports;
using DellarteDellaGuerra.Domain.Equipment.Get;
using DellarteDellaGuerra.Domain.Equipment.Get.Port;
using DellarteDellaGuerra.Equipment.Get.Mappers;
using DellarteDellaGuerra.Equipment.Get.MissionLogic;
using DellarteDellaGuerra.Equipment.Get.Providers;
using DellarteDellaGuerra.Infrastructure.Cache;
using DellarteDellaGuerra.Infrastructure.Configuration.Providers;
using DellarteDellaGuerra.Infrastructure.DisplayCompilingShaders.Providers;
using DellarteDellaGuerra.Infrastructure.Equipment.Get;
using DellarteDellaGuerra.Infrastructure.Equipment.List.Repositories;
using DellarteDellaGuerra.Infrastructure.Equipment.List.Utils;
using DellarteDellaGuerra.Infrastructure.Logging;
using DellarteDellaGuerra.Infrastructure.Patches;
using DellarteDellaGuerra.Infrastructure.Utils;
using DellarteDellaGuerra.RemoveOrphanChildren.MissionBehaviours;
using DellarteDellaGuerra.Utils;
using NLog;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using ILogger = DellarteDellaGuerra.Domain.Common.Logging.Port.ILogger;

namespace DellarteDellaGuerra.Infrastructure
{
    public class SubModule : MBSubModuleBase
    {
        private readonly ILogger _logger;

        private readonly IConfigurationProvider<string> _loggerConfigProvider;
        private readonly ILoggerFactory _loggerFactory;

        private readonly CampaignBehaviourDisabler _campaignBehaviourDisabler;

        private readonly DadgConfigWatcher _dadgConfigWatcher;

        private readonly HarmonyPatcher _harmonyPatcher;

        private readonly ICacheProvider _cacheProvider;

        #region SpawnEquipmentPools
        private INpcCharacterXmlProcessor _npcCharacterXmlProcessor;
        private IEquipmentRepository _equipmentRepository;
        private ICivilianEquipmentRepository _civilianEquipmentRepository;
        private ISiegeEquipmentRepository _siegeEquipmentRepository;
        private IBattleEquipmentRepository _battleEquipmentRepository;
        private IEncounterTypeProvider _encounterTypeProvider;
        private ITroopBattleEquipmentProvider _troopBattleEquipmentProvider;
        private ITroopSiegeEquipmentProvider _troopSiegeEquipmentProvider;
        private ITroopCivilianEquipmentProvider _troopCivilianEquipmentProvider;
        private IGetTroopEquipment _getTroopEquipment;
        private EquipmentPoolMapper _equipmentMapper;
        private TroopEquipmentSpawnLogic _troopEquipmentSpawnLogic;
        #endregion

        #region DisplayCompilingShaders
        private ICompilingShaderDisplayer _compilingShaderDisplayer;
        private ICompilingShaderNotifierConfig _compilingShaderNotifierConfig;
        private ICompilingShaderNumberProvider _compilingShaderNumberProvider;
        private DisplayShaderNumber _displayShaderNumber;
        #endregion

        public SubModule()
        {
            _loggerConfigProvider = new LoggerConfigPathProvider();
            _loggerFactory = new LoggerFactory(_loggerConfigProvider);

            _campaignBehaviourDisabler = new CampaignBehaviourDisabler();

            _dadgConfigWatcher = new DadgConfigWatcher(_loggerFactory);

            _harmonyPatcher = new HarmonyPatcher(_loggerFactory);

            _cacheProvider = new CacheCampaignBehaviour();
            
            _logger = _loggerFactory.CreateLogger<SubModule>();
        }

        public override void OnBeforeMissionBehaviorInitialize(Mission mission)
        {
            base.OnBeforeMissionBehaviorInitialize(mission);
            mission.AddMissionBehavior(_troopEquipmentSpawnLogic);
        }
        
        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            InfoPrinter.Display("DADG loaded");
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

            HandleSpawnEquipmentPoolsDependencies();
            HandleDisplayCompilingShadersDependencies();

            CompilingShaderNotifier.Init(_displayShaderNumber);
            game.AddGameHandler<CompilingShaderNotifier>();
            
            campaignGameStarter.AddBehavior(new NobleOrphanChildrenCampaignBehaviour());
            campaignGameStarter.AddBehavior(_cacheProvider as CampaignBehaviorBase);
        }

        public override void OnGameInitializationFinished(Game game)
        {
            if (game.GameType is not Campaign) return;
        
            _campaignBehaviourDisabler.Disable(Campaign.Current.CampaignBehaviorManager);
            SetCampaignStartingDate();
            LoadDadgBattleScenes();
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

        #region SpawnEquipmentPools
        private void HandleSpawnEquipmentPoolsDependencies()
        {

            _npcCharacterXmlProcessor = new NpcCharacterXmlProcessor(_loggerFactory);
            _equipmentRepository = new EquipmentRepository(_npcCharacterXmlProcessor);
            _civilianEquipmentRepository = new CivilianEquipmentRepository(_equipmentRepository);
            _siegeEquipmentRepository = new SiegeEquipmentRepository(_equipmentRepository);
            _battleEquipmentRepository = new BattleEquipmentRepository(_equipmentRepository,
                _siegeEquipmentRepository,
                _civilianEquipmentRepository);
            _equipmentMapper = new EquipmentPoolMapper(MBObjectManager.Instance);
            _troopBattleEquipmentProvider =
                new TroopBattleEquipmentProvider(_loggerFactory, _battleEquipmentRepository, _cacheProvider);
            _troopSiegeEquipmentProvider =
                new TroopSiegeEquipmentProvider(_loggerFactory, _siegeEquipmentRepository, _cacheProvider);
            _troopCivilianEquipmentProvider =
                new TroopCivilianEquipmentProvider(_loggerFactory, _civilianEquipmentRepository, _cacheProvider);
            _encounterTypeProvider = new EncounterTypeProvider();
            _getTroopEquipment = new GetTroopEquipment(_encounterTypeProvider, _troopBattleEquipmentProvider,
                _troopSiegeEquipmentProvider, _troopCivilianEquipmentProvider);
            _troopEquipmentSpawnLogic = new TroopEquipmentSpawnLogic(_getTroopEquipment, _equipmentMapper);
        }
        #endregion

        #region DisplayCompilingShaders
        private void HandleDisplayCompilingShadersDependencies()
        {
            _compilingShaderDisplayer = new CompilingShaderDisplayer();
            _compilingShaderNotifierConfig = new CompilingShaderNotifierConfig(_dadgConfigWatcher);
            _compilingShaderNumberProvider = new CompilingShaderNumberProvider();
            _displayShaderNumber = new DisplayShaderNumber(_compilingShaderNotifierConfig, _compilingShaderNumberProvider, _compilingShaderDisplayer);
        }
        #endregion
    }
}
