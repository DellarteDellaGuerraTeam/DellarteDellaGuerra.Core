using DellarteDellaGuerra.Domain.Titles.Model;

namespace DellarteDellaGuerra.Domain.Tests.Titles
{
    public class TitleTests
    {
        private static Title County(string? occupantClanId = null) =>
            new("county", "County", TitleRank.Count, "s_c", "old_lord", occupantClanId, occupantClanId is null ? null : 3f);

        [Fact]
        public void New_OpensTheLedgerWithTheInitialHolderOnDayZero()
        {
            var title = County();

            Assert.Equal(new[] { new TitleHolder("old_lord", 0f, TitleAcquisition.Initial) }, title.Holders);
            Assert.Equal("old_lord", title.HolderHeroId);
        }

        [Fact]
        public void EachHandover_AppendsToTheLedger_AndLeavesTheOriginalUntouched()
        {
            var original = County();

            var title = original
                .GrantTo("granted_lord", 1f)
                .ConqueredBy("conqueror", 2f)
                .InheritBy(null, null, 3f)
                .AwardTo("claimant", "clan_claimant", 4f);

            Assert.Equal(new[]
            {
                new TitleHolder("old_lord", 0f, TitleAcquisition.Initial),
                new TitleHolder("granted_lord", 1f, TitleAcquisition.Granted),
                new TitleHolder("conqueror", 2f, TitleAcquisition.Conquered),
                new TitleHolder(null, 3f, TitleAcquisition.Inherited),
                new TitleHolder("claimant", 4f, TitleAcquisition.Awarded)
            }, title.Holders);
            Assert.Equal("claimant", title.HolderHeroId);
            Assert.Single(original.Holders);
        }

        [Fact]
        public void GrantAndConquest_EndTheOccupation()
        {
            Assert.False(County("clan_occupier").GrantTo("lord", 5f).IsContested);
            Assert.False(County("clan_occupier").ConqueredBy("lord", 5f).IsContested);
        }

        [Fact]
        public void Inheritance_KeepsTheOccupation()
        {
            var title = County("clan_occupier").InheritBy("heir", "clan_heir", 5f);

            Assert.Equal("clan_occupier", title.OccupantClanId);
            Assert.Equal(3f, title.ContestedSinceDay);
        }

        [Fact]
        public void Inheritance_EndsTheOccupation_WhenTheHeirsClanOccupiesTheSeat()
        {
            Assert.False(County("clan_heir").InheritBy("heir", "clan_heir", 5f).IsContested);
        }

        [Fact]
        public void Award_EndsTheOccupation_OnlyWhenTheWinnersClanOccupiesTheSeat()
        {
            Assert.False(County("clan_winner").AwardTo("winner", "clan_winner", 5f).IsContested);
            Assert.Equal("clan_other", County("clan_other").AwardTo("winner", "clan_winner", 5f).OccupantClanId);
        }

        [Fact]
        public void HeldSinceDay_CarriesTheTenureThroughInheritance()
        {
            var title = County().GrantTo("lord", 7f).InheritBy("heir", "clan_lord", 20f).InheritBy("grandson", "clan_lord", 40f);

            Assert.Equal(7f, title.HeldSinceDay);
            Assert.Equal(50f, title.ConqueredBy("conqueror", 50f).HeldSinceDay);
        }

        [Fact]
        public void Equals_ComparesTheLedgerByContent()
        {
            Assert.Equal(County().GrantTo("lord", 7f), County().GrantTo("lord", 7f));
            Assert.NotEqual(County().GrantTo("lord", 7f), County().GrantTo("lord", 8f));
        }
    }
}
