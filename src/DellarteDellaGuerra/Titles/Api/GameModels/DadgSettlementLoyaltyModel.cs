using System.Linq;
using DellarteDellaGuerra.Domain.Titles.Model;
using DellarteDellaGuerra.Titles.Api;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;

namespace DellarteDellaGuerra.Titles.Api.GameModels
{
    // Adds a de jure loyalty modifier: settlements held by their rightful titular lord
    // gain +1 loyalty per day; settlements where a de jure claimant exists but is not
    // the current holder lose -1 loyalty per day.
    public class DadgSettlementLoyaltyModel : DefaultSettlementLoyaltyModel
    {
        public override ExplainedNumber CalculateLoyaltyChange(Town town, bool includeDescriptions = false)
        {
            var result = base.CalculateLoyaltyChange(town, includeDescriptions);
            if (!FeudalServices.IsInitialised || town.Settlement?.OwnerClan is null) return result;

            string settlementId = town.Settlement.StringId;
            var title = FeudalServices.Titles?.GetTitleBySeat(settlementId);
            if (title is null) return result;

            bool holderHasDeJureClaim = title.HolderClanId is not null
                && FeudalServices.Claims?
                    .GetClaimsOn(title.Id)
                    .Any(c => c.ClaimantClanId == title.HolderClanId
                               && c.Strength == ClaimStrength.DeJure)
                == true;

            if (holderHasDeJureClaim)
            {
                result.Add(1f, new TextObject("De Jure Holder"));
            }
            else if (title.HolderClanId is not null)
            {
                bool anyDeJureClaim = FeudalServices.Claims?
                    .GetClaimsOn(title.Id)
                    .Any(c => c.Strength == ClaimStrength.DeJure) == true;
                if (anyDeJureClaim)
                    result.Add(-1f, new TextObject("Contested Title"));
            }

            return result;
        }
    }
}
