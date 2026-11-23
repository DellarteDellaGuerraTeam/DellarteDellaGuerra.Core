using DellarteDellaGuerra.Domain.PoliticalMap.Labels;
using DellarteDellaGuerra.Domain.PoliticalMap.Overlay;

namespace DellarteDellaGuerra.Domain.Tests.PoliticalMap.Labels
{
    public class PoliticalMapLabelTiersTests
    {
        [Fact]
        public void EachViewLabelsOneTier()
        {
            Assert.Empty(PoliticalMapLabelTiers.Of(PoliticalMapView.Off));
            Assert.Equal(new[] { PoliticalMapLabelTier.Faction }, PoliticalMapLabelTiers.Of(PoliticalMapView.Kingdom));
            Assert.Equal(new[] { PoliticalMapLabelTier.Owner }, PoliticalMapLabelTiers.Of(PoliticalMapView.Owners));
            Assert.Equal(new[] { PoliticalMapLabelTier.Territory }, PoliticalMapLabelTiers.Of(PoliticalMapView.Owners, PoliticalMapLabelMode.Titles));
        }

        [Fact]
        public void KeysComeFromTheWorldOrTheOwnersRegions()
        {
            var ids = new[] { "town_a", "village_b" };
            var world = new FakeWorld();

            Assert.Equal(new[] { "kingdom_a", "kingdom_b" }, PoliticalMapLabelTiers.KeysOf(PoliticalMapLabelTier.Faction, ids, null, world));
            Assert.Equal(
                new[] { "clan_liege", null },
                PoliticalMapLabelTiers.KeysOf(PoliticalMapLabelTier.Owner, ids, new[] { "clan_liege", null }, world));
            Assert.Equal(
                new[] { "county_a", "barony_b" },
                PoliticalMapLabelTiers.KeysOf(PoliticalMapLabelTier.Territory, ids, new[] { "county_a", "barony_b" }, world));
        }

        [Fact]
        public void TheOwnerTierWithoutRegionKeysHasNoKeys()
        {
            Assert.All(
                PoliticalMapLabelTiers.KeysOf(PoliticalMapLabelTier.Owner, new[] { "town_a" }, null, new FakeWorld()),
                Assert.Null);
        }

        [Fact]
        public void OwnerRegionsKeepFinerFragmentsThanFactions()
        {
            Assert.True(PoliticalMapLabelTiers.MinCells(PoliticalMapLabelTier.Owner) < PoliticalMapLabelTiers.MinCells(PoliticalMapLabelTier.Faction));
        }

        private sealed class FakeWorld : IPoliticalMapWorld
        {
            public string? SeatOf(string settlementId) => settlementId;

            public string? OwnerClanOf(string settlementId) => settlementId == "town_a" ? "clan_a" : null;

            public string? FactionOf(string settlementId) => settlementId == "town_a" ? "kingdom_a" : "kingdom_b";

            public uint? ClanColor(string clanId) => null;

            public uint? FactionColor(string factionId) => null;

            public string? KingdomOfClan(string clanId) => null;

            public string? RulerClanOf(string kingdomId) => null;
        }
    }
}
