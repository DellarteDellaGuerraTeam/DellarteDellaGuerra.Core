using System.Collections.Generic;
using DellarteDellaGuerra.Domain.Church.Favour;
using DellarteDellaGuerra.Domain.Church.Hierarchy;
using DellarteDellaGuerra.Domain.Church.Ledger;
using DellarteDellaGuerra.Domain.Church.Port;
using Xunit;

namespace DellarteDellaGuerra.Domain.Tests.Church.Ledger
{
    public class BuildChurchLedgerUseCaseTests
    {
        [Fact]
        public void Execute_ProjectsWorldFactsAndDerivesChurchMeaning()
        {
            var priory = new ChurchMapEntry(
                "priory",
                string.Empty,
                ChurchNodeRank.Priory,
                "priory",
                new List<ChurchMapEntry>());
            var see = new ChurchMapEntry(
                "see_ely",
                "See of Ely",
                ChurchNodeRank.See,
                "ely_cathedral",
                new List<ChurchMapEntry> { priory });
            var world = new FakeChurchLedgerWorld(
                new Dictionary<string, ChurchFoundationFacts>
                {
                    ["ely_cathedral"] = new ChurchFoundationFacts(
                        "Ely Cathedral", "Osmund", false, 12, 220f,
                        "House Ely", "England", true, false),
                    ["priory"] = new ChurchFoundationFacts(
                        "Dover Priory", "Matilda", true, -3, 80f,
                        "House Dover", "England", false, true)
                },
                new List<int> { 12, -3 });
            var useCase = new BuildChurchLedgerUseCase(new FakeBuildChurchMap(see), world);

            var ledger = useCase.Execute();

            Assert.Equal(4.5f, ledger.Favour.AverageRelation);
            Assert.Equal(ChurchFavourRank.Favoured, ledger.Favour.Rank);
            Assert.Equal("See of Ely", ledger.Roots[0].Name);
            Assert.Equal("Ely Cathedral", ledger.Roots[0].SettlementName);
            Assert.Equal(ClergyOffice.Bishop, ledger.Roots[0].ClergyOffice);
            Assert.True(ledger.Roots[0].Status.HasFlag(ChurchFoundationStatus.PilgrimageShrine));

            var prioryResult = ledger.Roots[0].Children[0];
            Assert.Equal("Dover Priory", prioryResult.Name);
            Assert.Equal(ClergyOffice.Prioress, prioryResult.ClergyOffice);
            Assert.True(prioryResult.Status.HasFlag(ChurchFoundationStatus.UnderRaid));
        }

        [Fact]
        public void Execute_WhenClergyIsMissing_MarksTheOfficeVacant()
        {
            var abbey = new ChurchMapEntry(
                "abbey",
                string.Empty,
                ChurchNodeRank.Abbey,
                "abbey",
                new List<ChurchMapEntry>());
            var world = new FakeChurchLedgerWorld(
                new Dictionary<string, ChurchFoundationFacts>
                {
                    ["abbey"] = new ChurchFoundationFacts(
                        "Battle Abbey", null, false, null, null,
                        "House Battle", "England", false, false)
                },
                new List<int>());

            var ledger = new BuildChurchLedgerUseCase(new FakeBuildChurchMap(abbey), world).Execute();

            Assert.Null(ledger.Roots[0].ClergyOffice);
            Assert.True(ledger.Roots[0].Status.HasFlag(ChurchFoundationStatus.Vacant));
        }

        private class FakeBuildChurchMap : IBuildChurchMapUseCase
        {
            private readonly ChurchMap _map;

            public FakeBuildChurchMap(params ChurchMapEntry[] roots)
            {
                _map = new ChurchMap(roots);
            }

            public ChurchMap Execute() => _map;
        }

        private class FakeChurchLedgerWorld : IChurchLedgerWorld
        {
            private readonly IReadOnlyDictionary<string, ChurchFoundationFacts> _foundations;
            private readonly IReadOnlyCollection<int> _relations;

            public FakeChurchLedgerWorld(
                IReadOnlyDictionary<string, ChurchFoundationFacts> foundations,
                IReadOnlyCollection<int> relations)
            {
                _foundations = foundations;
                _relations = relations;
            }

            public ChurchFoundationFacts? GetFoundation(string settlementId) =>
                _foundations.TryGetValue(settlementId, out var facts) ? facts : null;

            public IReadOnlyCollection<int> GetLivingClergyRelations() => _relations;
        }
    }
}
