using DellarteDellaGuerra.Domain.Titles.Model;
using DellarteDellaGuerra.Domain.Titles.Port;
using DellarteDellaGuerra.Infrastructure.Titles;

namespace DellarteDellaGuerra.Infrastructure.Tests.Titles;

public class InMemoryTitleRegistryTests
{
    private readonly IdentityGenealogy _genealogy = new();
    private readonly InMemoryTitleRegistry _registry;

    public InMemoryTitleRegistryTests()
    {
        _registry = new InMemoryTitleRegistry(_genealogy);
    }

    private static Title YorkDuchy(string? holderClanId = "clan_york", string seat = "town_york") =>
        new Title("duchy_york", "Duchy of York", TitleRank.Duke, seat, holderClanId);

    [Fact]
    public void ShouldUpsertTitleOnSave()
    {
        _registry.SaveTitle(YorkDuchy());
        _registry.SaveTitle(YorkDuchy(holderClanId: "clan_neville"));

        Title? title = _registry.GetTitle("duchy_york");
        Assert.NotNull(title);
        Assert.Equal("clan_neville", _genealogy.GetHolderClanOf(title));
        Assert.Single(_registry.GetAllTitles());
    }

    [Fact]
    public void ShouldIndexTitleBySeat()
    {
        _registry.SaveTitle(YorkDuchy());

        Assert.Equal("duchy_york", _registry.GetTitleBySeat("town_york")?.Id);
        Assert.Null(_registry.GetTitleBySeat("town_unknown"));
    }

    [Fact]
    public void ShouldReindexSeatWhenSeatChangesOnSave()
    {
        _registry.SaveTitle(YorkDuchy());
        _registry.SaveTitle(YorkDuchy(seat: "castle_sandal"));

        Assert.Null(_registry.GetTitleBySeat("town_york"));
        Assert.Equal("duchy_york", _registry.GetTitleBySeat("castle_sandal")?.Id);
    }

    [Fact]
    public void ShouldFilterTitlesByHolderClan()
    {
        _registry.SaveTitle(YorkDuchy());
        _registry.SaveTitle(new Title("county_kent", "County of Kent", TitleRank.Earl, "town_kent", null));

        Assert.Equal(new[] { "duchy_york" }, _registry.GetTitlesByClan("clan_york").Select(title => title.Id));
        Assert.Empty(_registry.GetTitlesByClan("clan_lancaster"));
    }

    [Fact]
    public void ShouldInitialiseByClearingExistingTitles()
    {
        _registry.SaveTitle(YorkDuchy());

        _registry.Initialise([new Title("county_kent", "County of Kent", TitleRank.Earl, "town_kent", null)]);

        Assert.Null(_registry.GetTitle("duchy_york"));
        Assert.Null(_registry.GetTitleBySeat("town_york"));
        Assert.Equal("county_kent", _registry.GetTitleBySeat("town_kent")?.Id);
        Assert.Single(_registry.GetAllTitles());
    }

    [Fact]
    public void ShouldAnnounceAChangeOfHolderOrOccupantButNotAnUnchangedSave()
    {
        _registry.SaveTitle(YorkDuchy());
        var changed = new List<string>();
        _registry.HolderOrOccupantChanged += changed.Add;

        _registry.SaveTitle(YorkDuchy());
        _registry.SaveTitle(YorkDuchy(holderClanId: "clan_neville"));
        _registry.SaveTitle(_registry.GetTitle("duchy_york")!.WithOccupant("clan_percy", 10f));
        _registry.SaveTitle(new Title("county_kent", "County of Kent", TitleRank.Earl, "town_kent", null));

        Assert.Equal(new[] { "duchy_york", "duchy_york", "county_kent" }, changed);
    }

    [Fact]
    public void ShouldNotAnnounceTheTitlesItIsInitialisedWith()
    {
        var changed = new List<string>();
        _registry.HolderOrOccupantChanged += changed.Add;

        _registry.Initialise([YorkDuchy()]);
        _registry.SaveTitle(YorkDuchy(holderClanId: "clan_neville"));

        Assert.Equal(new[] { "duchy_york" }, changed);
    }

    [Fact]
    public void ShouldSnapshotAllTitles()
    {
        _registry.SaveTitle(YorkDuchy());
        _registry.SaveTitle(new Title("county_kent", "County of Kent", TitleRank.Earl, "town_kent", null));

        IReadOnlyList<Title> snapshot = _registry.Snapshot();

        Assert.Equal(2, snapshot.Count);
        Assert.Contains(snapshot, title => title.Id == "duchy_york");
        Assert.Contains(snapshot, title => title.Id == "county_kent");
    }
}
