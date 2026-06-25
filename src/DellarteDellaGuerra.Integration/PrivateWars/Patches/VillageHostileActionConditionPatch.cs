using System.Reflection;
using Harmony.DependencyInjection.Patches;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace DellarteDellaGuerra.Integration.PrivateWars.Patches
{
    // Show the "Take a hostile action" option on the village menu for same-kingdom private-war rivals.
    //
    // VillageHostileActionCampaignBehavior.game_menu_village_hostile_action_on_condition gates the
    // "village" menu "hostile_action" option. The method returns false when MapFaction is shared
    // (same kingdom), so the "Take a hostile action" button never appears. This postfix re-enables
    // it when the village's bound-town owner is a registered private-war enemy.
    public class VillageHostileActionConditionPatch : IPatch
    {
        public MethodInfo? TargetMethod =>
            AccessTools.Method(typeof(VillageHostileActionCampaignBehavior),
                "game_menu_village_hostile_action_on_condition");

        public MethodInfo? PatchMethod =>
            AccessTools.Method(typeof(VillageHostileActionConditionPatch), nameof(Postfix));

        public PatchType PatchType => PatchType.Postfix;

        private static void Postfix(ref bool __result, MenuCallbackArgs args)
        {
            if (__result) return;
            var cs = Settlement.CurrentSettlement;
            if (cs is null || !cs.IsVillage) return;
            var village = cs.Village;
            if (village is null) return;
            // village.Owner is the bound Town; its OwnerClan is the relevant clan for the war check.
            var ownerClan = village.Owner?.Settlement?.OwnerClan;
            if (!PrivateWarPatchHelper.AreEnemies(Hero.MainHero?.Clan, ownerClan)) return;
            if (MobileParty.MainParty.Army != null && MobileParty.MainParty.Army.LeaderParty != MobileParty.MainParty) return;

            args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
            __result = village.VillageState == Village.VillageStates.Normal;
        }
    }
}
