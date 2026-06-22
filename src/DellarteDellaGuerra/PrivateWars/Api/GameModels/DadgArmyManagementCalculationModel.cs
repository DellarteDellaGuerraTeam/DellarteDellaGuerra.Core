using System.Collections.Generic;
using DellarteDellaGuerra.Titles.Api;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;

namespace DellarteDellaGuerra.PrivateWars.Api.GameModels
{
    // Keeps a private-war enemy out of the belligerent's army candidate pool (design §4.1).
    //
    // The base builds its call list from leaderParty.MapFaction.WarPartyComponents — the whole
    // kingdom — so a same-kingdom feud enemy (C) is auto-invited into the attacker's (A's) army,
    // which is nonsensical while the two are at private war. Drop any candidate whose clan is at
    // private war with the army leader's clan; everyone else is returned unchanged (stock answer
    // when no war, design's pair-scoped contract).
    //
    // This is the army-leader's "who do I invite" decision, not a member's "should I quit" decision
    // (the latter stays a scored AI vote in AiArmyMemberBehavior / PrivateWarCampaignBehavior).
    public class DadgArmyManagementCalculationModel : DefaultArmyManagementCalculationModel
    {
        public override List<MobileParty> GetMobilePartiesToCallToArmy(MobileParty leaderParty)
        {
            var parties = base.GetMobilePartiesToCallToArmy(leaderParty);

            var leaderClan = leaderParty?.LeaderHero?.Clan;
            if (leaderClan is null || !FeudalServices.IsInitialised || FeudalServices.PrivateWarHostility is null)
                return parties;

            parties.RemoveAll(party =>
            {
                var candidateClan = party?.ActualClan;
                return candidateClan != null
                    && FeudalServices.PrivateWarHostility.AreEnemies(leaderClan.StringId, candidateClan.StringId);
            });

            return parties;
        }
    }
}
