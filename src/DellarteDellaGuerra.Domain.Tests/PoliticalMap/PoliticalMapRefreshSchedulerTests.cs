using DellarteDellaGuerra.Domain.PoliticalMap;

namespace DellarteDellaGuerra.Domain.Tests.PoliticalMap
{
    public class PoliticalMapRefreshSchedulerTests
    {
        private static readonly Dictionary<string, int> ChunkCounts = new()
        {
            ["county_kent"] = 3,
            ["county_york"] = 2,
            ["county_empty"] = 0
        };

        private static int ChunkCountOf(string territoryId) => ChunkCounts[territoryId];

        private static IEnumerable<(string, int)> Ids(IEnumerable<PoliticalMapChunk> chunks) =>
            chunks.Select(chunk => (chunk.TerritoryId, chunk.ChunkIndex));

        [Fact]
        public void NothingMarkedMeansNothingToBuild()
        {
            var scheduler = new PoliticalMapRefreshScheduler();

            Assert.False(scheduler.HasWork);
            Assert.Empty(scheduler.Tick(0, ChunkCountOf));
        }

        [Fact]
        public void ATerritoryIsRebuiltAChunkAFrameUntilItIsDone()
        {
            var scheduler = new PoliticalMapRefreshScheduler();
            scheduler.MarkDirty("county_kent");

            Assert.Equal(new[] { new PoliticalMapChunk("county_kent", 0, 3) }, scheduler.Tick(0.00, ChunkCountOf));
            Assert.Equal(new[] { new PoliticalMapChunk("county_kent", 1, 3) }, scheduler.Tick(0.02, ChunkCountOf));
            Assert.Equal(new[] { new PoliticalMapChunk("county_kent", 2, 3) }, scheduler.Tick(0.04, ChunkCountOf));
            Assert.Empty(scheduler.Tick(0.06, ChunkCountOf));
            Assert.False(scheduler.HasWork);
        }

        [Fact]
        public void TheBudgetCarriesOverIntoTheNextTerritoryWithinAFrame()
        {
            var scheduler = new PoliticalMapRefreshScheduler(chunksPerFrame: 4);
            scheduler.MarkDirty(new[] { "county_kent", "county_york" });

            Assert.Equal(
                new[] { ("county_kent", 0), ("county_kent", 1), ("county_kent", 2), ("county_york", 0) },
                Ids(scheduler.Tick(0, ChunkCountOf)));
            Assert.Equal(new[] { ("county_york", 1) }, Ids(scheduler.Tick(0.02, ChunkCountOf)));
        }

        [Fact]
        public void RepeatedMarksAreServedOnceInTheOrderTheyFirstArrived()
        {
            var scheduler = new PoliticalMapRefreshScheduler(chunksPerFrame: 10);
            scheduler.MarkDirty(new[] { "county_york", "county_kent", "county_york", "county_kent" });

            Assert.Equal(new[] { "county_york", "county_kent" }, scheduler.Pending);
            Assert.Equal(5, scheduler.Tick(0, ChunkCountOf).Count);
            Assert.False(scheduler.HasWork);
        }

        [Fact]
        public void ATerritoryWithNoCellsIsPassedOverWithoutUsingTheBudget()
        {
            var scheduler = new PoliticalMapRefreshScheduler();
            scheduler.MarkDirty(new[] { "county_empty", "county_york" });

            Assert.Equal(new[] { ("county_york", 0) }, Ids(scheduler.Tick(0, ChunkCountOf)));
        }

        [Fact]
        public void ATerritoryMarkedAgainSoonAfterItStartedWaitsOutTheThrottle()
        {
            var scheduler = new PoliticalMapRefreshScheduler(chunksPerFrame: 10, minSecondsBetweenRebuilds: 1.0);
            scheduler.MarkDirty("county_york");
            scheduler.Tick(0, ChunkCountOf);

            scheduler.MarkDirty("county_york");

            Assert.Empty(scheduler.Tick(0.5, ChunkCountOf));
            Assert.True(scheduler.HasWork);
            Assert.Equal(new[] { ("county_york", 0), ("county_york", 1) }, Ids(scheduler.Tick(1.0, ChunkCountOf)));
        }

        [Fact]
        public void AThrottledTerritoryDoesNotHoldUpTheOnesQueuedBehindIt()
        {
            var scheduler = new PoliticalMapRefreshScheduler(chunksPerFrame: 10, minSecondsBetweenRebuilds: 1.0);
            scheduler.MarkDirty("county_york");
            scheduler.Tick(0, ChunkCountOf);

            scheduler.MarkDirty(new[] { "county_york", "county_kent" });

            Assert.Equal(new[] { ("county_kent", 0), ("county_kent", 1), ("county_kent", 2) }, Ids(scheduler.Tick(0.1, ChunkCountOf)));
            Assert.Equal(new[] { "county_york" }, scheduler.Pending);
        }

        [Fact]
        public void ATerritoryMarkedPartWayThroughItsRebuildIsRebuiltAgainFromTheStart()
        {
            // Its finished chunks were painted with the colours from before the change.
            var scheduler = new PoliticalMapRefreshScheduler(minSecondsBetweenRebuilds: 1.0);
            scheduler.MarkDirty("county_kent");
            scheduler.Tick(0, ChunkCountOf);

            scheduler.MarkDirty("county_kent");

            Assert.Equal(new[] { ("county_kent", 1) }, Ids(scheduler.Tick(0.02, ChunkCountOf)));
            Assert.Equal(new[] { ("county_kent", 2) }, Ids(scheduler.Tick(0.04, ChunkCountOf)));
            Assert.Empty(scheduler.Tick(0.06, ChunkCountOf));
            Assert.Equal(new[] { ("county_kent", 0) }, Ids(scheduler.Tick(1.0, ChunkCountOf)));
        }

        [Fact]
        public void ClearingDropsQueuedAndUnfinishedWork()
        {
            var scheduler = new PoliticalMapRefreshScheduler();
            scheduler.MarkDirty(new[] { "county_kent", "county_york" });
            scheduler.Tick(0, ChunkCountOf);

            scheduler.Clear();

            Assert.False(scheduler.HasWork);
            Assert.Empty(scheduler.Tick(5, ChunkCountOf));
        }

        [Fact]
        public void ABudgetBelowOneChunkIsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new PoliticalMapRefreshScheduler(chunksPerFrame: 0));
        }
    }
}
