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
    // Show the "Besiege" option in the town/castle-outside menu for same-kingdom private-war rivals.
    //
    // EncounterGameMenuBehavior.game_menu_town_town_besiege_on_condition gates the option on
    // FactionManager.IsAtWarAgainstFaction(Hero.MainHero.MapFaction, Settlement.CurrentSettlement.MapFaction).
    // Two clans in the same kingdom share a MapFaction, so the check is always false — the option
    // never appears. This postfix re-enables the option when the settlement owner is a registered
    // private-war enemy. The optionLeaveType is already set by the vanilla method before the gate.
    public class BesiegeMenuConditionPatch : IPatch
    {
        public MethodInfo? TargetMethod =>
            AccessTools.Method(typeof(EncounterGameMenuBehavior),
                "game_menu_town_town_besiege_on_condition");

        public MethodInfo? PatchMethod =>
            AccessTools.Method(typeof(BesiegeMenuConditionPatch), nameof(Postfix));

        public PatchType PatchType => PatchType.Postfix;

        private static void Postfix(ref bool __result, MenuCallbackArgs args)
        {
            if (__result) return;
            var cs = Settlement.CurrentSettlement;
            if (cs is null || PartyBase.MainParty.NumberOfHealthyMembers <= 0) return;
            if (!PrivateWarPatchHelper.AreEnemies(Hero.MainHero?.Clan, cs.OwnerClan)) return;
            args.optionLeaveType = GameMenuOption.LeaveType.BesiegeTown;
            __result = !cs.IsUnderSiege;
        }
    }
}
