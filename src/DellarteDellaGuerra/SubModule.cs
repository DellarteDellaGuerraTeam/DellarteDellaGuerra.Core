using System;
using System.Reflection;
using System.Xml;
using DellarteDellaGuerra.Configuration.Providers;
using DellarteDellaGuerra.Missions;
using DellarteDellaGuerra.DadgCampaign;
using DellarteDellaGuerra.DadgCampaign.Behaviours;
using DellarteDellaGuerra.GameManager;
using DellarteDellaGuerra.Logging;
using DellarteDellaGuerra.Patches;
using DellarteDellaGuerra.Utils;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using NLog;

namespace DellarteDellaGuerra
{
    public class SubModule : MBSubModuleBase
    {
        private static readonly Logger Logger = LoggerFactory.GetLogger<SubModule>();
        private static readonly Harmony Harmony = new ("mod.harmony.dellartedellaguerra");
        private readonly CampaignBehaviourDisabler _campaignBehaviourDisabler = new ();

        public override void OnBeforeMissionBehaviorInitialize(Mission mission)
        {
            base.OnBeforeMissionBehaviorInitialize(mission);
            DadgMission.Decorate(mission, Campaign.Current.CampaignBehaviorManager);
        }

        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            InfoPrinter.Display("DADG loaded");
        }

        // load the harmony patches once as soon as possible before reaching the main menu 
        protected override void OnSubModuleLoad()
        {
            try
            {
                PocConfigReaderPatch.Patch(Harmony);
                Harmony.PatchAll();
            } catch (Exception e)
            {
                Logger.Error("Harmony patches failed: {}", e);
            }
        }

        protected override void OnSubModuleUnloaded()
        {
            LogManager.Shutdown();
            DadgConfigWatcher.Destroy();
        }

        protected override void InitializeGameStarter(Game game, IGameStarter starterObject)
        {
            game.AddGameHandler<ShaderCompilationNotifier<DadgConfigWatcher>>();

            if (game.GameType is not Campaign || starterObject is not CampaignGameStarter campaignGameStarter) return;

            campaignGameStarter.AddBehavior(new NobleOrphanChildrenCampaignBehaviour());
            campaignGameStarter.AddBehavior(new TroopEquipmentPoolsCampaignBehaviour());
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
                Logger.Warn("Could not find DADG battle scenes at {}. Using SandBox's battle scenes instead", battleScenesFilePath);
                return;
            }

            try
            {
                // Overrides sandbox battle scenes
                GameSceneDataManager.Instance?.LoadSPBattleScenes(battleScenesFilePath);   
            } catch (XmlException e)
            {
                Logger.Error("Failed to load {}: {}" , battleScenesFilePath, e);
            }
        }
    }
}
