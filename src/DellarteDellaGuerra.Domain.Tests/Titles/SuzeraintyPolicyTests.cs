using DellarteDellaGuerra.Domain.Titles;
using DellarteDellaGuerra.Domain.Titles.Model;

namespace DellarteDellaGuerra.Domain.Tests.Titles
{
    public class SuzeraintyPolicyTests
    {
        private static FakeFeudalStructure CreateStructure() => new FakeFeudalStructure()
            .AddTitle("kingdom_k", TitleRank.King)
            .AddTitle("duchy_d", TitleRank.Duke, "kingdom_k")
            .AddTitle("county_c", TitleRank.Earl, "duchy_d")
            .AddTitle("barony_b", TitleRank.Baron, "county_c");

        private static FakeGenealogy CreateGenealogy() => new FakeGenealogy()
            .AddHero("clan_king", "clan_king")
            .AddHero("clan_duke", "clan_duke")
            .AddHero("clan_earl", "clan_earl")
            .AddHero("clan_baron", "clan_baron");

        [Fact]
        public void GetSuzerain_ReturnsEarl_ForBaron()
        {
            var genealogy = CreateGenealogy();
            var titleRepository = new FakeTitleRepository(
                new Title("kingdom_k", "Kingdom", TitleRank.King, "s_k", "clan_king"),
                new Title("duchy_d", "Duchy", TitleRank.Duke, "s_d", "clan_duke"),
                new Title("county_c", "County", TitleRank.Earl, "s_c", "clan_earl"),
                new Title("barony_b", "Barony", TitleRank.Baron, "s_b", "clan_baron"))
            { Genealogy = genealogy };
            var policy = new SuzeraintyPolicy(titleRepository, CreateStructure(), genealogy);

            Assert.Equal("clan_earl", policy.GetSuzerain("clan_baron"));
        }

        [Fact]
        public void GetSuzerain_SkipsVacantCounty_ReturnsDuke()
        {
            var genealogy = CreateGenealogy();
            var titleRepository = new FakeTitleRepository(
                new Title("kingdom_k", "Kingdom", TitleRank.King, "s_k", "clan_king"),
                new Title("duchy_d", "Duchy", TitleRank.Duke, "s_d", "clan_duke"),
                new Title("county_c", "County", TitleRank.Earl, "s_c", null),
                new Title("barony_b", "Barony", TitleRank.Baron, "s_b", "clan_baron"))
            { Genealogy = genealogy };
            var policy = new SuzeraintyPolicy(titleRepository, CreateStructure(), genealogy);

            Assert.Equal("clan_duke", policy.GetSuzerain("clan_baron"));
        }

        [Fact]
        public void GetSuzerain_ReturnsKing_ForEarlWithoutDuke()
        {
            var genealogy = CreateGenealogy();
            var titleRepository = new FakeTitleRepository(
                new Title("kingdom_k", "Kingdom", TitleRank.King, "s_k", "clan_king"),
                new Title("duchy_d", "Duchy", TitleRank.Duke, "s_d", null),
                new Title("county_c", "County", TitleRank.Earl, "s_c", "clan_earl"))
            { Genealogy = genealogy };
            var policy = new SuzeraintyPolicy(titleRepository, CreateStructure(), genealogy);

            Assert.Equal("clan_king", policy.GetSuzerain("clan_earl"));
        }

        [Fact]
        public void GetSuzerain_ReturnsNull_ForKing()
        {
            var genealogy = CreateGenealogy();
            var titleRepository = new FakeTitleRepository(
                new Title("kingdom_k", "Kingdom", TitleRank.King, "s_k", "clan_king"))
            { Genealogy = genealogy };
            var policy = new SuzeraintyPolicy(titleRepository, CreateStructure(), genealogy);

            Assert.Null(policy.GetSuzerain("clan_king"));
        }

        [Fact]
        public void GetSuzerain_ReturnsNull_ForUntitledClan()
        {
            var genealogy = CreateGenealogy();
            var titleRepository = new FakeTitleRepository(
                new Title("kingdom_k", "Kingdom", TitleRank.King, "s_k", "clan_king"))
            { Genealogy = genealogy };
            var policy = new SuzeraintyPolicy(titleRepository, CreateStructure(), genealogy);

            Assert.Null(policy.GetSuzerain("clan_untitled"));
        }

        [Fact]
        public void GetPrimaryTitle_ReturnsTheHighestRankedTitleOrNull()
        {
            var genealogy = CreateGenealogy();
            var titleRepository = new FakeTitleRepository(
                new Title("duchy_d", "Duchy", TitleRank.Duke, "s_d", "clan_duke"),
                new Title("county_c", "County", TitleRank.Earl, "s_c", "clan_duke"))
            { Genealogy = genealogy };
            var policy = new SuzeraintyPolicy(titleRepository, CreateStructure(), genealogy);

            Assert.Equal(TitleRank.Duke, policy.GetPrimaryTitle("clan_duke")?.Rank);
            Assert.Null(policy.GetPrimaryTitle("clan_untitled"));
        }

        // county_x (clan_other) ── barony_a, listed first so only tenure can pass it over
        // county_c (clan_earl) ── barony_b (clan_baron)
        private static (SuzeraintyPolicy Policy, FakeTitleRepository Titles) SetupTwoBaronies()
        {
            var genealogy = CreateGenealogy().AddHero("clan_other", "clan_other");
            var structure = CreateStructure()
                .AddTitle("county_x", TitleRank.Earl, "duchy_d")
                .AddTitle("barony_a", TitleRank.Baron, "county_x");
            var titles = new FakeTitleRepository(
                new Title("barony_a", "Barony A", TitleRank.Baron, "s_ba", null),
                new Title("county_x", "County X", TitleRank.Earl, "s_cx", "clan_other"),
                new Title("duchy_d", "Duchy", TitleRank.Duke, "s_d", "clan_duke"),
                new Title("county_c", "County", TitleRank.Earl, "s_c", "clan_earl"),
                new Title("barony_b", "Barony", TitleRank.Baron, "s_b", "clan_baron"))
            { Genealogy = genealogy };
            return (new SuzeraintyPolicy(titles, structure, genealogy), titles);
        }

        [Fact]
        public void GetSuzerain_KeepsTheLiegeItServes_WhenItGainsATitleOfTheSameRank()
        {
            var (policy, titles) = SetupTwoBaronies();
            Assert.Equal("clan_earl", policy.GetSuzerain("clan_baron"));

            titles.SaveTitle(titles.GetTitle("barony_a")!.GrantTo("clan_baron", 10f));

            Assert.Equal("clan_earl", policy.GetSuzerain("clan_baron"));
        }

        [Fact]
        public void GetSuzerain_ServesUnderAHigherTitle_WhenItGainsOne()
        {
            var (policy, titles) = SetupTwoBaronies();
            Assert.Equal("clan_earl", policy.GetSuzerain("clan_baron"));

            titles.SaveTitle(titles.GetTitle("county_x")!.GrantTo("clan_baron", 10f));

            Assert.Equal("clan_duke", policy.GetSuzerain("clan_baron"));
        }

        [Fact]
        public void GetSuzerain_ServesUnderItsOtherTitle_WhenItLosesThePrimaryOne()
        {
            var (policy, titles) = SetupTwoBaronies();
            Assert.Equal("clan_earl", policy.GetSuzerain("clan_baron"));
            titles.SaveTitle(titles.GetTitle("barony_a")!.GrantTo("clan_baron", 10f));

            titles.SaveTitle(titles.GetTitle("barony_b")!.ConqueredBy(null, 20f));

            Assert.Equal("clan_other", policy.GetSuzerain("clan_baron"));
        }

        [Fact]
        public void GetSuzerain_KeepsTheLiegeItServes_WhenAnHeirInheritsBothTitles()
        {
            var (policy, titles) = SetupTwoBaronies();
            titles.Genealogy.AddHero("baron_heir", "clan_baron");
            titles.SaveTitle(titles.GetTitle("barony_a")!.GrantTo("clan_baron", 10f));

            titles.SaveTitle(titles.GetTitle("barony_b")!.InheritBy("baron_heir", "clan_baron", 20f));
            titles.SaveTitle(titles.GetTitle("barony_a")!.InheritBy("baron_heir", "clan_baron", 20f));

            Assert.Equal("clan_earl", policy.GetSuzerain("clan_baron"));
        }

        [Fact]
        public void GetPrimaryTitle_FallsBackToOrdinalIdRegardlessOfListingOrder_WhenBothWereTakenTheSameDay()
        {
            var (policy, titles) = SetupTwoBaronies();

            titles.SaveTitle(titles.GetTitle("barony_b")!.GrantTo("clan_baron", 10f));
            titles.SaveTitle(titles.GetTitle("barony_a")!.GrantTo("clan_baron", 10f));

            var reversed = new FakeTitleRepository(titles.GetAllTitles().Reverse().ToArray()) { Genealogy = titles.Genealogy };
            policy = new SuzeraintyPolicy(reversed, CreateStructure(), titles.Genealogy);
            Assert.Equal("barony_a", policy.GetPrimaryTitle("clan_baron")?.Id);
        }

        [Theory]
        [InlineData(TitleRank.Baron)]
        [InlineData(TitleRank.Earl)]
        public void GetSuzerain_SkipsAHouseOfEqualOrLowerRank(TitleRank prospectiveLiegeRank)
        {
            var genealogy = CreateGenealogy();
            var titles = new FakeTitleRepository(
                new Title("kingdom_k", "Kingdom", TitleRank.King, "", "clan_king"),
                new Title("duchy_d", "Estate", prospectiveLiegeRank, "", "clan_duke"),
                new Title("county_c", "Earldom", TitleRank.Earl, "", "clan_earl")) { Genealogy = genealogy };
            var policy = new SuzeraintyPolicy(titles, CreateStructure(), genealogy);

            Assert.Equal("clan_king", policy.GetSuzerain("clan_earl"));
        }

        [Fact]
        public void GetSuzerain_UsesTheLiegesHighestDignityRatherThanTheEstateInThePath()
        {
            var genealogy = CreateGenealogy();
            var titles = new FakeTitleRepository(
                new Title("duchy_d", "Duchy", TitleRank.Duke, "", "clan_duke"),
                new Title("county_c", "Estate", TitleRank.Baron, "", "clan_duke"),
                new Title("barony_b", "Barony", TitleRank.Baron, "", "clan_baron")) { Genealogy = genealogy };

            Assert.Equal("clan_duke", new SuzeraintyPolicy(titles, CreateStructure(), genealogy).GetSuzerain("clan_baron"));
        }

        [Fact]
        public void GetSuzerain_FollowsExplicitAllegianceWithoutChangingTerritorialNesting()
        {
            var genealogy = CreateGenealogy();
            var structure = CreateStructure().WithAllegiance("barony_b", "duchy_d");
            var titles = new FakeTitleRepository(
                new Title("duchy_d", "Duchy", TitleRank.Duke, "", "clan_duke"),
                new Title("county_c", "County", TitleRank.Earl, "", "clan_earl"),
                new Title("barony_b", "Barony", TitleRank.Baron, "", "clan_baron")) { Genealogy = genealogy };

            Assert.Equal("clan_duke", new SuzeraintyPolicy(titles, structure, genealogy).GetSuzerain("clan_baron"));
            Assert.Equal("county_c", structure.GetDeJureSuzerainTitleId("barony_b"));
        }

        [Fact]
        public void GetSuzerain_TerminatesOnMalformedAllegianceCycles()
        {
            var genealogy = CreateGenealogy();
            var structure = CreateStructure().WithAllegiance("county_c", "barony_b");
            var titles = new FakeTitleRepository(
                new Title("county_c", "County", TitleRank.Earl, "", "clan_earl"),
                new Title("barony_b", "Barony", TitleRank.Baron, "", "clan_earl")) { Genealogy = genealogy };

            Assert.Null(new SuzeraintyPolicy(titles, structure, genealogy).GetSuzerain("clan_earl"));
        }
    }
}
