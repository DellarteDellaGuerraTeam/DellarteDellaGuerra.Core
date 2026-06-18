using System.Reflection;
using DellarteDellaGuerra.Titles.Api;
using Harmony.DependencyInjection.Patches;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;

namespace DellarteDellaGuerra.Integration.PrivateWars.Patches
{
    // Keep a private-war captive held (design §4.5 point 3 / plan §9 — the AI auto-end hazard).
    // PrisonerReleaseCampaignBehavior frees any prisoner whose MapFaction !IsAtWarWith the captor's
    // MapFaction; for a same-kingdom pair that test is always false, so the moment a fief changes
    // hands or the kingdom makes peace, the captive evaporates and the war's prisoner score with it.
    // All four of those release sites funnel through EndCaptivityAction.ApplyInternal, so a single
    // prefix there is the surgical fix: skip only the two involuntary "no longer at war" auto-releases
    // (ReleasedAfterPeace / ReleasedAfterBattle) of a hero whose clan is at private war with its captor.
    // Escape, ransom, deliberate release, death and real-war peace all fall through to vanilla.
    // The player's own captivity is a separate hazard (PlayerCaptivityCampaignBehavior, phase 4); the
    // main hero is left to vanilla here.
    public class PrivateWarPrisonerRetentionPatch : IPatch
    {
        public MethodInfo? TargetMethod =>
            AccessTools.Method(typeof(EndCaptivityAction), "ApplyInternal");

        public MethodInfo? PatchMethod =>
            AccessTools.Method(typeof(PrivateWarPrisonerRetentionPatch), nameof(SkipPrivateWarRelease));

        public PatchType PatchType => PatchType.Prefix;

        private static bool SkipPrivateWarRelease(Hero prisoner, EndCaptivityDetail detail)
        {
            if (prisoner is null || prisoner == Hero.MainHero) return true;
            if (detail != EndCaptivityDetail.ReleasedAfterPeace && detail != EndCaptivityDetail.ReleasedAfterBattle)
                return true;

            var captorParty = prisoner.PartyBelongedToAsPrisoner;
            var captorClan = captorParty?.MobileParty?.ActualClan ?? captorParty?.Settlement?.OwnerClan;

            // Block the auto-release (return false) only for a genuine private-war captor/captive pair.
            return !PrivateWarPatchHelper.AreEnemies(prisoner.Clan, captorClan);
        }
    }
}
