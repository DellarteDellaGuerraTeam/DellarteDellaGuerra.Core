using System.Reflection;
using Harmony.DependencyInjection.Patches;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace DellarteDellaGuerra.Integration.PrivateWars.Patches
{
    // Show the "Continue siege preparations" option during a same-kingdom private-war siege encounter.
    //
    // EncounterGameMenuBehavior.game_menu_town_besiege_continue_siege_on_condition gates the option on
    // FactionManager.IsAtWarAgainstFaction(Hero.MainHero.MapFaction, encounteredParty.MapFaction).
    // For a same-kingdom pair that is always false, so the option disappears — leaving the player
    // with no way to continue a valid private-war siege. This postfix re-enables the option using the
    // same non-faction conditions as vanilla, replacing the faction gate with an AreEnemies check.
    public class ContinueSiegeMenuConditionPatch : IPatch
    {
        public MethodInfo? TargetMethod =>
            AccessTools.Method(typeof(EncounterGameMenuBehavior),
                "game_menu_town_besiege_continue_siege_on_condition");

        public MethodInfo? PatchMethod =>
            AccessTools.Method(typeof(ContinueSiegeMenuConditionPatch), nameof(Postfix));

        public PatchType PatchType => PatchType.Postfix;

        private static void Postfix(ref bool __result, MenuCallbackArgs args)
        {
            if (__result) return;

            PartyBase? encounteredParty = PlayerEncounter.EncounteredParty;
            if (encounteredParty is null) return;

            MapEvent? encounteredBattle = PlayerEncounter.EncounteredBattle;
            if (encounteredBattle is null) return;
            if (encounteredBattle.GetLeaderParty(PartyBase.MainParty.Side) != PartyBase.MainParty) return;
            if (!encounteredParty.IsSettlement) return;

            var settlement = encounteredParty.Settlement;
            if (!settlement.IsFortification) return;
            if (!PrivateWarPatchHelper.AreEnemies(Hero.MainHero?.Clan, settlement.OwnerClan)) return;
            if (!settlement.IsUnderSiege) return;

            __result = settlement.CurrentSiegeState == Settlement.SiegeState.OnTheWalls;
        }
    }
}
