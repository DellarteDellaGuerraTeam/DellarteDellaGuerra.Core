using System.Reflection;
using Harmony.DependencyInjection.Patches;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;

namespace DellarteDellaGuerra.Integration.PrivateWars.Patches
{
    // Show the "Attack army" option in the army_encounter menu for same-kingdom private-war rivals.
    //
    // EncounterGameMenuBehavior.game_menu_army_attack_on_condition returns
    // MobileParty.MainParty.MapFaction.IsAtWarWith(PlayerEncounter.EncounteredMobileParty.MapFaction).
    // Two clans in the same kingdom share a MapFaction, so the result is always false — the "Attack
    // army" button never appears. This postfix enables the option when the encountered party's clan
    // is a registered private-war enemy of the player's clan.
    public class ArmyAttackMenuConditionPatch : IPatch
    {
        public MethodInfo? TargetMethod =>
            AccessTools.Method(typeof(EncounterGameMenuBehavior),
                "game_menu_army_attack_on_condition");

        public MethodInfo? PatchMethod =>
            AccessTools.Method(typeof(ArmyAttackMenuConditionPatch), nameof(Postfix));

        public PatchType PatchType => PatchType.Postfix;

        private static void Postfix(ref bool __result, MenuCallbackArgs args)
        {
            if (__result) return;
            var enc = PlayerEncounter.EncounteredMobileParty;
            if (enc is null) return;
            if (!PrivateWarPatchHelper.AreEnemies(Hero.MainHero?.Clan, enc.ActualClan)) return;
            __result = true;
        }
    }
}
