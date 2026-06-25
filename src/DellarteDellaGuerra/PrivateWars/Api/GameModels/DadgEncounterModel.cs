using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace DellarteDellaGuerra.PrivateWars.Api.GameModels
{
    // Re-include the same-kingdom defenders the vanilla candidacy filter drops in a private-war siege.
    //
    // DefaultEncounterModel.GetDefenderPartiesOfSettlement delegates to Town.GetDefenderParties, which
    // yields a stationed party only when party.MapFaction.IsAtWarWith(BesiegerCamp.MapFaction). Two clans
    // in the same kingdom share a MapFaction (design §2), so the garrison party, the militia party and any
    // feud-belligerent lord party inside the town are all filtered out — the assault starts with an empty
    // defender side.
    //
    // This override is GATE 1 of the two-gate defender-population path: it puts those parties back into the
    // candidate list when the besieger is a registered private-war enemy of the settlement owner. GATE 2 —
    // assignment of a candidate to DefenderSide — is SiegeEvent.CanPartyJoinSide, a non-virtual engine
    // method with no model seam, handled by SiegeDefenderJoinPatch. Both are load-bearing; neither suffices
    // alone. When no private war is active this returns exactly the vanilla set.
    public class DadgEncounterModel : DefaultEncounterModel
    {
        public override IEnumerable<PartyBase> GetDefenderPartiesOfSettlement(
            Settlement settlement, MapEvent.BattleTypes mapEventType)
        {
            var seen = new HashSet<PartyBase>();
            var baseParties = base.GetDefenderPartiesOfSettlement(settlement, mapEventType);
            if (baseParties != null)
            {
                foreach (var party in baseParties)
                {
                    seen.Add(party);
                    yield return party;
                }
            }

            if (!settlement.IsFortification) yield break;

            var besiegerClan = settlement.SiegeEvent?.BesiegerCamp?.LeaderParty?.ActualClan;
            if (!PrivateWarSiegeDefenderPolicy.AreEnemies(besiegerClan, settlement.OwnerClan)) yield break;

            foreach (var mobileParty in settlement.Parties)
            {
                var party = mobileParty.Party;
                if (party is null || seen.Contains(party)) continue;
                if (!mobileParty.IsActive || mobileParty.IsCaravan || mobileParty.IsVillager) continue;

                // The owner's garrison/militia, plus any feud-belligerent lord party inside the town, defend
                // the walls. PrivateWarSiegeDefenderPolicy.IsDefender is the shared rule the assault-side
                // patch (SiegeEvent.CanPartyJoinSide) consults too; neutral same-kingdom parties fall out.
                if (PrivateWarSiegeDefenderPolicy.IsDefender(mobileParty, besiegerClan, settlement, mapEventType))
                {
                    seen.Add(party);
                    yield return party;
                }
            }
        }

        // GATE 1, second seam. Settlement (the defender-side ISiegeEventSide) exposes its involved parties
        // through TWO independent EncounterModel methods: GetInvolvedPartiesForEventType delegates to
        // GetDefenderPartiesOfSettlement (overridden above), while HasInvolvedPartyForEventType and
        // GetNextInvolvedPartyForEventType both loop on GetNextDefenderPartyOfSettlement. The siege-map
        // overlay (EncounterMenuOverlayVM) buckets a party as a defender via HasInvolvedPartyForEventType,
        // so without this override the garrison/militia appear in the involved list yet fail the defender
        // test and render in the besieger (player) panel. Enumerate the same augmented set here to keep the
        // two seams consistent. When no private war applies this defers to the vanilla iterator unchanged.
        public override PartyBase GetNextDefenderPartyOfSettlement(
            Settlement settlement, ref int partyIndex, MapEvent.BattleTypes mapEventType)
        {
            var besiegerClan = settlement.SiegeEvent?.BesiegerCamp?.LeaderParty?.ActualClan;
            if (!settlement.IsFortification ||
                !PrivateWarSiegeDefenderPolicy.AreEnemies(besiegerClan, settlement.OwnerClan))
                return base.GetNextDefenderPartyOfSettlement(settlement, ref partyIndex, mapEventType);

            partyIndex++;
            var i = 0;
            foreach (var party in GetDefenderPartiesOfSettlement(settlement, mapEventType))
            {
                if (i == partyIndex) return party;
                i++;
            }
            return null;
        }
    }
}
