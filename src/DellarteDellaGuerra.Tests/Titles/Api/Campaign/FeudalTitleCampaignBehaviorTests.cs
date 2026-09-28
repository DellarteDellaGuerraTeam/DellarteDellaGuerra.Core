using System.Reflection;
using DellarteDellaGuerra.Domain.Titles;
using DellarteDellaGuerra.Domain.Titles.Model;
using DellarteDellaGuerra.Titles.Api.Campaign;

namespace DellarteDellaGuerra.Tests.Titles.Api.Campaign
{
    public class FeudalTitleCampaignBehaviorTests
    {
        private static readonly Title ConfiguredYork =
            new Title("duchy_york", "Duchy of York", TitleRank.Duke, "town_york", "york_richard");

        private static readonly Title WonYork =
            new Title("duchy_york", "Duchy of York", TitleRank.Duke, "town_york", "lancaster_henry");

        private static readonly Claim EarlierCampaignClaim =
            new Claim("claim_1", "clan_percy", "duchy_york", ClaimStrength.Weak, ClaimOrigin.Conquest);

        [Fact]
        public void ANewCampaignStartsFromTheConfiguredTitlesWhateverAnEarlierOneLeftBehind()
        {
            var store = new FakeStateStore();
            store.InitialiseTitles(new[] { WonYork });
            store.InitialiseClaims(new[] { EarlierCampaignClaim });
            var bloodClaims = new CountingBloodClaims();

            Invoke(NewBehavior(store, bloodClaims), "OnNewGameCreated");

            Assert.Equal("york_richard", Assert.Single(store.SnapshotTitles()).HolderHeroId);
            Assert.Empty(store.SnapshotClaims());
            Assert.Equal(1, bloodClaims.Runs);
        }

        [Fact]
        public void ASaveWithoutFeudalStateStartsFromTheConfiguredTitlesWhateverAnEarlierOneLeftBehind()
        {
            var store = new FakeStateStore();
            store.InitialiseTitles(new[] { WonYork });
            store.InitialiseClaims(new[] { EarlierCampaignClaim });
            var bloodClaims = new CountingBloodClaims();

            Invoke(NewBehavior(store, bloodClaims), "OnGameLoaded");

            Assert.Equal("york_richard", Assert.Single(store.SnapshotTitles()).HolderHeroId);
            Assert.Empty(store.SnapshotClaims());
            Assert.Equal(1, bloodClaims.Runs);
        }

        private static FeudalTitleCampaignBehavior NewBehavior(FakeStateStore store, CountingBloodClaims bloodClaims) =>
            new FeudalTitleCampaignBehavior(null!, bloodClaims, null!, store, () => new[] { ConfiguredYork });

        // The handlers are private campaign-event listeners; the campaign starter argument is unused.
        private static void Invoke(FeudalTitleCampaignBehavior behavior, string handler) =>
            typeof(FeudalTitleCampaignBehavior)
                .GetMethod(handler, BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(behavior, new object?[] { null });

        private sealed class CountingBloodClaims : IGenerateBloodClaimsUseCase
        {
            public int Runs { get; private set; }

            public IReadOnlyList<Claim> Execute()
            {
                Runs++;
                return Array.Empty<Claim>();
            }
        }

        private sealed class FakeStateStore : IFeudalStateStore
        {
            private List<Title> _titles = new();
            private List<Claim> _claims = new();
            private IReadOnlyDictionary<string, string?> _reattachments = new Dictionary<string, string?>();

            public void InitialiseTitles(IEnumerable<Title> titles) => _titles = titles.ToList();
            public IReadOnlyList<Title> SnapshotTitles() => _titles;
            public void InitialiseClaims(IEnumerable<Claim> claims) => _claims = claims.ToList();
            public IReadOnlyList<Claim> SnapshotClaims() => _claims;
            public void InitialiseReattachments(IReadOnlyDictionary<string, string?> suzerainTitleIdByTitleId) =>
                _reattachments = suzerainTitleIdByTitleId;
            public IReadOnlyDictionary<string, string?> SnapshotReattachments() => _reattachments;
        }
    }
}
