using DellarteDellaGuerra.Domain.Church.Pilgrimage;
using Xunit;

namespace DellarteDellaGuerra.Domain.Tests.Church.Pilgrimage
{
    public class PilgrimagePolicyTests
    {
        [Fact]
        public void ShouldSpawn_WhenPartyCapReached_ReturnsFalse()
        {
            var shouldSpawn = PilgrimagePolicy.ShouldSpawn(activeParties: 3, maxParties: 3, roll: 0f);

            Assert.False(shouldSpawn);
        }

        [Fact]
        public void ShouldSpawn_WhenPartyCapExceeded_ReturnsFalse()
        {
            var shouldSpawn = PilgrimagePolicy.ShouldSpawn(activeParties: 4, maxParties: 3, roll: 0f);

            Assert.False(shouldSpawn);
        }

        [Fact]
        public void ShouldSpawn_WhenOnePartySlotLeftAndRollSucceeds_ReturnsTrue()
        {
            var shouldSpawn = PilgrimagePolicy.ShouldSpawn(activeParties: 2, maxParties: 3, roll: 0.04f);

            Assert.True(shouldSpawn);
        }

        [Fact]
        public void ShouldSpawn_WhenRollIsBelowSpawnChance_ReturnsTrue()
        {
            var shouldSpawn = PilgrimagePolicy.ShouldSpawn(
                activeParties: 0,
                maxParties: 3,
                roll: PilgrimagePolicy.SpawnChancePerSettlementPerDay - 0.001f);

            Assert.True(shouldSpawn);
        }

        [Fact]
        public void ShouldSpawn_WhenRollEqualsSpawnChance_ReturnsFalse()
        {
            var shouldSpawn = PilgrimagePolicy.ShouldSpawn(
                activeParties: 0,
                maxParties: 3,
                roll: PilgrimagePolicy.SpawnChancePerSettlementPerDay);

            Assert.False(shouldSpawn);
        }

        [Fact]
        public void ShouldSpawn_WhenRollIsAboveSpawnChance_ReturnsFalse()
        {
            var shouldSpawn = PilgrimagePolicy.ShouldSpawn(activeParties: 0, maxParties: 3, roll: 0.5f);

            Assert.False(shouldSpawn);
        }
    }
}
