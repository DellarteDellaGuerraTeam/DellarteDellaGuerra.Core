﻿using System;
using System.Linq;
using System.Reflection;
using System.Xml;
using DellarteDellaGuerra.DisableNativeBehaviour.MissionBehaviours;
using DellarteDellaGuerra.DisplayCompilingShaders;
using DellarteDellaGuerra.DisplayCompilingShaders.Adapters;
using DellarteDellaGuerra.Domain.DisplayCompilingShaders;
using DellarteDellaGuerra.Domain.DisplayCompilingShaders.Ports;
using DellarteDellaGuerra.Domain.Logging.Port;
using DellarteDellaGuerra.Domain.SpawnEquipmentPools.ListEquipment;
using DellarteDellaGuerra.Domain.SpawnEquipmentPools.ListEquipment.Port;
using DellarteDellaGuerra.Infrastructure.Configuration.Models;
using DellarteDellaGuerra.Infrastructure.Configuration.Providers;
using DellarteDellaGuerra.Infrastructure.DisplayCompilingShaders.Adapters;
using DellarteDellaGuerra.Infrastructure.Logging;
using DellarteDellaGuerra.Infrastructure.Patches;
using DellarteDellaGuerra.Infrastructure.Poc.Patches;
using DellarteDellaGuerra.Infrastructure.SpawnEquipmentPools.ListEquipment.Repositories;
using DellarteDellaGuerra.Infrastructure.SpawnEquipmentPools.ListEquipment.Utils;
using DellarteDellaGuerra.Infrastructure.Steam.Patches;
using DellarteDellaGuerra.Infrastructure.Utils;
using DellarteDellaGuerra.RemoveOrphanChildren.MissionBehaviours;
using DellarteDellaGuerra.SpawnEquipmentPools.Mappers;
using DellarteDellaGuerra.SpawnEquipmentPools.MissionBehaviours;
using DellarteDellaGuerra.SpawnEquipmentPools.MissionLogic;
using DellarteDellaGuerra.Utils;
using HarmonyLib;
using NLog;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using ILogger = DellarteDellaGuerra.Domain.Logging.Port.ILogger;

namespace DellarteDellaGuerra.Infrastructure
{
    public class SubModule : MBSubModuleBase
    {
        private readonly ILogger _logger;

        private readonly IConfigurationProvider<string> _loggerConfigProvider;
        private readonly ILoggerFactory _loggerFactory;

        private readonly DadgMission _dadgMission;
        private readonly CampaignBehaviourDisabler _campaignBehaviourDisabler;

        private readonly DadgConfigWatcher _dadgConfigWatcher;

        private readonly HarmonyPatcher _harmonyPatcher;

        #region SpawnEquipmentPools
        private INpcCharacterXmlProcessor _npcCharacterXmlProcessor;
        private ListBattleEquipment _listBattleEquipment;
        private EquipmentPoolMapper _equipmentMapper;
        private MissionTroopEquipmentCampaignBehaviour _missionTroopEquipmentCampaignBehaviour;
        private ICharacterEquipmentRepository _characterEquipmentRepository;
        private ICharacterCivilianEquipmentRepository _characterCivilianEquipmentRepository;
        private ICharacterSiegeEquipmentRepository _characterSiegeEquipmentRepository;
        #endregion

        #region DisplayCompilingShaders
        private ICompilingShaderDisplayer _compilingShaderDisplayer;
        private ICompilingShaderNotifierConfig _compilingShaderNotifierConfig;
        private ICompilingShaderNumberProvider _compilingShaderNumberProvider;
        private DisplayShaderNumber _displayShaderNumber;
        private IConfigurationProvider<DadgConfig> _configProvider;
        #endregion

        public SubModule()
        {
            _loggerConfigProvider = new LoggerConfigPathProvider();
            _loggerFactory = new LoggerFactory(_loggerConfigProvider);

            _dadgMission = new DadgMission(_loggerFactory);
            _campaignBehaviourDisabler = new CampaignBehaviourDisabler();

            _dadgConfigWatcher = new DadgConfigWatcher(_loggerFactory);

            _harmonyPatcher = new HarmonyPatcher(_loggerFactory);
            
            _logger = _loggerFactory.CreateLogger<SubModule>();
        }

        public override void OnBeforeMissionBehaviorInitialize(Mission mission)
        {
            base.OnBeforeMissionBehaviorInitialize(mission);
            _dadgMission.Decorate(mission, Campaign.Current.CampaignBehaviorManager);
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
            campaignGameStarter.AddBehavior(_missionTroopEquipmentCampaignBehaviour);
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
            _characterEquipmentRepository = new CharacterEquipmentRepository(_npcCharacterXmlProcessor);
            _characterCivilianEquipmentRepository = new CharacterCivilianEquipmentRepository(_characterEquipmentRepository);
            _characterSiegeEquipmentRepository = new CharacterSiegeEquipmentRepository(_characterEquipmentRepository);
            _listBattleEquipment = new ListBattleEquipment(_characterEquipmentRepository, _characterSiegeEquipmentRepository, _characterCivilianEquipmentRepository);
            _equipmentMapper = new EquipmentPoolMapper(MBObjectManager.Instance);
            _missionTroopEquipmentCampaignBehaviour = new MissionTroopEquipmentCampaignBehaviour(_loggerFactory, _listBattleEquipment, _equipmentMapper);
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
