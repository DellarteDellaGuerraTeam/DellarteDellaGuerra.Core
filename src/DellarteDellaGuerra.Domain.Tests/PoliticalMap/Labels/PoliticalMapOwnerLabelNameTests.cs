using DellarteDellaGuerra.Domain.PoliticalMap.Labels;

namespace DellarteDellaGuerra.Domain.Tests.PoliticalMap.Labels
{
    public class PoliticalMapOwnerLabelNameTests
    {
        [Fact]
        public void TheRulersOwnLandUsesItsLandedTitleLikeAnyOtherOwner()
        {
            Assert.Equal("Duchy of Lancaster", PoliticalMapOwnerLabelName.Resolve(
                PoliticalMapLabelMode.Titles, "Duchy of Lancaster", "Lancaster", "London"));
        }

        [Fact]
        public void AnUntitledEstateUsesItsPlaceNameWithoutInventingATitle()
        {
            Assert.Equal("Corfe Castle", PoliticalMapOwnerLabelName.Resolve(
                PoliticalMapLabelMode.Titles, null, "Stourton", "Corfe Castle"));
        }

        [Fact]
        public void ARegisteredTitleTakesPrecedenceOverTheEstateName()
        {
            Assert.Equal("Duchy of Somerset", PoliticalMapOwnerLabelName.Resolve(
                PoliticalMapLabelMode.Titles, "Duchy of Somerset", "Beaufort", "Corfe Castle"));
        }

        [Fact]
        public void ClanModeAlwaysUsesTheClanName()
        {
            Assert.Equal("Lancaster", PoliticalMapOwnerLabelName.Resolve(
                PoliticalMapLabelMode.Clans, "Duchy of Lancaster", "Lancaster", "London"));
        }

        [Fact]
        public void MissingTitleAndEstateDoNotSilentlySwitchBackToClanMode()
        {
            Assert.Null(PoliticalMapOwnerLabelName.Resolve(
                PoliticalMapLabelMode.Titles, null, "Stourton", null));
        }
    }
}
