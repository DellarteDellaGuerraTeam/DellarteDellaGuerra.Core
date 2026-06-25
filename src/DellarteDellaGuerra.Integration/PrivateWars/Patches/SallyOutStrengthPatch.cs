using System.Reflection;
using Harmony.DependencyInjection.Patches;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace DellarteDellaGuerra.Integration.PrivateWars.Patches
{
    // Fix the sally-out strength check for same-kingdom private-war besiegers.
    //
    // SallyOutsCampaignBehavior.CheckSallyOut accumulates besieger-side strength only when
    //   mobileParty.MapFaction.IsAtWarWith(settlement.Party.MapFaction)
    // For a same-kingdom private-war besieger this is always false, so besieger strength = 0
    // and the garrison sallies unconditionally (even 1 man beats a 280-man army by the ratio
    // test "garrisonStrength > 0 * 2f").
    //
    // This prefix intercepts when the siege besieger is a private-war enemy of the settlement
    // owner. It replicates the vanilla strength comparison using siege-camp membership
    // (BesiegerCamp.IsBesiegerSideParty) as the besieger discriminator instead of MapFaction
    // equality, which breaks in a same-kingdom private war where both sides share a MapFaction.
    //   - Garrison NOT stronger: suppress the sally (salliedOut = false, skip original).
    //   - Garrison IS stronger: let original run (it will naturally reach the same conclusion
    //     and start the sally, which is the correct outcome).
    public class SallyOutStrengthPatch : IPatch
    {
        private const float SallyOutPowerRatioForHelpingReliefForce = 1.5f;
        private const float SallyOutPowerRatio = 2f;

        public MethodInfo? TargetMethod =>
            AccessTools.Method(typeof(SallyOutsCampaignBehavior), "CheckSallyOut");

        public MethodInfo? PatchMethod =>
            AccessTools.Method(typeof(SallyOutStrengthPatch), nameof(Prefix));

        public PatchType PatchType => PatchType.Prefix;

        // Returns false = skip original. Returns true = run original.
        private static bool Prefix(Settlement settlement, bool checkForNavalSallyOut, ref bool salliedOut)
        {
            // Only intercept when this is a private-war siege (same-kingdom enemies).
            var siegeEvent = settlement.SiegeEvent;
            if (siegeEvent is null) return true;

            var besiegerClan = siegeEvent.BesiegerCamp?.LeaderParty?.ActualClan;
            var ownerClan = settlement.OwnerClan;
            if (!PrivateWarPatchHelper.AreEnemies(besiegerClan, ownerClan)) return true;

            // Mirror vanilla's flag/flag2 to pick the same ratio it would pick.
            var leaderMapEvent = siegeEvent.BesiegerCamp!.LeaderParty!.MapEvent;
            bool isSiegeOutside = leaderMapEvent?.IsSiegeOutside ?? false;
            bool isBlockade = leaderMapEvent?.IsBlockade ?? false;

            // Early-out mirrors vanilla: wrong map-event type for this sallyOut variant.
            if ((isBlockade && !checkForNavalSallyOut) || (isSiegeOutside && checkForNavalSallyOut))
                return true;

            // Private-war besieger detected. Replicate vanilla's strength scan.
            // num  = besieger-side strength (parties that belong to the besieger camp)
            // num2 = garrison-relief strength (same-faction, non-private-war-enemy parties)
            // num3 = garrison strength (already-involved defender parties)
            var context = checkForNavalSallyOut
                ? MapEvent.PowerCalculationContext.SeaBattle
                : MapEvent.PowerCalculationContext.PlainBattle;
            var garrisonEventType = checkForNavalSallyOut
                ? MapEvent.BattleTypes.BlockadeSallyOutBattle
                : MapEvent.BattleTypes.SallyOut;

            float num3 = 0f;
            foreach (var party in settlement.GetInvolvedPartiesForEventType(garrisonEventType))
                num3 += party.GetCustomStrength(BattleSideEnum.Attacker, context);

            float num = 0f;
            float num2 = 0f;

            // Vanilla scans parties within GetEncounterJoiningRadius of the besieger camp.
            var leaderPos = siegeEvent.BesiegerCamp.LeaderParty.Position.ToVec2();
            float radius = Campaign.Current.Models.EncounterModel.GetEncounterJoiningRadius;
            var data = MobileParty.StartFindingLocatablesAroundPosition(leaderPos, radius);

            for (var mp = MobileParty.FindNextLocatable(ref data); mp != null; mp = MobileParty.FindNextLocatable(ref data))
            {
                if (mp.CurrentSettlement != null || !(mp.Aggressiveness > 0f)) continue;

                float aggrMult = mp.Aggressiveness > 0.5f ? 1f : mp.Aggressiveness * 2f;

                if (siegeEvent.BesiegerCamp.IsBesiegerSideParty(mp))
                {
                    // Party is in the besieger camp (leader + attached parties).
                    num += aggrMult * mp.Party.GetCustomStrength(BattleSideEnum.Defender, context);
                }
                else if (mp.MapFaction == settlement.MapFaction
                         && !PrivateWarPatchHelper.AreEnemies(mp.ActualClan, settlement.OwnerClan))
                {
                    // Same faction as settlement and NOT a private-war enemy of the owner
                    // → counts as garrison relief.
                    num2 += aggrMult * mp.Party.GetCustomStrength(BattleSideEnum.Attacker, context);
                }
            }

            float sallySideStrength = num3 + num2;
            float ratio = (isSiegeOutside || isBlockade) ? SallyOutPowerRatioForHelpingReliefForce : SallyOutPowerRatio;
            bool garrisonWouldSally = sallySideStrength > num * ratio;

            if (!garrisonWouldSally)
            {
                // Besieger is strong enough — suppress the sally and skip the original.
                salliedOut = false;
                return false;
            }

            // Garrison outpowers the besieger — let vanilla run. Vanilla will compute num=0 (its
            // IsAtWarWith is false for same-kingdom) so it will ALWAYS decide to sally in this
            // branch, which is what we want here (garrison genuinely stronger).
            return true;
        }
    }
}
