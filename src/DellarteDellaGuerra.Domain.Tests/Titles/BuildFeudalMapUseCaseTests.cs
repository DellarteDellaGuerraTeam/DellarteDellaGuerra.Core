using DellarteDellaGuerra.Domain.Titles;
using DellarteDellaGuerra.Domain.Titles.Model;

namespace DellarteDellaGuerra.Domain.Tests.Titles
{
    public class BuildFeudalMapUseCaseTests
    {
        private static FakeFeudalStructure CreateStructure() => new FakeFeudalStructure()
            .AddTitle("kingdom_k", TitleRank.King, name: "Kingdom")
            .AddTitle("duchy_d", TitleRank.Duke, "kingdom_k", name: "Duchy")
            .AddTitle("county_c", TitleRank.Count, "duchy_d", name: "County")
            .AddTitle("barony_b", TitleRank.Baron, "county_c", name: "Barony");

        [Fact]
        public void Execute_BuildsTreeShape_FollowingDeJureStructure()
        {
            var useCase = new BuildFeudalMapUseCase(new FakeTitleRepository(), CreateStructure());

            FeudalMap map = useCase.Execute();

            var realm = Assert.Single(map.Realms);
            Assert.Equal("kingdom_k", realm.TitleId);
            var duchy = Assert.Single(realm.Vassals);
            Assert.Equal("duchy_d", duchy.TitleId);
            var county = Assert.Single(duchy.Vassals);
            Assert.Equal("county_c", county.TitleId);
            var barony = Assert.Single(county.Vassals);
            Assert.Equal("barony_b", barony.TitleId);
            Assert.Empty(barony.Vassals);
        }

        [Fact]
        public void Execute_UsesNameAndRank_FromFeudalStructure()
        {
            var useCase = new BuildFeudalMapUseCase(new FakeTitleRepository(), CreateStructure());

            FeudalMap map = useCase.Execute();

            var realm = Assert.Single(map.Realms);
            Assert.Equal("Kingdom", realm.Name);
            Assert.Equal(TitleRank.King, realm.Rank);
            Assert.Equal(TitleRank.Duke, realm.Vassals[0].Rank);
        }

        [Fact]
        public void Execute_OverlaysHolderAndSeat_FromTitleRepository()
        {
            var titleRepository = new FakeTitleRepository(
                new Title("kingdom_k", "Kingdom", TitleRank.King, "s_k", "clan_king"),
                new Title("duchy_d", "Duchy", TitleRank.Duke, "s_d", "clan_duke"));
            var useCase = new BuildFeudalMapUseCase(titleRepository, CreateStructure());

            FeudalMap map = useCase.Execute();

            var realm = Assert.Single(map.Realms);
            Assert.Equal("clan_king", realm.HolderClanId);
            Assert.Equal("s_k", realm.SeatSettlementId);
            Assert.Equal("clan_duke", realm.Vassals[0].HolderClanId);
            Assert.Equal("s_d", realm.Vassals[0].SeatSettlementId);
        }

        [Fact]
        public void Execute_FallsBackToVacantHolderAndEmptySeat_WhenRepositoryHasNoEntry()
        {
            var titleRepository = new FakeTitleRepository(
                new Title("kingdom_k", "Kingdom", TitleRank.King, "s_k", "clan_king"));
            var useCase = new BuildFeudalMapUseCase(titleRepository, CreateStructure());

            FeudalMap map = useCase.Execute();

            FeudalMapEntry duchy = map.Realms[0].Vassals[0];
            Assert.Null(duchy.HolderClanId);
            Assert.Equal(string.Empty, duchy.SeatSettlementId);
        }

        [Fact]
        public void Execute_FallsBackToNullHolder_WhenRepositoryTitleIsVacant()
        {
            var titleRepository = new FakeTitleRepository(
                new Title("kingdom_k", "Kingdom", TitleRank.King, "s_k", null));
            var useCase = new BuildFeudalMapUseCase(titleRepository, CreateStructure());

            FeudalMap map = useCase.Execute();

            Assert.Null(map.Realms[0].HolderClanId);
            Assert.Equal("s_k", map.Realms[0].SeatSettlementId);
        }

        [Fact]
        public void Execute_DetectsAllRoots_WithoutDeJureSuzerain()
        {
            var structure = new FakeFeudalStructure()
                .AddTitle("kingdom_a", TitleRank.King, name: "Kingdom A")
                .AddTitle("kingdom_b", TitleRank.King, name: "Kingdom B")
                .AddTitle("duchy_d", TitleRank.Duke, "kingdom_b", name: "Duchy");
            var useCase = new BuildFeudalMapUseCase(new FakeTitleRepository(), structure);

            FeudalMap map = useCase.Execute();

            Assert.Equal(2, map.Realms.Count);
            Assert.Equal("kingdom_a", map.Realms[0].TitleId);
            Assert.Equal("kingdom_b", map.Realms[1].TitleId);
        }

        [Fact]
        public void Execute_ReturnsEmptyMap_WhenStructureHasNoTitles()
        {
            var useCase = new BuildFeudalMapUseCase(new FakeTitleRepository(), new FakeFeudalStructure());

            Assert.Empty(useCase.Execute().Realms);
        }
    }
}
