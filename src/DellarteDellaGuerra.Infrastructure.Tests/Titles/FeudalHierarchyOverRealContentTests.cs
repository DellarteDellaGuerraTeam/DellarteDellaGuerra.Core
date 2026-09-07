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

    [Fact]
    [Trait("Category", "DADG content integration")]
    public void EveryHouseInTheRealmAnswersToTheCrownThroughAChainThatTerminates()
    {
        // The chain is walked at runtime to resolve war sides, so a loop in it would hang the
        // daily tick rather than merely mis-answer. Fifty-three houses hold land; every one of
        // them arrives at Lancaster in at most three steps, and only Lancaster answers to nobody.
        Content content = Content.Load();
        IReadOnlyList<string> holdingClans = content.HoldingClans();

        Assert.Equal(53, holdingClans.Count);
        Assert.Equal(new[] { Crown }, holdingClans.Where(clan => content.Suzerain(clan) is null));

        var depths = new Dictionary<string, int>();
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

            Assert.Equal(Crown, walked[^1]);
            depths[clanId] = walked.Count - 1;
        }

        Assert.Equal(0, depths[Crown]);
        Assert.Equal(3, depths.Values.Max());
    }

    [Fact]
    [Trait("Category", "DADG content integration")]
    public void TheSuzerainAndDirectVassalViewsPartitionTheRealmTheSameWay()
    {
        // Two use cases answer the same question from opposite ends, and the campaign layer uses
        // both. They agree here on all fifty-three houses: each non-royal house is named by
        // exactly one liege, and the crown is named by none, which is the same statement as
        // "the chains form one tree rooted at Lancaster" arrived at the other way round.
        Content content = Content.Load();
        IReadOnlyList<string> holdingClans = content.HoldingClans();

        var vassalsByLiege = holdingClans.ToDictionary(clan => clan, content.DirectVassals);

        Assert.Equal(17, vassalsByLiege[Crown].Count);
        Assert.Equal(16, vassalsByLiege.Count(entry => entry.Value.Count > 0));

        foreach (string clanId in holdingClans)
        {
            string[] liegesNamingIt = vassalsByLiege
                .Where(entry => entry.Value.Contains(clanId))
                .Select(entry => entry.Key)
                .ToArray();

            if (clanId == Crown) Assert.Empty(liegesNamingIt);
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
        // dignity gathers the realm — all ninety-six seats, one per title save the kingdom
        // itself, which is the only title in the content authored without a seat of its own.
        Content content = Content.Load();

        Assert.Equal(97, content.AllTitles().Count);
        Assert.Equal(
            new[] { "kingdom_england" },
            content.AllTitles()
                .Where(title => string.IsNullOrEmpty(title.SeatSettlementId))
                .Select(title => title.Id));

        IReadOnlyList<string> realm = content.DeJureSettlements("kingdom_england");
        Assert.Equal(96, realm.Count);
        Assert.Equal(96, realm.Distinct().Count());
        Assert.Equal(
            content.AllTitles()
                .Select(title => title.SeatSettlementId)
                .Where(seat => !string.IsNullOrEmpty(seat))
                .OrderBy(seat => seat),
            realm.OrderBy(seat => seat));

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

        public TitleRank? HighestRank(string clanId) => _suzerain.GetHighestRank(clanId);

        public IReadOnlyList<string> DirectVassals(string clanId) => _directVassals.Execute(clanId);

        public IReadOnlyList<string> DeJureSettlements(string titleId) => _deJureSettlements.Execute(titleId);

        public string? SuzerainTitleOf(string titleId) => _structure.GetDeJureSuzerainTitleId(titleId);

        public string? ClanHolding(string titleId) => _genealogy.GetHolderClanOf(_titles.GetTitle(titleId));

        public IReadOnlyList<string> HoldingClans() =>
            _titles.GetAllTitles()
                .Select(title => _genealogy.GetHolderClanOf(title))
                .Where(clanId => clanId is not null)
                .Select(clanId => clanId!)
                .Distinct()
                .OrderBy(clanId => clanId)
                .ToList();
    }
}
