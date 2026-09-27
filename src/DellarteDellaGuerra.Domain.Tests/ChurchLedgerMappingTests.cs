using System.Collections.Generic;
using DellarteDellaGuerra.Domain.Church.Hierarchy;
using DellarteDellaGuerra.Domain.Church.Ledger;
using DellarteDellaGuerra.Domain.Church.Port;
using Xunit;

namespace DellarteDellaGuerra.Domain.Tests.Church.Ledger
{
    public class ChurchLedgerMappingTests
    {
        [Theory]
        [InlineData(ChurchNodeRank.See, false, ClergyOffice.Bishop)]
        [InlineData(ChurchNodeRank.See, true, ClergyOffice.Bishop)]
        [InlineData(ChurchNodeRank.Cathedral, false, ClergyOffice.Bishop)]
        [InlineData(ChurchNodeRank.Cathedral, true, ClergyOffice.Bishop)]
        [InlineData(ChurchNodeRank.Abbey, false, ClergyOffice.Abbot)]
        [InlineData(ChurchNodeRank.Abbey, true, ClergyOffice.Abbess)]
        [InlineData(ChurchNodeRank.Priory, false, ClergyOffice.Prior)]
        [InlineData(ChurchNodeRank.Priory, true, ClergyOffice.Prioress)]
        public void Execute_MapsRankAndSexToClergyOffice(
            ChurchNodeRank rank,
            bool clergyIsFemale,
            ClergyOffice expectedOffice)
        {
            var result = ExecuteSingle(
                rank,
                new ChurchFoundationFacts(
                    "Foundation", "Clergy", clergyIsFemale, 0, 0f,
                    "Owner", "Faction", false, false));

            Assert.Equal(expectedOffice, result.ClergyOffice);
        }

        [Theory]
        [InlineData(false, false, false, ChurchFoundationStatus.None)]
        [InlineData(true, false, false, ChurchFoundationStatus.PilgrimageShrine)]
        [InlineData(false, true, false, ChurchFoundationStatus.Vacant)]
        [InlineData(false, false, true, ChurchFoundationStatus.UnderRaid)]
        [InlineData(true, true, false, ChurchFoundationStatus.PilgrimageShrine | ChurchFoundationStatus.Vacant)]
        [InlineData(true, false, true, ChurchFoundationStatus.PilgrimageShrine | ChurchFoundationStatus.UnderRaid)]
        [InlineData(false, true, true, ChurchFoundationStatus.Vacant | ChurchFoundationStatus.UnderRaid)]
        [InlineData(true, true, true, ChurchFoundationStatus.PilgrimageShrine | ChurchFoundationStatus.Vacant | ChurchFoundationStatus.UnderRaid)]
        public void Execute_ComposesFoundationStatusFlags(
            bool isShrine,
            bool isVacant,
            bool isUnderRaid,
            ChurchFoundationStatus expectedStatus)
        {
            var result = ExecuteSingle(
                ChurchNodeRank.Abbey,
                new ChurchFoundationFacts(
                    "Foundation", isVacant ? null : "Clergy", false, 0, 0f,
                    "Owner", "Faction", isShrine, isUnderRaid));

            Assert.Equal(expectedStatus, result.Status);
            Assert.Equal(!isVacant, result.ClergyOffice.HasValue);
        }

        [Theory]
        [InlineData("", "settlement")]
        [InlineData("Configured Name", "Configured Name")]
        public void Execute_WhenWorldFactsAreUnavailable_UsesConfiguredFallbackAndMarksEntryUnavailable(
            string configuredName,
            string expectedName)
        {
            var entry = new ChurchMapEntry(
                "entry", configuredName, ChurchNodeRank.Abbey, "settlement",
                new List<ChurchMapEntry>());
            var result = Execute(entry, new Dictionary<string, ChurchFoundationFacts>());

            Assert.Equal(expectedName, result.Name);
            Assert.Equal(expectedName, result.SettlementName);
            Assert.False(result.IsAvailable);
            Assert.Null(result.ClergyName);
            Assert.Null(result.ClergyOffice);
            Assert.Null(result.PlayerRelation);
            Assert.Null(result.ClergyPower);
            Assert.Null(result.OwnerName);
            Assert.Null(result.FactionName);
            Assert.Equal(ChurchFoundationStatus.None, result.Status);
        }

        private static ChurchLedgerEntry ExecuteSingle(
            ChurchNodeRank rank,
            ChurchFoundationFacts facts)
        {
            var entry = new ChurchMapEntry(
                "entry", "Configured Name", rank, "settlement",
                new List<ChurchMapEntry>());
            return Execute(
                entry,
                new Dictionary<string, ChurchFoundationFacts> { ["settlement"] = facts });
        }

        private static ChurchLedgerEntry Execute(
            ChurchMapEntry entry,
            IReadOnlyDictionary<string, ChurchFoundationFacts> foundations)
        {
            var useCase = new BuildChurchLedgerUseCase(
                new FakeBuildChurchMap(entry),
                new FakeChurchLedgerWorld(foundations));
            return useCase.Execute().Roots[0];
        }

        private class FakeBuildChurchMap : IBuildChurchMapUseCase
        {
            private readonly ChurchMap _map;

            public FakeBuildChurchMap(ChurchMapEntry root)
            {
                _map = new ChurchMap(new[] { root });
            }

            public ChurchMap Execute() => _map;
        }

        private class FakeChurchLedgerWorld : IChurchLedgerWorld
        {
            private readonly IReadOnlyDictionary<string, ChurchFoundationFacts> _foundations;

            public FakeChurchLedgerWorld(
                IReadOnlyDictionary<string, ChurchFoundationFacts> foundations)
            {
                _foundations = foundations;
            }

            public ChurchFoundationFacts? GetFoundation(string settlementId) =>
                _foundations.TryGetValue(settlementId, out var facts) ? facts : null;

            public IReadOnlyCollection<int> GetLivingClergyRelations() => new int[0];
        }
    }
}
