using Bannerlord.PrivateWars.Domain;
using Bannerlord.PrivateWars.Domain.Model;
using Bannerlord.PrivateWars.Domain.Port;
using Bannerlord.PrivateWars.Registry;
using DellarteDellaGuerra.Domain.Titles;
using DellarteDellaGuerra.Infrastructure.Titles;

namespace DellarteDellaGuerra.Infrastructure.Tests.Titles;

public class FeudalHierarchyCacheTests
{
    private const string HierarchyXml = """
        <Feudalism>
          <Kingdom id="kingdom" name="Kingdom" kingClanId="king">
            <Duchy id="duchy_a" name="A" holderClanId="attacker">
              <County id="county" name="County" holderClanId="vassal" />
            </Duchy>
            <Duchy id="duchy_b" name="B" holderClanId="defender" />
          </Kingdom>
        </Feudalism>
        """;

    private readonly XmlFeudalStructure _structure = new(FeudalStructureParser.Parse(HierarchyXml));
    private readonly InMemoryTitleRegistry _titles;
    private readonly InMemoryPrivateWarRegistry _wars;

    public FeudalHierarchyCacheTests()
    {
        var genealogy = new IdentityGenealogy();
        _titles = new InMemoryTitleRegistry(genealogy);
        _titles.Initialise(_structure.BuildInitialTitles(genealogy));
        var hierarchy = new TestHierarchy(new SuzeraintyPolicy(_titles, _structure, genealogy),
            _titles, _structure);
        _wars = new InMemoryPrivateWarRegistry(new WarSideResolver(), hierarchy);
        _wars.Add(new PrivateWar("war", "attacker", "defender", [], [], "claim", "duchy_b", "seat",
            new Dictionary<string, string>(), 0, 0, 0, 0, PrivateWarStatus.Active));
    }

    [Fact]
    public void TitleTransferAndEmptySaveRestoreInvalidateCachedSides()
    {
        Assert.True(_wars.AreEnemies("vassal", "defender"));
        var duchy = _titles.GetTitle("duchy_a")!;
        _titles.SaveTitle(duchy.GrantTo("defender", 1));

        Assert.False(_wars.AreEnemies("vassal", "defender"));
        Assert.True(_wars.AreAllies("vassal", "defender"));

        _titles.Initialise([]);

        Assert.False(_wars.AreAllies("vassal", "defender"));
    }

    [Fact]
    public void ReattachmentAndSaveRestoreInvalidateCachedSides()
    {
        Assert.True(_wars.AreEnemies("vassal", "defender"));

        _structure.Reattach("county", "duchy_b");

        Assert.False(_wars.AreEnemies("vassal", "defender"));
        Assert.True(_wars.AreAllies("vassal", "defender"));

        _structure.InitialiseReattachments(new Dictionary<string, string?>());

        Assert.True(_wars.AreEnemies("vassal", "defender"));
        Assert.False(_wars.AreAllies("vassal", "defender"));
    }

    private sealed class TestHierarchy(SuzeraintyPolicy policy, InMemoryTitleRegistry titles,
        XmlFeudalStructure structure) : IVersionedSuzerainProvider
    {
        public long? Revision => titles.Revision + structure.Revision;
        public string? GetSuzerain(string clanId) => policy.GetSuzerain(clanId);
    }
}
