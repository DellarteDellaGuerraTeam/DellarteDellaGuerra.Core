using DellarteDellaGuerra.Domain.PoliticalMap.Labels;
using DellarteDellaGuerra.Domain.PoliticalMap.Overlay;

namespace DellarteDellaGuerra.Domain.Tests.PoliticalMap.Labels
{
    public class PoliticalMapLabelRegionCacheTests
    {
        private static string?[][] Keys(params string?[] keys) => new[] { keys };

        private static IReadOnlyList<PoliticalMapRegion>[] Regions(string key) =>
            new IReadOnlyList<PoliticalMapRegion>[] { new[] { new PoliticalMapRegion(key, 500, 1f, 2f, 3f) } };

        [Fact]
        public void ReturnsEachOfThreeStatesWithoutFindingRegionsAgain()
        {
            var cache = new PoliticalMapLabelRegionCache();
            var kingdom = Regions("realm");
            var vassals = Regions("liege");
            var subVassals = Regions("clan");
            cache.Store(PoliticalMapView.Kingdom, PoliticalMapOwnerLevel.Vassals, Keys("realm"), kingdom);
            cache.Store(PoliticalMapView.Owners, PoliticalMapOwnerLevel.Vassals, Keys("liege"), vassals);
            cache.Store(PoliticalMapView.Owners, PoliticalMapOwnerLevel.SubVassals, Keys("clan"), subVassals);

            for (int visit = 0; visit < 3; visit++)
            {
                Assert.Same(subVassals, cache.Get(PoliticalMapView.Owners, PoliticalMapOwnerLevel.SubVassals, Keys("clan")));
                Assert.Same(kingdom, cache.Get(PoliticalMapView.Kingdom, PoliticalMapOwnerLevel.SubVassals, Keys("realm")));
                Assert.Same(vassals, cache.Get(PoliticalMapView.Owners, PoliticalMapOwnerLevel.Vassals, Keys("liege")));
            }
        }

        [Fact]
        public void SameKeysDoNotShareLayoutsBetweenViewsOrOwnerLevels()
        {
            var cache = new PoliticalMapLabelRegionCache();
            cache.Store(PoliticalMapView.Owners, PoliticalMapOwnerLevel.Vassals, Keys("clan"), Regions("clan"));

            Assert.Null(cache.Get(PoliticalMapView.Owners, PoliticalMapOwnerLevel.SubVassals, Keys("clan")));
            Assert.Null(cache.Get(PoliticalMapView.Kingdom, PoliticalMapOwnerLevel.Vassals, Keys("clan")));
        }

        [Fact]
        public void ValidatesEveryTierAndEverySettlementIncludingNullsAndOrder()
        {
            var cache = new PoliticalMapLabelRegionCache();
            string?[][] keys = { new string?[] { "realm", null }, new string?[] { "a", "b", "c" } };
            cache.Store(PoliticalMapView.Owners, PoliticalMapOwnerLevel.SubVassals, keys, Regions("a"));

            Assert.NotNull(cache.Get(PoliticalMapView.Owners, PoliticalMapOwnerLevel.SubVassals, keys));
            AssertMiss(new[] { keys[0], new string?[] { "a", "b", "changed" } });
            AssertMiss(new[] { new string?[] { "realm", "owner" }, keys[1] });
            AssertMiss(new[] { keys[0], new string?[] { "b", "a", "c" } });
            AssertMiss(new[] { keys[0], new string?[] { "a", "b" } });
            AssertMiss(new[] { keys[0] });
            AssertMiss(new[] { new string?[] { "Realm", null }, keys[1] });

            void AssertMiss(string?[][] changed) =>
                Assert.Null(cache.Get(PoliticalMapView.Owners, PoliticalMapOwnerLevel.SubVassals, changed));
        }

        [Theory]
        [InlineData(PoliticalMapView.Kingdom, PoliticalMapOwnerLevel.SubVassals)]
        [InlineData(PoliticalMapView.Owners, PoliticalMapOwnerLevel.Vassals)]
        [InlineData(PoliticalMapView.Owners, PoliticalMapOwnerLevel.SubVassals)]
        public void ChangedKeysReplaceOnlyThatStatesSnapshot(PoliticalMapView view, PoliticalMapOwnerLevel level)
        {
            var cache = new PoliticalMapLabelRegionCache();
            var latest = Regions("latest");
            for (int change = 0; change < 10; change++)
                cache.Store(view, level, Keys(change.ToString()), Regions(change.ToString()));
            cache.Store(view, level, Keys("latest"), latest);

            Assert.Null(cache.Get(view, level, Keys("0")));
            Assert.Null(cache.Get(view, level, Keys("9")));
            Assert.Same(latest, cache.Get(view, level, Keys("latest")));
        }

        [Fact]
        public void RetainsIndependentKeySnapshotWhenCallerChangesItsArrays()
        {
            var cache = new PoliticalMapLabelRegionCache();
            var keys = Keys("original", null);
            var regions = Regions("original");
            cache.Store(PoliticalMapView.Owners, PoliticalMapOwnerLevel.Vassals, keys, regions);
            keys[0][0] = "changed";
            keys[0] = new string?[] { "replaced" };

            Assert.Same(regions, cache.Get(PoliticalMapView.Owners, PoliticalMapOwnerLevel.Vassals, Keys("original", null)));
            Assert.Null(cache.Get(PoliticalMapView.Owners, PoliticalMapOwnerLevel.Vassals, keys));
        }

        [Fact]
        public void ClearDropsAllStatesEvenIfNextGridOrSessionHasIdenticalKeys()
        {
            var cache = new PoliticalMapLabelRegionCache();
            cache.Store(PoliticalMapView.Kingdom, PoliticalMapOwnerLevel.Vassals, Keys("realm"), Regions("realm"));
            cache.Store(PoliticalMapView.Owners, PoliticalMapOwnerLevel.Vassals, Keys("liege"), Regions("liege"));
            cache.Store(PoliticalMapView.Owners, PoliticalMapOwnerLevel.SubVassals, Keys("clan"), Regions("clan"));

            cache.Clear();

            Assert.Null(cache.Get(PoliticalMapView.Kingdom, PoliticalMapOwnerLevel.Vassals, Keys("realm")));
            Assert.Null(cache.Get(PoliticalMapView.Owners, PoliticalMapOwnerLevel.Vassals, Keys("liege")));
            Assert.Null(cache.Get(PoliticalMapView.Owners, PoliticalMapOwnerLevel.SubVassals, Keys("clan")));
        }

        [Fact]
        public void OffHasNoCacheSlot()
        {
            var cache = new PoliticalMapLabelRegionCache();
            cache.Store(PoliticalMapView.Off, PoliticalMapOwnerLevel.Vassals, Keys(), Array.Empty<IReadOnlyList<PoliticalMapRegion>>());

            Assert.Null(cache.Get(PoliticalMapView.Off, PoliticalMapOwnerLevel.Vassals, Keys()));
        }
    }
}
