using DellarteDellaGuerra.Domain.PoliticalMap.Grid;
using DellarteDellaGuerra.Domain.PoliticalMap.Labels;
using DellarteDellaGuerra.Domain.PoliticalMap.Overlay;

namespace DellarteDellaGuerra.Domain.Tests.PoliticalMap.Labels
{
    public class PoliticalMapOwnerLabelAnchorsTests
    {
        private static PoliticalMapGrid Grid(params string[] rows)
        {
            int width = rows[0].Length;
            return new PoliticalMapGrid(width, rows.Length, 2f, new[] { "s1", "s2", "s3", "s4" },
                rows.SelectMany(row => row.Select(c => c == '.' ? PoliticalMapGrid.None : (ushort)(c - '0'))).ToArray());
        }

        private static IReadOnlyList<PoliticalMapRegion> Find(PoliticalMapGrid grid, string?[] keys) =>
            PoliticalMapRegions.Find(grid, keys, 1);

        [Fact]
        public void ContinuingHolderUsesDetailedOwnLandAnchorAndKeepsCoarseSizing()
        {
            var grid = Grid("111222222", "111222222", "111222222");
            string?[] coarseKeys = { "ruler", "ruler" };
            var coarse = Assert.Single(Find(grid, coarseKeys));
            var detailed = Find(grid, new[] { "ruler", "vassal" });
            var ownLand = Assert.Single(detailed, region => region.Key == "ruler");

            var anchored = Assert.Single(PoliticalMapOwnerLabelAnchors.Apply(grid, coarseKeys, new[] { coarse }, detailed));

            Assert.NotEqual(coarse.X, ownLand.X);
            Assert.Equal((ownLand.X, ownLand.Y), (anchored.X, anchored.Y));
            Assert.Equal("s1", grid.SettlementAt(anchored.X, anchored.Y));
            Assert.Equal(coarse.Key, anchored.Key);
            Assert.Equal(coarse.CellCount, anchored.CellCount);
            Assert.Equal(coarse.Radius, anchored.Radius);
        }

        [Fact]
        public void DisconnectedCoarseRegionsUseTheirOwnComponentNotGlobalLargestAnchor()
        {
            var grid = Grid("11222.33334444", "11222.33334444");
            string?[] coarseKeys = { "ruler", "ruler", "ruler", "ruler" };
            var coarse = Find(grid, coarseKeys);
            var detailed = Find(grid, new[] { "ruler", "left-vassal", "ruler", "right-vassal" });

            var anchored = PoliticalMapOwnerLabelAnchors.Apply(grid, coarseKeys, coarse, detailed);

            Assert.Equal(2, anchored.Count);
            Assert.Equal(new[] { "s3", "s1" }, anchored.Select(region => grid.SettlementAt(region.X, region.Y)));
            Assert.Equal(coarse.Select(region => region.CellCount), anchored.Select(region => region.CellCount));
            Assert.All(anchored, region => Assert.Contains(detailed,
                own => own.Key == region.Key && own.X == region.X && own.Y == region.Y));
            Assert.Equal(anchored.Reverse().ToArray(), PoliticalMapOwnerLabelAnchors.Apply(grid, coarseKeys,
                coarse.Reverse().ToArray(), detailed.Reverse().ToArray()).ToArray());
        }

        [Fact]
        public void LargestOwnFragmentWinsRegardlessOfDetailedRegionOrder()
        {
            var grid = Grid("112223333", "112223333");
            string?[] coarseKeys = { "ruler", "ruler", "ruler" };
            var coarse = Find(grid, coarseKeys);
            var detailed = Find(grid, new[] { "ruler", "vassal", "ruler" });

            var forward = PoliticalMapOwnerLabelAnchors.Apply(grid, coarseKeys, coarse, detailed);
            var reverse = PoliticalMapOwnerLabelAnchors.Apply(grid, coarseKeys, coarse, detailed.Reverse().ToArray());

            Assert.Equal(forward.ToArray(), reverse.ToArray());
            Assert.Equal("s3", grid.SettlementAt(forward[0].X, forward[0].Y));
        }

        [Theory]
        [InlineData("112233", "112233")]
        [InlineData("1122", "1122", "3322", "3322")]
        public void EqualSizedFragmentsTieBreakByWorldCoordinates(params string[] rows)
        {
            var grid = Grid(rows);
            string?[] coarseKeys = { "ruler", "ruler", "ruler" };
            var coarse = Find(grid, coarseKeys);
            var detailed = Find(grid, new[] { "ruler", "vassal", "ruler" });

            var anchored = Assert.Single(PoliticalMapOwnerLabelAnchors.Apply(grid, coarseKeys, coarse, detailed.Reverse().ToArray()));

            Assert.Equal("s1", grid.SettlementAt(anchored.X, anchored.Y));
        }

        [Fact]
        public void FragmentWithoutDetailedOwnLandKeepsItsPoleInsteadOfJumpingAcrossWater()
        {
            var grid = Grid("111.222");
            string?[] coarseKeys = { "ruler", "ruler" };
            var coarse = Find(grid, coarseKeys);
            var detailed = Find(grid, new[] { "ruler", "vassal" });

            var anchored = PoliticalMapOwnerLabelAnchors.Apply(grid, coarseKeys, coarse, detailed);

            Assert.Equal(coarse.ToArray(), anchored.ToArray());
        }

        [Fact]
        public void MissingDetailedLabelsKeepCoarseLayoutAndEmptyCoarseLayoutStaysEmpty()
        {
            var grid = Grid("111222");
            string?[] coarseKeys = { "ruler", "ruler" };
            var coarse = Find(grid, coarseKeys);
            var none = Array.Empty<PoliticalMapRegion>();

            Assert.Same(coarse, PoliticalMapOwnerLabelAnchors.Apply(grid, coarseKeys, coarse, none));
            Assert.Same(none, PoliticalMapOwnerLabelAnchors.Apply(grid, coarseKeys, none, coarse));
        }

        [Fact]
        public void CoarseAnchorCacheInvalidatesWhenOnlyDetailedOwnershipChanges()
        {
            var grid = Grid("111222222");
            string?[] coarseKeys = { "ruler", "ruler" };
            string?[] oldDetailedKeys = { "ruler", "vassal" };
            string?[] newDetailedKeys = { "vassal", "ruler" };
            var coarse = Find(grid, coarseKeys);
            var oldRegions = PoliticalMapOwnerLabelAnchors.Apply(grid, coarseKeys, coarse, Find(grid, oldDetailedKeys));
            var newRegions = PoliticalMapOwnerLabelAnchors.Apply(grid, coarseKeys, coarse, Find(grid, newDetailedKeys));
            var cache = new PoliticalMapLabelRegionCache();
            cache.Store(PoliticalMapView.Owners, PoliticalMapOwnerLevel.Vassals,
                new[] { coarseKeys, oldDetailedKeys }, new[] { oldRegions });

            Assert.NotNull(cache.Get(PoliticalMapView.Owners, PoliticalMapOwnerLevel.Vassals, new[] { coarseKeys, oldDetailedKeys }));
            Assert.Null(cache.Get(PoliticalMapView.Owners, PoliticalMapOwnerLevel.Vassals, new[] { coarseKeys, newDetailedKeys }));
            Assert.NotEqual(oldRegions[0].X, newRegions[0].X);
            Assert.Equal("s2", grid.SettlementAt(newRegions[0].X, newRegions[0].Y));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ColdOrPrewarmedDetailedLayoutProducesTheSameCachedWorldAnchor(bool prewarmDetailed)
        {
            var grid = Grid("111222222", "111222222");
            string?[] coarseKeys = { "ruler", "ruler" };
            string?[] detailedKeys = { "ruler", "vassal" };
            var cache = new PoliticalMapLabelRegionCache();
            if (prewarmDetailed)
                cache.Store(PoliticalMapView.Owners, PoliticalMapOwnerLevel.SubVassals,
                    new[] { detailedKeys }, new[] { Find(grid, detailedKeys) });
            var detailed = cache.Get(PoliticalMapView.Owners, PoliticalMapOwnerLevel.SubVassals,
                new[] { detailedKeys })?[0] ?? Find(grid, detailedKeys);
            var anchored = PoliticalMapOwnerLabelAnchors.Apply(grid, coarseKeys, Find(grid, coarseKeys), detailed);
            cache.Store(PoliticalMapView.Owners, PoliticalMapOwnerLevel.Vassals,
                new[] { coarseKeys, detailedKeys }, new[] { anchored });
            cache.Store(PoliticalMapView.Owners, PoliticalMapOwnerLevel.SubVassals, new[] { detailedKeys }, new[] { detailed });

            var atCoarse = cache.Get(PoliticalMapView.Owners, PoliticalMapOwnerLevel.Vassals, new[] { coarseKeys, detailedKeys })![0][0];
            var atDetailed = Assert.Single(cache.Get(PoliticalMapView.Owners, PoliticalMapOwnerLevel.SubVassals,
                new[] { detailedKeys })![0], region => region.Key == atCoarse.Key);
            Assert.Equal((atDetailed.X, atDetailed.Y), (atCoarse.X, atCoarse.Y));
            Assert.Equal(atCoarse, cache.Get(PoliticalMapView.Owners, PoliticalMapOwnerLevel.Vassals,
                new[] { coarseKeys, detailedKeys })![0][0]);
        }
    }
}
