using DellarteDellaGuerra.Domain.PrivateWars;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;

namespace DellarteDellaGuerra.PrivateWars.Api.GameModels
{
    public class DadgVolunteerModel : DefaultVolunteerModel
    {
        private readonly PrivateWarInteractionPolicy _interactionPolicy;

        public DadgVolunteerModel(PrivateWarInteractionPolicy interactionPolicy)
        {
            _interactionPolicy = interactionPolicy;
        }

        public override int MaximumIndexHeroCanRecruitFromHero(
            Hero buyerHero,
            Hero sellerHero,
            int useValueAsRelation = -101)
        {
            var vanillaMaximumIndex = base.MaximumIndexHeroCanRecruitFromHero(
                buyerHero,
                sellerHero,
                useValueAsRelation);
            var ownerClan = sellerHero.CurrentSettlement?.OwnerClan;
            var arePrivateEnemies = PrivateWarSiegeDefenderPolicy.AreEnemies(buyerHero.Clan, ownerClan);

            return _interactionPolicy.RestrictRecruitableIndex(
                arePrivateEnemies,
                vanillaMaximumIndex);
        }
    }
}
