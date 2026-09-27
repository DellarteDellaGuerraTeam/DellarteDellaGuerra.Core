using DellarteDellaGuerra.Domain.Titles;
using DellarteDellaGuerra.Domain.Titles.Model;

namespace DellarteDellaGuerra.Domain.Tests.Titles
{
    public class SuzeraintyPolicyTests
    {
        private static FakeFeudalStructure CreateStructure() => new FakeFeudalStructure()
            .AddTitle("kingdom_k", TitleRank.King)
            .AddTitle("duchy_d", TitleRank.Duke, "kingdom_k")
            .AddTitle("county_c", TitleRank.Count, "duchy_d")
            .AddTitle("barony_b", TitleRank.Baron, "county_c");

        private static FakeGenealogy CreateGenealogy() => new FakeGenealogy()
            .AddHero("clan_king", "clan_king")
            .AddHero("clan_duke", "clan_duke")
            .AddHero("clan_count", "clan_count")
            .AddHero("clan_baron", "clan_baron");

        [Fact]
        public void GetSuzerain_ReturnsCount_ForBaron()
        {
            var genealogy = CreateGenealogy();
            var titleRepository = new FakeTitleRepository(
                new Title("kingdom_k", "Kingdom", TitleRank.King, "s_k", "clan_king"),
                new Title("duchy_d", "Duchy", TitleRank.Duke, "s_d", "clan_duke"),
                new Title("county_c", "County", TitleRank.Count, "s_c", "clan_count"),
                new Title("barony_b", "Barony", TitleRank.Baron, "s_b", "clan_baron"))
            { Genealogy = genealogy };
            var policy = new SuzeraintyPolicy(titleRepository, CreateStructure(), genealogy);

            Assert.Equal("clan_count", policy.GetSuzerain("clan_baron"));
        }

        [Fact]
        public void GetSuzerain_SkipsVacantCounty_ReturnsDuke()
        {
            var genealogy = CreateGenealogy();
            var titleRepository = new FakeTitleRepository(
                new Title("kingdom_k", "Kingdom", TitleRank.King, "s_k", "clan_king"),
                new Title("duchy_d", "Duchy", TitleRank.Duke, "s_d", "clan_duke"),
                new Title("county_c", "County", TitleRank.Count, "s_c", null),
                new Title("barony_b", "Barony", TitleRank.Baron, "s_b", "clan_baron"))
            { Genealogy = genealogy };
            var policy = new SuzeraintyPolicy(titleRepository, CreateStructure(), genealogy);

            Assert.Equal("clan_duke", policy.GetSuzerain("clan_baron"));
        }

        [Fact]
        public void GetSuzerain_ReturnsKing_ForCountWithoutDuke()
        {
            var genealogy = CreateGenealogy();
            var titleRepository = new FakeTitleRepository(
                new Title("kingdom_k", "Kingdom", TitleRank.King, "s_k", "clan_king"),
                new Title("duchy_d", "Duchy", TitleRank.Duke, "s_d", null),
                new Title("county_c", "County", TitleRank.Count, "s_c", "clan_count"))
            { Genealogy = genealogy };
            var policy = new SuzeraintyPolicy(titleRepository, CreateStructure(), genealogy);

            Assert.Equal("clan_king", policy.GetSuzerain("clan_count"));
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
                new Title("county_c", "County", TitleRank.Count, "s_c", "clan_duke"))
            { Genealogy = genealogy };
            var policy = new SuzeraintyPolicy(titleRepository, CreateStructure(), genealogy);

            Assert.Equal(TitleRank.Duke, policy.GetPrimaryTitle("clan_duke")?.Rank);
            Assert.Null(policy.GetPrimaryTitle("clan_untitled"));
        }

        // county_x (clan_other) ── barony_x, listed first so an unpinned tie would pick it
        // county_c (clan_count) ── barony_b (clan_baron)
        private static (SuzeraintyPolicy Policy, FakeTitleRepository Titles) SetupTwoBaronies()
        {
            var genealogy = CreateGenealogy().AddHero("clan_other", "clan_other");
            var structure = CreateStructure()
                .AddTitle("county_x", TitleRank.Count, "duchy_d")
                .AddTitle("barony_x", TitleRank.Baron, "county_x");
            var titles = new FakeTitleRepository(
                new Title("barony_x", "Barony X", TitleRank.Baron, "s_bx", null),
                new Title("county_x", "County X", TitleRank.Count, "s_cx", "clan_other"),
                new Title("duchy_d", "Duchy", TitleRank.Duke, "s_d", "clan_duke"),
                new Title("county_c", "County", TitleRank.Count, "s_c", "clan_count"),
                new Title("barony_b", "Barony", TitleRank.Baron, "s_b", "clan_baron"))
            { Genealogy = genealogy };
            return (new SuzeraintyPolicy(titles, structure, genealogy), titles);
        }

        [Fact]
        public void GetSuzerain_KeepsTheLiegeItServes_WhenItGainsATitleOfTheSameRank()
        {
            var (policy, titles) = SetupTwoBaronies();
            Assert.Equal("clan_count", policy.GetSuzerain("clan_baron"));

            titles.SaveTitle(titles.GetTitle("barony_x")!.WithHolder("clan_baron"));

            Assert.Equal("clan_count", policy.GetSuzerain("clan_baron"));
        }

        [Fact]
        public void GetSuzerain_ServesUnderAHigherTitle_WhenItGainsOne()
        {
            var (policy, titles) = SetupTwoBaronies();
            Assert.Equal("clan_count", policy.GetSuzerain("clan_baron"));

            titles.SaveTitle(titles.GetTitle("county_x")!.WithHolder("clan_baron"));

            Assert.Equal("clan_duke", policy.GetSuzerain("clan_baron"));
        }

        [Fact]
        public void GetSuzerain_ServesUnderItsOtherTitle_WhenItLosesThePrimaryOne()
        {
            var (policy, titles) = SetupTwoBaronies();
            Assert.Equal("clan_count", policy.GetSuzerain("clan_baron"));
            titles.SaveTitle(titles.GetTitle("barony_x")!.WithHolder("clan_baron"));

            titles.SaveTitle(titles.GetTitle("barony_b")!.WithHolder(null));

            Assert.Equal("clan_other", policy.GetSuzerain("clan_baron"));
        }
    }
}
