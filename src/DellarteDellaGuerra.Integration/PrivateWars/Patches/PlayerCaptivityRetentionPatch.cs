using System.Reflection;
using DellarteDellaGuerra.Titles.Api;
using Harmony.DependencyInjection.Patches;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;

namespace DellarteDellaGuerra.Integration.PrivateWars.Patches
{
    // Keep the player captive when held by a same-kingdom private-war rival (design §4.2 AUTO-END
    // risk #2). PlayerCaptivityCampaignBehavior.CheckCaptivityChange (called each tick while the
    // player is a prisoner) checks !FactionManager.IsAtWarAgainstFaction(captorMapFaction, mainMapFaction).
    // For a same-kingdom pair both parties share one MapFaction → the test is always false, and the
    // method's third branch immediately routes the player to "menu_captivity_end_no_more_enemies" and
    // releases them on the next tick. A Prefix that returns false (skip the method body) when an
    // Active private war exists between the player's clan and the captor's clan prevents that automatic
    // release. All player-initiated escape paths go through menu consequences (EndCaptivityAction.ApplyByEscape)
    // and are unaffected. When the war concludes AreEnemies returns false → vanilla resumes → the
    // engine releases the player naturally via the same "no more enemies" menu.
    public class PlayerCaptivityRetentionPatch : IPatch
    {
        public MethodInfo? TargetMethod =>
            AccessTools.Method(typeof(PlayerCaptivityCampaignBehavior), nameof(PlayerCaptivityCampaignBehavior.CheckCaptivityChange));

        public MethodInfo? PatchMethod =>
            AccessTools.Method(typeof(PlayerCaptivityRetentionPatch), nameof(SkipIfPrivateWarCaptive));

        public PatchType PatchType => PatchType.Prefix;

        // Returns false (skip vanilla) only while the player is held by a private-war rival,
        // suppressing the automatic "no more enemies" release. Returns true in all other situations
        // so vanilla captivity logic (ransom, time-based escape) proceeds normally.
        private static bool SkipIfPrivateWarCaptive()
        {
            if (!FeudalServices.IsInitialised || FeudalServices.PrivateWarHostility is null) return true;

            var captorParty = Hero.MainHero.PartyBelongedToAsPrisoner;
            if (captorParty is null) return true;

            var captorClan = captorParty.MobileParty?.ActualClan ?? captorParty.Settlement?.OwnerClan;
            if (captorClan is null) return true;

            // Return false (skip the method body) while an active private war keeps these two clans
            // as enemies. Once the war concludes AreEnemies returns false and vanilla runs again.
            return !PrivateWarPatchHelper.AreEnemies(Hero.MainHero.Clan, captorClan);
        }
    }
}
