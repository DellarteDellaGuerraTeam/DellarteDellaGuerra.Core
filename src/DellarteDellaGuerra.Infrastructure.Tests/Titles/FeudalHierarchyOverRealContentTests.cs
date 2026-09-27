using DellarteDellaGuerra.Domain.Titles;
using DellarteDellaGuerra.Domain.Titles.Model;
using DellarteDellaGuerra.Domain.Titles.Port;
using DellarteDellaGuerra.Infrastructure.Titles;

namespace DellarteDellaGuerra.Infrastructure.Tests.Titles;

/// <summary>
/// Runs the three hierarchy use cases — suzerain, direct vassals, de jure settlements — over the
/// real 1471 title configuration. Their unit tests build tidy trees; the authored one is not tidy,
/// because it was written to look like England rather than to look like a tree. Houses hold land
/// of houses that hold land of them, eight counties answer straight to the crown with no duchy in
/// between, and the crown's own dignity has no seat at all.
/// </summary>
/// <remarks>
/// Nothing here needs a campaign: the hierarchy is static configuration joined to the genealogy,
/// so the production adapters read it exactly as they do at runtime.
/// </remarks>
public class FeudalHierarchyOverRealContentTests
{
    private const string Crown = "clan_lancaster";
    private const string ScottishCrown = "clan_stewart";
    private const string IslesCrown = "clan_macdonald_isles";

    [Fact]
    [Trait("Category", "DADG content integration")]
    public void EveryHouseInTheRealmAnswersToTheCrownThroughAChainThatTerminates()
    {
        // The chain is walked at runtime to resolve war sides, so a loop in it would hang the
        // daily tick rather than merely mis-answer. Seventy-nine houses hold land across three
        // realms, and only the three kings answer to nobody. Fifty-three of them are England's,
        // and every one arrives at Lancaster in at most three steps; Scotland's thirteen arrive
        // at Stewart and the Isles' thirteen at MacDonald, in at most two.
        Content content = Content.Load();
        IReadOnlyList<string> holdingClans = content.HoldingClans();

        Assert.Equal(79, holdingClans.Count);
        Assert.Equal(
            new[] { Crown, ScottishCrown, IslesCrown }.OrderBy(clan => clan),
            holdingClans.Where(clan => content.Suzerain(clan) is null));

        var depths = new Dictionary<string, int>();
        var realms = new Dictionary<string, string>();
        foreach (string clanId in holdingClans)
        {
            var walked = new List<string> { clanId };
            string? current = content.Suzerain(clanId);
            while (current is not null)
            {
                Assert.DoesNotContain(current, walked);
                walked.Add(current);
                current = content.Suzerain(current);
            }

            realms[clanId] = walked[^1];
            depths[clanId] = walked.Count - 1;
        }

        Assert.Equal(53, realms.Count(entry => entry.Value == Crown));
        Assert.Equal(13, realms.Count(entry => entry.Value == ScottishCrown));
        Assert.Equal(13, realms.Count(entry => entry.Value == IslesCrown));

        // Each realm's houses are the houses holding land in that realm's own title tree.
        Assert.Equal(content.HoldingClansUnder("kingdom_england"), InRealm(Crown));
        Assert.Equal(content.HoldingClansUnder("kingdom_scotland"), InRealm(ScottishCrown));
        Assert.Equal(content.HoldingClansUnder("kingdom_isles"), InRealm(IslesCrown));

        Assert.Equal(0, depths[Crown]);
        Assert.Equal(3, InRealm(Crown).Max(clan => depths[clan]));
        Assert.Equal(2, InRealm(ScottishCrown).Max(clan => depths[clan]));
        Assert.Equal(2, InRealm(IslesCrown).Max(clan => depths[clan]));

        IEnumerable<string> InRealm(string king) =>
            realms.Where(entry => entry.Value == king).Select(entry => entry.Key).OrderBy(clan => clan);
    }

    [Fact]
    [Trait("Category", "DADG content integration")]
    public void TheSuzerainAndDirectVassalViewsPartitionTheRealmTheSameWay()
    {
        // Two use cases answer the same question from opposite ends, and the campaign layer uses
        // both. They agree here on all seventy-nine houses: each non-royal house is named by
        // exactly one liege, and the three kings are named by none, which is the same statement
        // as "the chains form one tree per realm" arrived at the other way round. England's tree
        // is unchanged by the other two: seventeen houses answer straight to Lancaster, and
        // sixteen of England's houses have vassals of their own.
        Content content = Content.Load();
        IReadOnlyList<string> holdingClans = content.HoldingClans();
        string[] kings = { Crown, ScottishCrown, IslesCrown };

        var vassalsByLiege = holdingClans.ToDictionary(clan => clan, content.DirectVassals);
        IReadOnlyList<string> england = content.HoldingClansUnder("kingdom_england");

        Assert.Equal(17, vassalsByLiege[Crown].Count);
        Assert.Equal(16, vassalsByLiege.Count(entry => england.Contains(entry.Key) && entry.Value.Count > 0));

        foreach (string clanId in holdingClans)
        {
            string[] liegesNamingIt = vassalsByLiege
                .Where(entry => entry.Value.Contains(clanId))
                .Select(entry => entry.Key)
                .ToArray();

            if (kings.Contains(clanId)) Assert.Empty(liegesNamingIt);
            else Assert.Equal(content.Suzerain(clanId), Assert.Single(liegesNamingIt));
        }

        Assert.All(
            vassalsByLiege,
            entry => Assert.All(entry.Value, vassal => Assert.Equal(entry.Key, content.Suzerain(vassal))));
    }

    [Fact]
    [Trait("Category", "DADG content integration")]
    public void AHouseAnswersForItsHighestDignityEvenWhereItHoldsLandOfItsOwnVassal()
    {
        // York and Talbot each hold land of the other. Talbot's county of Shropshire is held of
        // the crown, and York holds the barony of Ludlow beneath it; York's duchy is also held of
        // the crown, and Talbot's county of Hallamshire sits beneath that. Tenure alone would make
        // this a cycle. It does not, because the walk starts from the house's highest dignity
        // only: York is a duke and answers to the king, so Talbot answers to York and the barony
        // York holds of Talbot changes nothing.
        Content content = Content.Load();

        Assert.Equal(TitleRank.Duke, content.HighestRank("clan_york"));
        Assert.Equal(TitleRank.Count, content.HighestRank("clan_talbot"));

        Assert.Equal(Crown, content.Suzerain("clan_york"));
        Assert.Equal("clan_york", content.Suzerain("clan_talbot"));

        Assert.Equal("clan_york", content.ClanHolding("barony_ludlow"));
        Assert.Equal("clan_talbot", content.ClanHolding("county_shropshire"));
        Assert.Equal("county_shropshire", content.SuzerainTitleOf("barony_ludlow"));

        Assert.Equal("clan_talbot", content.ClanHolding("county_hallamshire"));
        Assert.Equal("duchy_york", content.SuzerainTitleOf("county_hallamshire"));

        // The rule reaches past a liege who holds nothing of his own worth naming: Clifford's
        // barony sits under a Percy county, so Clifford answers to Percy and Percy to the king.
        Assert.Equal("clan_percy", content.Suzerain("clan_clifford"));
        Assert.Equal(Crown, content.Suzerain("clan_percy"));
    }

    [Fact]
    [Trait("Category", "DADG content integration")]
    public void ATitleFlattensToItsOwnSeatPlusEverySeatBeneathIt()
    {
        // What a war is actually fought over. A barony is worth its own castle; a county gathers
        // its baronies; a duchy gathers the counties and their baronies in turn. The crown's
        // dignity gathers the realm — all ninety-six of England's seats, one per title save the
        // kingdom itself. Scotland's crown gathers twenty-six and the Isles' sixteen the same way.
        // The three kingdoms are the only titles authored without a seat of their own, and the
        // three realms between them account for every seat in the content, none twice.
        Content content = Content.Load();

        Assert.Equal(141, content.AllTitles().Count);
        Assert.Equal(
            new[] { "kingdom_england", "kingdom_isles", "kingdom_scotland" },
            content.AllTitles()
                .Where(title => string.IsNullOrEmpty(title.SeatSettlementId))
                .Select(title => title.Id)
                .OrderBy(id => id));

        IReadOnlyList<string> realm = content.DeJureSettlements("kingdom_england");
        IReadOnlyList<string> scotland = content.DeJureSettlements("kingdom_scotland");
        IReadOnlyList<string> isles = content.DeJureSettlements("kingdom_isles");

        Assert.Equal(96, realm.Count);
        Assert.Equal(96, realm.Distinct().Count());
        Assert.Equal(26, scotland.Count);
        Assert.Equal(26, scotland.Distinct().Count());
        Assert.Equal(16, isles.Count);
        Assert.Equal(16, isles.Distinct().Count());

        Assert.Equal(content.SeatsUnder("kingdom_england"), realm.OrderBy(seat => seat));
        Assert.Equal(content.SeatsUnder("kingdom_scotland"), scotland.OrderBy(seat => seat));
        Assert.Equal(content.SeatsUnder("kingdom_isles"), isles.OrderBy(seat => seat));
        Assert.Equal(
            content.AllTitles()
                .Select(title => title.SeatSettlementId)
                .Where(seat => !string.IsNullOrEmpty(seat))
                .OrderBy(seat => seat),
            realm.Concat(scotland).Concat(isles).OrderBy(seat => seat));

        // And every one of them has a house to hold it: no Scottish dignity is left to nobody.
        Assert.All(
            content.AllTitles().Where(title => title.Id != "kingdom_england" && !realm.Contains(title.SeatSettlementId)),
            title => Assert.NotNull(content.ClanHolding(title.Id)));

        Assert.Equal(14, content.DeJureSettlements("duchy_york").Count);
        Assert.Equal(5, content.DeJureSettlements("county_york").Count);
        Assert.Equal(1, content.DeJureSettlements("barony_middleham").Count);

        // The roll-up nests: York's county is inside York's duchy is inside the realm.
        Assert.Subset(
            realm.ToHashSet(),
            content.DeJureSettlements("duchy_york").ToHashSet());
        Assert.Subset(
            content.DeJureSettlements("duchy_york").ToHashSet(),
            content.DeJureSettlements("county_york").ToHashSet());
    }

    /// <summary>
    /// The real content wired through the production adapters, exposing the three hierarchy use
    /// cases over one shared load.
    /// </summary>
    private sealed class Content
    {
        private readonly DadgXmlGenealogy _genealogy;
        private readonly InMemoryTitleRegistry _titles;
        private readonly XmlFeudalStructure _structure;
        private readonly GetSuzerainUseCase _suzerain;
        private readonly GetDirectVassalsUseCase _directVassals;
        private readonly GetDeJureSettlementsUseCase _deJureSettlements;

        private Content(DadgXmlGenealogy genealogy, XmlFeudalStructure structure)
        {
            _genealogy = genealogy;
            _structure = structure;
            _titles = new InMemoryTitleRegistry(genealogy);
            _titles.Initialise(structure.BuildInitialTitles(genealogy));
            _suzerain = new GetSuzerainUseCase(_titles, structure, genealogy);
            _directVassals = new GetDirectVassalsUseCase(_titles, structure, _suzerain, genealogy);
            _deJureSettlements = new GetDeJureSettlementsUseCase(_titles, structure);
        }

        public static Content Load()
        {
            using Stream titlesStream = DadgContent.OpenTitles();
            using Stream heroesStream = DadgContent.OpenHeroes();
            using Stream charactersStream = DadgContent.OpenCharacters();
            using Stream clansStream = DadgContent.OpenClans();

            return new Content(
                DadgXmlGenealogy.Load(heroesStream, charactersStream, clansStream),
                new XmlFeudalStructure(FeudalStructureParser.Parse(titlesStream)));
        }

        public IReadOnlyList<Title> AllTitles() => _titles.GetAllTitles();

        public string? Suzerain(string clanId) => _suzerain.Execute(clanId);

        public TitleRank? HighestRank(string clanId) => _suzerain.GetPrimaryTitle(clanId)?.Rank;

        public IReadOnlyList<string> DirectVassals(string clanId) => _directVassals.Execute(clanId);

        public IReadOnlyList<string> DeJureSettlements(string titleId) => _deJureSettlements.Execute(titleId);

        public string? SuzerainTitleOf(string titleId) => _structure.GetDeJureSuzerainTitleId(titleId);

        public string? ClanHolding(string titleId) => _genealogy.GetHolderClanOf(_titles.GetTitle(titleId));

        public IReadOnlyList<string> HoldingClans() => HoldingClansOf(_titles.GetAllTitles());

        /// <summary>Every house holding a title in the tree under a root title, the root included.</summary>
        public IReadOnlyList<string> HoldingClansUnder(string rootTitleId) =>
            HoldingClansOf(_titles.GetAllTitles().Where(title => RootOf(title.Id) == rootTitleId));

        /// <summary>The seats of every title in the tree under a root title, read off the titles themselves.</summary>
        public IOrderedEnumerable<string> SeatsUnder(string rootTitleId) =>
            _titles.GetAllTitles()
                .Where(title => RootOf(title.Id) == rootTitleId && !string.IsNullOrEmpty(title.SeatSettlementId))
                .Select(title => title.SeatSettlementId)
                .OrderBy(seat => seat);

        private string RootOf(string titleId)
        {
            string current = titleId;
            while (_structure.GetDeJureSuzerainTitleId(current) is { } parent) current = parent;
            return current;
        }

        private IReadOnlyList<string> HoldingClansOf(IEnumerable<Title> titles) =>
            titles
                .Select(title => _genealogy.GetHolderClanOf(title))
                .Where(clanId => clanId is not null)
                .Select(clanId => clanId!)
                .Distinct()
                .OrderBy(clanId => clanId)
                .ToList();
    }
}
