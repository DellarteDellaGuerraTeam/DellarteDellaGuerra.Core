using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.Church.Ledger;
using DellarteDellaGuerra.Domain.Church.Port;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace DellarteDellaGuerra.Church
{
    public class BannerlordChurchLedgerWorld : IChurchLedgerWorld
    {
        private readonly ChurchSettlements _churchSettlements;

        public BannerlordChurchLedgerWorld(ChurchSettlements churchSettlements)
        {
            _churchSettlements = churchSettlements;
        }

        public ChurchFoundationFacts? GetFoundation(string settlementId)
        {
            if (Campaign.Current is null) return null;

            var settlement = Settlement.Find(settlementId);
            if (settlement is null) return null;

            var clergy = settlement.Notables.FirstOrDefault(notable => notable.IsPreacher && notable.IsAlive);
            return new ChurchFoundationFacts(
                settlement.Name.ToString(),
                clergy?.Name.ToString(),
                clergy?.IsFemale == true,
                clergy is null || Hero.MainHero is null
                    ? (int?)null
                    : CharacterRelationManager.GetHeroRelation(Hero.MainHero, clergy),
                clergy?.Power,
                settlement.OwnerClan?.Name.ToString(),
                settlement.MapFaction?.Name.ToString(),
                _churchSettlements.IsShrine(settlement),
                settlement.IsUnderRaid);
        }

        public IReadOnlyCollection<int> GetLivingClergyRelations()
        {
            var relations = new List<int>();
            if (Campaign.Current is null || Hero.MainHero is null) return relations;

            foreach (var settlement in Settlement.All)
            {
                if (!_churchSettlements.IsChurchSettlement(settlement)) continue;

                foreach (var clergy in settlement.Notables)
                {
                    if (!clergy.IsPreacher || !clergy.IsAlive) continue;

                    // This is the raw store ChangeRelationAction writes. Hero.GetRelation would
                    // add personality trait effects and disagree with the effects of donations.
                    relations.Add(CharacterRelationManager.GetHeroRelation(Hero.MainHero, clergy));
                }
            }

            return relations;
        }
    }
}
