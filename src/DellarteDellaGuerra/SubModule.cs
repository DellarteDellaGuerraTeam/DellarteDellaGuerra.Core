using System;
using System.IO;
using System.Reflection;
using System.Xml;
using DellarteDellaGuerra.Configuration.Providers;
using DellarteDellaGuerra.DadgCampaign;
using DellarteDellaGuerra.GameManager;
using DellarteDellaGuerra.Logging;
using DellarteDellaGuerra.Utils;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;
using NLog;

namespace DellarteDellaGuerra
{
    public class SubModule : MBSubModuleBase
    {
        private static readonly Logger Logger = LoggerFactory.GetLogger<SubModule>();
        private static readonly Harmony Harmony = new ("mod.harmony.dellartedellaguerra");
        private readonly CampaignBehaviourDisabler _campaignBehaviourDisabler = new();

        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            InfoPrinter.Display("DADG loaded");
        }

        protected override void OnSubModuleLoad()
        {
            SetCampaignStartingDate();
            try
            {
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

        public override void OnGameInitializationFinished(Game game)
        {
            base.OnGameInitializationFinished(game);
            game.AddGameHandler<ShaderCompilationNotifier<DadgConfigWatcher>>();
            _campaignBehaviourDisabler.Disable(Campaign.Current.CampaignBehaviorManager);
            LoadDadgBattleScenes();
        }

        private void SetCampaignStartingDate()
        {
            CampaignTime startTime = CampaignTime.Years(1471) + CampaignTime.Weeks(4) + CampaignTime.Days(1);
            typeof(CampaignData).GetField("CampaignStartTime",BindingFlags.Static|BindingFlags.Public)?.SetValue(null,startTime);
        }

        private void LoadDadgBattleScenes()
        {
            var battleScenesFilePath = $"{ ModuleHelper.GetModuleFullPath("DellarteDellaGuerra") }ModuleData/dadg_battle_scenes.xml";
            if (!File.Exists(battleScenesFilePath))
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