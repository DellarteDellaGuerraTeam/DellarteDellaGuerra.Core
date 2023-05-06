﻿using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace DADG_Core
{
    public class SubModule : MBSubModuleBase
    {
        public static Harmony HarmonyInstance { get; private set; }
        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            InformationManager.DisplayMessage(new InformationMessage("DADG loaded", new TaleWorlds.Library.Color(134, 114, 250)));
        }
        protected override void OnSubModuleLoad()
        {
            CampaignTime startTime = CampaignTime.Years(1471)+CampaignTime.Weeks(5)+CampaignTime.Days(4);
            typeof(CampaignData).GetField("CampaignStartTime",BindingFlags.Static|BindingFlags.Public)?.SetValue(null,startTime);
            
            HarmonyInstance = new Harmony("mod.harmony.dellartedellaguerra");
            HarmonyInstance.PatchAll();
        }

        
    }
}