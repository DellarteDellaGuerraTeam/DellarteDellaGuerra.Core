using System.Reflection;
using DellarteDellaGuerra.Domain.PrivateWars.Model;
using DellarteDellaGuerra.Domain.Titles.Model;
using DellarteDellaGuerra.PrivateWars.Api.Campaign;
using DellarteDellaGuerra.PrivateWars.Spi;
using DellarteDellaGuerra.Titles.Api.Campaign;

namespace DellarteDellaGuerra.Tests.PrivateWars;

public class PrivateWarStateSerialiserTests
{
    [Fact]
    public void CurrentTwelveFieldRecord_RoundTrips()
    {
        var original = new PrivateWar(
            "war-1", "attacker", "defender", "claim", "title", "goal",
            new Dictionary<string, string>
            {
                ["town"] = "defender",
                ["castle"] = "attacker"
            },
            12.5f, -7.25f, 100.5f, 110.75f, PrivateWarStatus.Active);

        var serialised = PrivateWarStateSerialiser.SerialiseWars(new[] { original });
        var loaded = Assert.Single(PrivateWarStateSerialiser.DeserialiseWars(serialised));

        AssertWarEqual(original, loaded);
    }

    [Fact]
    public void LegacyElevenFieldRecord_UsesStartDayForGoalLastTakenDay()
    {
        const string legacy =
            "war-legacy|attacker|defender|claim|title|goal|12.5|-7.25|100.5|Active|town:defender";

        var loaded = Assert.Single(PrivateWarStateSerialiser.DeserialiseWars(new List<string> { legacy }));

        Assert.Equal(100.5f, loaded.StartDay);
        Assert.Equal(100.5f, loaded.GoalLastTakenDay);
        Assert.Equal("defender", loaded.OriginalFiefOwners["town"]);
    }

    [Fact]
    public void NonNumericOptionalGoalLastTakenDay_PreservesStartDayFallback()
    {
        const string malformedOptional =
            "war-current|attacker|defender|claim|title|goal|12.5|-7.25|100.5|Active|town:defender|not-a-day";

        var loaded = Assert.Single(
            PrivateWarStateSerialiser.DeserialiseWars(new List<string> { malformedOptional }));

        Assert.Equal(100.5f, loaded.StartDay);
        Assert.Equal(100.5f, loaded.GoalLastTakenDay);
    }

    [Fact]
    public void MalformedRecord_DoesNotPoisonFollowingValidRecord()
    {
        const string valid =
            "war-valid|attacker|defender|claim|title|goal|12.5|-7.25|100.5|Active|town:defender|110.75";
        List<PrivateWar>? loaded = null;

        var exception = Record.Exception(() => loaded = PrivateWarStateSerialiser.DeserialiseWars(
            new List<string> { null!, "not|enough|fields", valid }));

        Assert.Null(exception);
        Assert.Equal("war-valid", Assert.Single(loaded!).Id);
    }

    [Fact]
    public void GameLoad_RestoresLoadedWarIntoRuntimeHostilityRegistry()
    {
        const string savedWar =
            "war-loaded|attacker|defender|claim|title|goal|12.5|-7.25|100.5|Active|town:defender|110.75";
        var stateStore = new RuntimePrivateWarStateStore();
        var behavior = CreateBehavior(stateStore);

        SetSerialisedWars(behavior, new List<string> { savedWar });
        InvokeOnGameLoaded(behavior);

        Assert.Equal(1, stateStore.InitialisationCount);
        Assert.True(stateStore.AreEnemies("attacker", "defender"));
        Assert.Equal("war-loaded", Assert.Single(stateStore.PrivateWars).Id);
    }

    [Fact]
    public void GameLoad_WithNoSavedWars_ClearsStaleRuntimeHostility()
    {
        var staleWar = new PrivateWar(
            "war-stale", "attacker", "defender", "claim", "title", "goal",
            new Dictionary<string, string>(), 0f, 0f, 10f, 10f, PrivateWarStatus.Active);
        var stateStore = new RuntimePrivateWarStateStore();
        stateStore.InitialisePrivateWars(new[] { staleWar });
        var behavior = CreateBehavior(stateStore);

        SetSerialisedWars(behavior, new List<string>());
        InvokeOnGameLoaded(behavior);

        Assert.False(stateStore.AreEnemies("attacker", "defender"));
        Assert.Empty(stateStore.PrivateWars);
        Assert.Equal(2, stateStore.InitialisationCount);
    }

    private static void AssertWarEqual(PrivateWar expected, PrivateWar actual)
    {
        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.AttackerPrincipalClanId, actual.AttackerPrincipalClanId);
        Assert.Equal(expected.DefenderPrincipalClanId, actual.DefenderPrincipalClanId);
        Assert.Equal(expected.CasusBelliType, actual.CasusBelliType);
        Assert.Equal(expected.TitleId, actual.TitleId);
        Assert.Equal(expected.MainGoalSettlementId, actual.MainGoalSettlementId);
        Assert.Equal(expected.OriginalFiefOwners, actual.OriginalFiefOwners);
        Assert.Equal(expected.BattleScore, actual.BattleScore);
        Assert.Equal(expected.Score, actual.Score);
        Assert.Equal(expected.StartDay, actual.StartDay);
        Assert.Equal(expected.GoalLastTakenDay, actual.GoalLastTakenDay);
        Assert.Equal(expected.Status, actual.Status);
    }

    private static PrivateWarCampaignBehavior CreateBehavior(IFeudalStateStore stateStore)
        => new(stateStore, null!, null!, null!, null!, null!, null!, null!);

    private static void SetSerialisedWars(PrivateWarCampaignBehavior behavior, List<string> serialisedWars)
        => typeof(PrivateWarCampaignBehavior)
            .GetField("_serialisedWars", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(behavior, serialisedWars);

    private static void InvokeOnGameLoaded(PrivateWarCampaignBehavior behavior)
        => typeof(PrivateWarCampaignBehavior)
            .GetMethod("OnGameLoaded", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(behavior, new object?[] { null });

    private sealed class RuntimePrivateWarStateStore : IFeudalStateStore
    {
        public int InitialisationCount { get; private set; }
        public IReadOnlyList<PrivateWar> PrivateWars { get; private set; } = Array.Empty<PrivateWar>();

        public bool AreEnemies(string firstClanId, string secondClanId)
            => PrivateWars.Any(war =>
                war.Status == PrivateWarStatus.Active
                && (war.AttackerPrincipalClanId == firstClanId && war.DefenderPrincipalClanId == secondClanId
                    || war.AttackerPrincipalClanId == secondClanId && war.DefenderPrincipalClanId == firstClanId));

        public void InitialisePrivateWars(IEnumerable<PrivateWar> wars)
        {
            InitialisationCount++;
            PrivateWars = wars.ToList();
        }

        public IReadOnlyList<PrivateWar> SnapshotPrivateWars() => PrivateWars;
        public void InitialiseTitles(IEnumerable<Title> titles) => throw new NotSupportedException();
        public IReadOnlyList<Title> SnapshotTitles() => throw new NotSupportedException();
        public void InitialiseClaims(IEnumerable<Claim> claims) => throw new NotSupportedException();
        public IReadOnlyList<Claim> SnapshotClaims() => throw new NotSupportedException();
        public void InitialiseTensions(IEnumerable<FeudalTension> tensions) => throw new NotSupportedException();
        public IReadOnlyList<FeudalTension> SnapshotTensions() => throw new NotSupportedException();
    }
}
