using DellarteDellaGuerra.PrivateWars.Api.Interactions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.CampaignBehaviors.AiBehaviors;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace DellarteDellaGuerra.Tests.PrivateWars;

public class PrivateWarInteractionPatchTargetTests
{
    [Fact]
    public void RecruitmentEntryTarget_ResolvesExactPublicCallback()
    {
        var target = RecruitmentEntryPatchTarget.Resolve();

        Assert.Equal(typeof(RecruitmentCampaignBehavior), target.DeclaringType);
        Assert.Equal(nameof(RecruitmentCampaignBehavior.OnBeforeSettlementEntered), target.Name);
        Assert.Equal(
            [typeof(MobileParty), typeof(Settlement), typeof(Hero)],
            target.GetParameters().Select(parameter => parameter.ParameterType));
    }

    [Fact]
    public void SettlementVisitTarget_ResolvesExactPrivateStaticPredicate()
    {
        var target = SettlementVisitPatchTarget.Resolve();

        Assert.Equal(typeof(AiVisitSettlementBehavior), target.DeclaringType);
        Assert.Equal("IsSettlementSuitableForVisitingCondition", target.Name);
        Assert.True(target.IsStatic);
        Assert.False(target.IsPublic);
        Assert.Equal(
            [typeof(MobileParty), typeof(Settlement)],
            target.GetParameters().Select(parameter => parameter.ParameterType));
    }
}
