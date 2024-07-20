using System.Reflection;
using System.Xml;
using DellarteDellaGuerra.DisableNativeBehaviour.MissionBehaviours;
using DellarteDellaGuerra.DisplayCompilingShaders;
using DellarteDellaGuerra.DisplayCompilingShaders.Providers;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Domain.DisplayCompilingShaders;
using DellarteDellaGuerra.Domain.EquipmentPool;
using DellarteDellaGuerra.Domain.EquipmentPool.Util;
using DellarteDellaGuerra.Infrastructure.Caching;
using DellarteDellaGuerra.Infrastructure.Configuration.Providers;
using DellarteDellaGuerra.Infrastructure.DisplayCompilingShaders.Providers;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.Get;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Providers.Battle;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Providers.Civilian;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Providers.Siege;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Repositories;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Utils;
using DellarteDellaGuerra.Infrastructure.Logging;
using DellarteDellaGuerra.Infrastructure.Missions.Events;
using DellarteDellaGuerra.Infrastructure.Patches;
using DellarteDellaGuerra.Infrastructure.Utils;
using DellarteDellaGuerra.RemoveOrphanChildren.MissionBehaviours;
using DellarteDellaGuerra.SetSpawnEquipment.EquipmentPool.Mappers;
using DellarteDellaGuerra.SetSpawnEquipment.EquipmentPool.Providers;
using DellarteDellaGuerra.SetSpawnEquipment.MissionLogic;
using DellarteDellaGuerra.SetSpawnEquipment.MissionLogic.Utils;
using DellarteDellaGuerra.SetSpawnEquipment.Ports;
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
        private readonly ILoggerFactory _loggerFactory;
        private readonly CampaignBehaviourDisabler _campaignBehaviourDisabler;
        private readonly DadgConfigWatcher _dadgConfigWatcher;
        private readonly HarmonyPatcher _harmonyPatcher;
        private readonly ICacheProvider _cacheProvider;
        private readonly IMissionEventCallbackRegister _missionEventExecutor;
        private DisplayShaderNumber _displayShaderNumber;

        private BattleMissionSpawnEquipmentPoolSetter _battleMissionSpawnEquipmentInitialiser;
        private FriendlyMissionSpawnEquipmentPoolSetter _friendlyMissionSpawnEquipmentInitialiser;
        
        public SubModule()
        {
            _loggerFactory = new LoggerFactory(new LoggerConfigPathProvider());
            _campaignBehaviourDisabler = new CampaignBehaviourDisabler();
            _dadgConfigWatcher = new DadgConfigWatcher(_loggerFactory);
            _harmonyPatcher = new HarmonyPatcher(_loggerFactory);
            _cacheProvider = new CacheCampaignBehaviour();
            _missionEventExecutor = new MissionEventExecutor();
            _logger = _loggerFactory.CreateLogger<SubModule>();
        }

        public override void OnBeforeMissionBehaviorInitialize(Mission mission)
        {
            base.OnBeforeMissionBehaviorInitialize(mission);

            mission.MissionBehaviors.Insert(0, _missionEventExecutor as MissionBehavior);
            mission.MissionLogics.Insert(0, _missionEventExecutor as MissionLogic);
            AddEquipmentSpawnMissionBehaviour(mission);
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
            _cacheProvider.InvalidateCache();

            if (game.GameType is not Campaign || starterObject is not CampaignGameStarter campaignGameStarter) return;

            HandleEquipmentSpawnDependencies();
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

        #region GetEquipmentSpawn

        private void HandleEquipmentSpawnDependencies()
        {
            var npcCharacterXmlProcessor = new MergedModulesXmlProcessor(_loggerFactory, _cacheProvider);
            var characterEquipmentRepository = new CharacterEquipmentPoolRepository(npcCharacterXmlProcessor);
            var equipmentRosterRepository = new EquipmentRosterEquipmentPoolRepository(npcCharacterXmlProcessor);
            var civilianEquipmentRepository =
                new CivilianEquipmentPoolProvider(_loggerFactory, characterEquipmentRepository,
                    equipmentRosterRepository);
            var siegeEquipmentRepository =
                new SiegeEquipmentPoolProvider(_loggerFactory, characterEquipmentRepository, equipmentRosterRepository);
            var battleEquipmentRepository =
                new BattleEquipmentPoolProvider(_loggerFactory, siegeEquipmentRepository, civilianEquipmentRepository,
                    characterEquipmentRepository, equipmentRosterRepository);
            var troopBattleEquipmentProvider =
                new TroopBattleEquipmentProvider(_loggerFactory, battleEquipmentRepository, _cacheProvider);
            var troopSiegeEquipmentProvider =
                new TroopSiegeEquipmentProvider(_loggerFactory, siegeEquipmentRepository, _cacheProvider);
            var troopCivilianEquipmentProvider =
                new TroopCivilianEquipmentProvider(_loggerFactory, civilianEquipmentRepository, _cacheProvider);
            var encounterTypeProvider = new EncounterTypeProvider();

            var random = new Random();
            var equipmentPicker = new EquipmentPoolPoolPicker(random);

            var equipmentMapper = new EquipmentPoolMapper(MBObjectManager.Instance);
            var getEquipmentPool = new GetEquipmentPool(encounterTypeProvider, troopBattleEquipmentProvider,
                troopSiegeEquipmentProvider, troopCivilianEquipmentProvider, equipmentPicker);

            var agentSpawnEquipmentPoolSetter = new AgentSpawnEquipmentPoolSetter(getEquipmentPool, equipmentMapper,
                _missionEventExecutor, MBObjectManager.Instance);

            _battleMissionSpawnEquipmentInitialiser =
                new BattleMissionSpawnEquipmentPoolSetter(agentSpawnEquipmentPoolSetter);
            _friendlyMissionSpawnEquipmentInitialiser =
                new FriendlyMissionSpawnEquipmentPoolSetter(getEquipmentPool, equipmentMapper, _loggerFactory);
        }

        private void AddEquipmentSpawnMissionBehaviour(Mission mission)
        {
            // mission.MissionBehaviors.Insert(1, _friendlyMissionSpawnEquipmentInitialiser);
            // mission.MissionLogics.Insert(1, _friendlyMissionSpawnEquipmentInitialiser);
            mission.AddMissionBehavior(_friendlyMissionSpawnEquipmentInitialiser);
            // mission.AddMissionBehavior(_battleMissionSpawnEquipmentInitialiser);
        }
        #endregion

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
    }
}
