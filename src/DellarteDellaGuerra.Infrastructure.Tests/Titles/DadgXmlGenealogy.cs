using System.Xml.Linq;
using DellarteDellaGuerra.Domain.Titles.Model;
using DellarteDellaGuerra.Domain.Titles.Port;

namespace DellarteDellaGuerra.Infrastructure.Tests.Titles;

/// <summary>
/// Test adapter that presents the DADG content XML through the same domain port as the
/// live Bannerlord campaign. It deliberately contains no claim rules.
/// </summary>
internal sealed class DadgXmlGenealogy : IGenealogy
{
    private const string HeroPrefix = "Hero.";
    private const string FactionPrefix = "Faction.";

    private readonly IReadOnlyDictionary<string, HeroNode> _heroesById;
    private readonly IReadOnlyDictionary<string, string> _leaderIdByClanId;

    private DadgXmlGenealogy(
        IReadOnlyDictionary<string, HeroNode> heroesById,
        IReadOnlyDictionary<string, string> leaderIdByClanId)
    {
        _heroesById = heroesById;
        _leaderIdByClanId = leaderIdByClanId;
    }

    public int HeroCount => _heroesById.Count;

    public static DadgXmlGenealogy Load(string heroesPath, string charactersPath, string clansPath)
    {
        XDocument heroesDocument = XDocument.Load(heroesPath);
        XDocument charactersDocument = XDocument.Load(charactersPath);
        XDocument clansDocument = XDocument.Load(clansPath);

        Dictionary<string, bool> isFemaleByHeroId = charactersDocument
            .Descendants("NPCCharacter")
            .ToDictionary(
                element => RequiredAttribute(element, "id"),
                element => ParseBoolean(element.Attribute("is_female")?.Value, false));

        XElement[] heroElements = heroesDocument.Descendants("Hero").ToArray();
        var childrenByParentId = new Dictionary<string, List<string>>();
        foreach (XElement heroElement in heroElements)
        {
            string heroId = RequiredAttribute(heroElement, "id");
            AddChild(childrenByParentId, NormaliseReference(heroElement.Attribute("father")?.Value, HeroPrefix), heroId);
            AddChild(childrenByParentId, NormaliseReference(heroElement.Attribute("mother")?.Value, HeroPrefix), heroId);
        }

        var heroesById = new Dictionary<string, HeroNode>();
        foreach (XElement heroElement in heroElements)
        {
            string heroId = RequiredAttribute(heroElement, "id");
            if (!isFemaleByHeroId.TryGetValue(heroId, out bool isFemale))
            {
                throw new InvalidDataException($"DADG character data has no NPCCharacter for hero '{heroId}'.");
            }

            heroesById.Add(
                heroId,
                new HeroNode(
                    heroId,
                    isFemale,
                    ParseBoolean(heroElement.Attribute("alive")?.Value, true),
                    NormaliseReference(heroElement.Attribute("faction")?.Value, FactionPrefix),
                    childrenByParentId.TryGetValue(heroId, out List<string>? children)
                        ? children
                        : Array.Empty<string>()));
        }

        ValidateParentReferences(heroElements, heroesById);

        Dictionary<string, string> leaderIdByClanId = clansDocument
            .Descendants("Faction")
            .Where(element => element.Attribute("owner") is not null)
            .ToDictionary(
                element => RequiredAttribute(element, "id"),
                element => NormaliseReference(RequiredAttribute(element, "owner"), HeroPrefix)!);

        return new DadgXmlGenealogy(heroesById, leaderIdByClanId);
    }

    public HeroNode? GetHero(string heroId) =>
        _heroesById.TryGetValue(heroId, out HeroNode? hero) ? hero : null;

    public string? GetClanLeaderId(string clanId) =>
        _leaderIdByClanId.TryGetValue(clanId, out string? leaderId) ? leaderId : null;

    public IReadOnlyList<string> GetDeceasedClanMemberIds(string clanId) =>
        _heroesById.Values
            .Where(hero => !hero.IsAlive && hero.ClanId == clanId)
            .Select(hero => hero.Id)
            .ToList();

    private static void AddChild(
        IDictionary<string, List<string>> childrenByParentId,
        string? parentId,
        string childId)
    {
        if (parentId is null) return;

        if (!childrenByParentId.TryGetValue(parentId, out List<string>? children))
        {
            children = new List<string>();
            childrenByParentId[parentId] = children;
        }

        children.Add(childId);
    }

    private static void ValidateParentReferences(
        IEnumerable<XElement> heroElements,
        IReadOnlyDictionary<string, HeroNode> heroesById)
    {
        foreach (XElement heroElement in heroElements)
        {
            string heroId = RequiredAttribute(heroElement, "id");
            foreach (string attributeName in new[] { "father", "mother" })
            {
                string? parentId = NormaliseReference(heroElement.Attribute(attributeName)?.Value, HeroPrefix);
                if (parentId is not null && !heroesById.ContainsKey(parentId))
                {
                    throw new InvalidDataException(
                        $"DADG hero '{heroId}' refers to missing {attributeName} '{parentId}'.");
                }
            }
        }
    }

    private static string RequiredAttribute(XElement element, string attributeName) =>
        element.Attribute(attributeName)?.Value
        ?? throw new InvalidDataException(
            $"DADG element '{element.Name.LocalName}' is missing required attribute '{attributeName}'.");

    private static bool ParseBoolean(string? value, bool defaultValue) =>
        value is null ? defaultValue : bool.Parse(value);

    private static string? NormaliseReference(string? value, string prefix)
    {
        if (value is null) return null;
        return value.StartsWith(prefix, StringComparison.Ordinal) ? value[prefix.Length..] : value;
    }
}
