using System.Reflection;
using Harmony.DependencyInjection.Patches;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;

namespace DellarteDellaGuerra.Integration.PrivateWars.Patches
{
    // Show the "Raid the village" button inside the village_hostile_action submenu for private-war rivals.
    //
    // VillageHostileActionCampaignBehavior.game_menu_village_hostile_action_raid_village_on_condition returns
    // !DiplomacyHelper.IsSameFactionAndNotEliminated(Hero.MainHero.MapFaction, Settlement.CurrentSettlement.MapFaction).
    // Two clans in the same kingdom share a MapFaction, so IsSameFactionAndNotEliminated is true → the
    // "Raid the village" button is hidden. This postfix re-enables it when the settlement's owner is a
    // registered private-war enemy.
    public class VillageRaidConditionPatch : IPatch
    {
        public MethodInfo? TargetMethod =>
            AccessTools.Method(typeof(VillageHostileActionCampaignBehavior),
                "game_menu_village_hostile_action_raid_village_on_condition");

        public MethodInfo? PatchMethod =>
            AccessTools.Method(typeof(VillageRaidConditionPatch), nameof(Postfix));

        public PatchType PatchType => PatchType.Postfix;

        private static void Postfix(ref bool __result, MenuCallbackArgs args)
        {
            if (__result) return;
            var cs = Settlement.CurrentSettlement;
            if (cs is null || !cs.IsVillage) return;
            var ownerClan = cs.Village?.Owner?.Settlement?.OwnerClan;
            if (!PrivateWarPatchHelper.AreEnemies(Hero.MainHero?.Clan, ownerClan)) return;

            args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
            __result = true;
        }
    }
}
