using DellarteDellaGuerra.PrivateWars.Api.GameModels;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;

namespace DellarteDellaGuerra.Tests.PrivateWars;

public class PrivateWarModelSeamTests
{
    [Fact]
    public void EncounterModel_OverridesNonAttachedNpcReinforcementEnumeration()
    {
        var method = typeof(DadgEncounterModel).GetMethod(
            nameof(DadgEncounterModel.FindNonAttachedNpcPartiesWhoWillJoinPlayerEncounter),
            [typeof(List<MobileParty>), typeof(List<MobileParty>)]);

        Assert.NotNull(method);
        Assert.Equal(typeof(DadgEncounterModel), method!.DeclaringType);
    }

    [Fact]
    public void VolunteerModel_OverridesRecruitableIndex()
    {
        var method = typeof(DadgVolunteerModel).GetMethod(
            nameof(DadgVolunteerModel.MaximumIndexHeroCanRecruitFromHero),
            [typeof(Hero), typeof(Hero), typeof(int)]);

        Assert.NotNull(method);
        Assert.Equal(typeof(DadgVolunteerModel), method!.DeclaringType);
    }
}
