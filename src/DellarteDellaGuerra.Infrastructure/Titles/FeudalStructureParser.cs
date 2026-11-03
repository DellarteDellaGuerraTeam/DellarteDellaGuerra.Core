using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml.Linq;
using DellarteDellaGuerra.Domain.Titles.Model;
using DellarteDellaGuerra.Infrastructure.Titles.Model;

namespace DellarteDellaGuerra.Infrastructure.Titles;

/**
 * <summary>
 * Parses the feudal titles configuration XML into a forest of <see cref="FeudalTitleNode"/>.
 * <br/>
 * <br/>
 * Expected schema:
 * <code>
 * &lt;Feudalism&gt;
 *   &lt;Kingdom id="..." name="..." seat="..." kingClanId="..." heldSince="..."&gt;
 *     &lt;Duchy id="..." name="..." seat="..." holderClanId="..." heldSince="..."&gt;
 *       &lt;County id="..." name="..." seat="..." holderClanId="..." heldSince="..."&gt;
 *         &lt;Claim clanId="..." strength="..."/&gt;
 *         &lt;Barony id="..." name="..." seat="..." holderClanId="..." heldSince="..."/&gt;
 *       &lt;/County&gt;
 *     &lt;/Duchy&gt;
 *   &lt;/Kingdom&gt;
 * &lt;/Feudalism&gt;
 * </code>
 * heldSince is the optional year the holder's house took the title. A Claim element gives a
 * clan a claim on its parent title from the start of the campaign; strength is Weak, Strong
 * or DeJure, Strong when absent.
 * Optional dignityRank and dignityName describe the held dignity independently of the element's
 * territorial tier and name. Optional allegiance names a title in the same realm instead of the
 * territorial parent; the resulting allegiance chains must be acyclic.
 * </summary>
 */
public static class FeudalStructureParser
{
    private const string IdAttribute = "id";
    private const string NameAttribute = "name";
    private const string SeatAttribute = "seat";
    private const string HolderClanIdAttribute = "holderClanId";
    private const string KingClanIdAttribute = "kingClanId";
    private const string HeldSinceAttribute = "heldSince";
    private const string ClaimElement = "Claim";
    private const string ClanIdAttribute = "clanId";
    private const string StrengthAttribute = "strength";

    internal static IReadOnlyList<FeudalTitleNode> Parse(string xml)
    {
        return ParseDocument(XDocument.Parse(xml));
    }

    internal static IReadOnlyList<FeudalTitleNode> Parse(Stream stream)
    {
        return ParseDocument(XDocument.Load(stream));
    }

    private static IReadOnlyList<FeudalTitleNode> ParseDocument(XDocument document)
    {
        XElement? root = document.Root;
        if (root is null)
        {
            throw new InvalidOperationException("The feudal titles configuration has no root element");
        }

        var seenTitleIds = new HashSet<string>();
        var seenSeats = new HashSet<string>();
        var forest = new List<FeudalTitleNode>();
        foreach (XElement element in root.Elements())
        {
            forest.Add(ParseTitleElement(element, seenTitleIds, seenSeats));
        }

        ValidateAllegiances(forest);
        return forest;
    }

    private static void ValidateAllegiances(IReadOnlyList<FeudalTitleNode> forest)
    {
        var parents = new Dictionary<string, string?>();
        var realms = new Dictionary<string, string>();
        var nodes = new Dictionary<string, FeudalTitleNode>();
        void Index(FeudalTitleNode node, string? parent, string realm)
        {
            nodes.Add(node.TitleId, node);
            parents.Add(node.TitleId, parent);
            realms.Add(node.TitleId, realm);
            foreach (FeudalTitleNode child in node.Children) Index(child, node.TitleId, realm);
        }
        foreach (FeudalTitleNode root in forest) Index(root, null, root.TitleId);

        foreach (FeudalTitleNode node in nodes.Values)
        {
            if (node.AllegianceTitleId is not { } allegiance) continue;
            if (!nodes.ContainsKey(allegiance) || realms[allegiance] != realms[node.TitleId]
                || parents[node.TitleId] is null)
                throw new InvalidOperationException($"Invalid allegiance '{allegiance}' on title '{node.TitleId}': expected a title in the same realm on a non-root title");
            parents[node.TitleId] = allegiance;
        }
        foreach (string titleId in nodes.Keys)
        {
            var seen = new HashSet<string>();
            for (string? current = titleId; current is not null; current = parents[current])
                if (!seen.Add(current))
                    throw new InvalidOperationException($"Cyclic allegiance at title '{current}' in the feudal titles configuration");
        }
    }

    private static FeudalTitleNode ParseTitleElement(
        XElement element,
        ISet<string> seenTitleIds,
        ISet<string> seenSeats)
    {
        TitleRank rank = ParseRank(element);
        string titleId = GetRequiredAttribute(element, IdAttribute);
        if (!seenTitleIds.Add(titleId))
        {
            throw new InvalidOperationException($"Duplicate title id '{titleId}' in the feudal titles configuration");
        }

        string name = element.Attribute(NameAttribute)?.Value ?? titleId;
        string seatSettlementId = element.Attribute(SeatAttribute)?.Value ?? string.Empty;
        if (seatSettlementId.Length > 0 && !seenSeats.Add(seatSettlementId))
        {
            throw new InvalidOperationException(
                $"Duplicate seat '{seatSettlementId}' in the feudal titles configuration (title '{titleId}')");
        }

        string holderAttribute = rank == TitleRank.King ? KingClanIdAttribute : HolderClanIdAttribute;
        string? initialHolderClanId = element.Attribute(holderAttribute)?.Value;
        int? initialHeldSinceYear = ParseHeldSinceYear(element, titleId);
        TitleRank? dignityRank = null;
        if (element.Attribute("dignityRank") is { } dignity)
        {
            if (rank == TitleRank.King || (dignity.Value != "Baron" && dignity.Value != "Earl" && dignity.Value != "Duke"))
                throw new InvalidOperationException($"Invalid dignityRank '{dignity.Value}' on title '{titleId}': expected Baron, Earl or Duke on a non-kingdom title");
            dignityRank = (TitleRank) Enum.Parse(typeof(TitleRank), dignity.Value);
        }
        string? dignityName = element.Attribute("dignityName") is null ? null : GetRequiredAttribute(element, "dignityName");
        string? allegiance = element.Attribute("allegiance") is null ? null : GetRequiredAttribute(element, "allegiance");

        var children = new List<FeudalTitleNode>();
        var claims = new List<FeudalClaimNode>();
        foreach (XElement child in element.Elements())
        {
            if (child.Name.LocalName == ClaimElement)
            {
                claims.Add(ParseClaimElement(child, titleId, initialHolderClanId));
                continue;
            }

            children.Add(ParseTitleElement(child, seenTitleIds, seenSeats));
        }

        return new FeudalTitleNode(
            titleId, name, rank, seatSettlementId, initialHolderClanId, children, initialHeldSinceYear, claims,
            dignityRank, dignityName, allegiance);
    }

    private static FeudalClaimNode ParseClaimElement(XElement element, string titleId, string? holderClanId)
    {
        string clanId = GetRequiredAttribute(element, ClanIdAttribute);
        if (clanId == holderClanId)
        {
            throw new InvalidOperationException(
                $"Clan '{clanId}' claims title '{titleId}', which it already holds, in the feudal titles configuration");
        }

        string? value = element.Attribute(StrengthAttribute)?.Value;
        ClaimStrength strength = ClaimStrength.Strong;
        if (value is not null
            && (!Enum.TryParse(value, false, out strength) || !Enum.IsDefined(typeof(ClaimStrength), strength)))
        {
            throw new InvalidOperationException(
                $"Invalid claim '{StrengthAttribute}' '{value}' on title '{titleId}' in the feudal titles configuration. " +
                "Expected one of: Weak, Strong, DeJure");
        }

        return new FeudalClaimNode(clanId, strength);
    }

    private static int? ParseHeldSinceYear(XElement element, string titleId)
    {
        string? value = element.Attribute(HeldSinceAttribute)?.Value;
        if (value is null) return null;
        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int year))
        {
            throw new InvalidOperationException(
                $"Invalid '{HeldSinceAttribute}' year '{value}' on title '{titleId}' in the feudal titles configuration");
        }

        return year;
    }

    private static TitleRank ParseRank(XElement element)
    {
        switch (element.Name.LocalName)
        {
            case "Kingdom":
                return TitleRank.King;
            case "Duchy":
                return TitleRank.Duke;
            case "County":
                return TitleRank.Earl;
            case "Barony":
                return TitleRank.Baron;
            default:
                throw new InvalidOperationException(
                    $"Unknown title element '{element.Name.LocalName}' in the feudal titles configuration. " +
                    "Expected one of: Kingdom, Duchy, County, Barony");
        }
    }

    private static string GetRequiredAttribute(XElement element, string attributeName)
    {
        string? value = element.Attribute(attributeName)?.Value;
        if (string.IsNullOrEmpty(value))
        {
            throw new InvalidOperationException(
                $"Missing required attribute '{attributeName}' on element '{element.Name.LocalName}' " +
                "in the feudal titles configuration");
        }

        return value!;
    }
}
