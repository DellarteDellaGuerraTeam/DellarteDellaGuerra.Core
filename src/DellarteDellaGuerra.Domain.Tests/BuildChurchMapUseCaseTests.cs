using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.Church;
using DellarteDellaGuerra.Domain.Church.Hierarchy;
using DellarteDellaGuerra.Domain.Church.Port;
using Xunit;

namespace DellarteDellaGuerra.Domain.Tests.Church.Hierarchy
{
    public class BuildChurchMapUseCaseTests
    {
        [Fact]
        public void Execute_WithDioceses_BuildsOneSeeRootPerDiocese()
        {
            var useCase = new BuildChurchMapUseCase(new FakeChurchSettlementsProvider(
                new ChurchDioceseData("see_ely", "See of Ely", new List<ChurchSettlementData>
                {
                    new("village_Ely_Cathedral", ChurchSettlementKind.Cathedral),
                    new("village_Battle_Abbey", ChurchSettlementKind.Abbey)
                }),
                new ChurchDioceseData("see_llandaff", "See of Llandaff", new List<ChurchSettlementData>
                {
                    new("village_Llandaff_Cathedral", ChurchSettlementKind.Cathedral)
                })));

            var map = useCase.Execute();

            Assert.Equal(2, map.Roots.Count);
            Assert.All(map.Roots, root => Assert.Equal(ChurchNodeRank.See, root.Rank));
            Assert.Equal(new[] { "see_ely", "see_llandaff" }, map.Roots.Select(root => root.Id));
            Assert.Equal("See of Ely", map.Roots[0].Name);
        }

        [Fact]
        public void Execute_SeatIsTheCathedralMemberSettlement()
        {
            var useCase = new BuildChurchMapUseCase(new FakeChurchSettlementsProvider(
                new ChurchDioceseData("see_ely", "See of Ely", new List<ChurchSettlementData>
                {
                    new("village_Battle_Abbey", ChurchSettlementKind.Abbey),
                    new("village_Ely_Cathedral", ChurchSettlementKind.Cathedral)
                })));

            var map = useCase.Execute();

            Assert.Equal("village_Ely_Cathedral", map.Roots[0].SettlementId);
        }

        [Fact]
        public void Execute_ChildrenAreNonCathedralMembersOrderedAbbeysBeforePriories()
        {
            var useCase = new BuildChurchMapUseCase(new FakeChurchSettlementsProvider(
                new ChurchDioceseData("see_st_asaph", "See of St Asaph", new List<ChurchSettlementData>
                {
                    new("village_St_Asaph_Cathedral", ChurchSettlementKind.Cathedral),
                    new("village_Lanercost_Priory", ChurchSettlementKind.Priory),
                    new("village_Hexham_Abbey", ChurchSettlementKind.Abbey),
                    new("village_Byland_Abbey", ChurchSettlementKind.Abbey)
                })));

            var map = useCase.Execute();

            var children = map.Roots[0].Children;
            Assert.Equal(
                new[] { "village_Hexham_Abbey", "village_Byland_Abbey", "village_Lanercost_Priory" },
                children.Select(child => child.SettlementId));
            Assert.Equal(
                new[] { ChurchNodeRank.Abbey, ChurchNodeRank.Abbey, ChurchNodeRank.Priory },
                children.Select(child => child.Rank));
            Assert.All(children, child => Assert.Empty(child.Children));
        }

        [Fact]
        public void Execute_WithNoDioceses_ReturnsEmptyMap()
        {
            var useCase = new BuildChurchMapUseCase(new FakeChurchSettlementsProvider());

            var map = useCase.Execute();

            Assert.Empty(map.Roots);
        }

        private class FakeChurchSettlementsProvider : IChurchSettlementsProvider
        {
            private readonly IReadOnlyCollection<ChurchDioceseData> _dioceses;

            public FakeChurchSettlementsProvider(params ChurchDioceseData[] dioceses)
            {
                _dioceses = dioceses;
            }

            public IReadOnlyCollection<ChurchSettlementData> GetChurchSettlements() =>
                _dioceses.SelectMany(diocese => diocese.Members).ToList();

            public IReadOnlyCollection<ChurchDioceseData> GetDioceses() => _dioceses;
        }
    }
}
