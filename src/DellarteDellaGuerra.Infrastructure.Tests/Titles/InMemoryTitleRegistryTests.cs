using DellarteDellaGuerra.Domain.Titles.Model;
using DellarteDellaGuerra.Infrastructure.Titles;

namespace DellarteDellaGuerra.Infrastructure.Tests.Titles;

public class InMemoryTitleRegistryTests
{
    private readonly InMemoryTitleRegistry _registry = new InMemoryTitleRegistry();

    private static Title YorkDuchy(string? holderClanId = "clan_york", string seat = "town_york") =>
        new Title("duchy_york", "Duchy of York", TitleRank.Duke, seat, holderClanId);

    [Fact]
    public void ShouldUpsertTitleOnSave()
    {
        _registry.SaveTitle(YorkDuchy());
        _registry.SaveTitle(YorkDuchy(holderClanId: "clan_neville"));

        Title? title = _registry.GetTitle("duchy_york");
        Assert.NotNull(title);
        Assert.Equal("clan_neville", title!.HolderClanId);
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
        _registry.SaveTitle(new Title("county_kent", "County of Kent", TitleRank.Count, "town_kent", null));

        Assert.Equal(new[] { "duchy_york" }, _registry.GetTitlesByClan("clan_york").Select(title => title.Id));
        Assert.Empty(_registry.GetTitlesByClan("clan_lancaster"));
    }

    [Fact]
    public void ShouldInitialiseByClearingExistingTitles()
    {
        _registry.SaveTitle(YorkDuchy());

        _registry.Initialise([new Title("county_kent", "County of Kent", TitleRank.Count, "town_kent", null)]);

        Assert.Null(_registry.GetTitle("duchy_york"));
        Assert.Null(_registry.GetTitleBySeat("town_york"));
        Assert.Equal("county_kent", _registry.GetTitleBySeat("town_kent")?.Id);
        Assert.Single(_registry.GetAllTitles());
    }

    [Fact]
    public void ShouldSnapshotAllTitles()
    {
        _registry.SaveTitle(YorkDuchy());
        _registry.SaveTitle(new Title("county_kent", "County of Kent", TitleRank.Count, "town_kent", null));

        IReadOnlyList<Title> snapshot = _registry.Snapshot();

        Assert.Equal(2, snapshot.Count);
        Assert.Contains(snapshot, title => title.Id == "duchy_york");
        Assert.Contains(snapshot, title => title.Id == "county_kent");
    }
}
