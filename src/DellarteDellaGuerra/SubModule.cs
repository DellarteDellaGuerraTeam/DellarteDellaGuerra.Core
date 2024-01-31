using System;
using System.IO;
using System.Reflection;
using System.Xml;
using DellarteDellaGuerra.DadgCampaign;
using DellarteDellaGuerra.Logging;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;
using NLog;

namespace DellarteDellaGuerra
{
    public class SubModule : MBSubModuleBase
    {
        private static readonly NLog.Logger Logger = LoggerFactory.GetLogger<SubModule>();
        private static readonly Harmony Harmony = new ("mod.harmony.dellartedellaguerra");
        private readonly CampaignBehaviourDisabler _campaignBehaviourDisabler;

        public SubModule()
        {
            _campaignBehaviourDisabler = new CampaignBehaviourDisabler();
        }
    
        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            InformationManager.DisplayMessage(new InformationMessage("DADG loaded", new Color(134, 114, 250)));
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

        public override void OnGameInitializationFinished(Game game)
        {
            base.OnGameInitializationFinished(game);
            LoadDadgBattleScenes();
            _campaignBehaviourDisabler.Disable(Campaign.Current.CampaignBehaviorManager);
        }

        protected override void OnSubModuleUnloaded()
        {
            base.OnSubModuleUnloaded();
            LogManager.Shutdown();
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