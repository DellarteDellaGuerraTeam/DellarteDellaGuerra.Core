using System;
using System.Collections.Generic;
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
 *   &lt;Kingdom id="..." name="..." seat="..." kingClanId="..."&gt;
 *     &lt;Duchy id="..." name="..." seat="..." holderClanId="..."&gt;
 *       &lt;County id="..." name="..." seat="..." holderClanId="..."&gt;
 *         &lt;Barony id="..." name="..." seat="..." holderClanId="..."/&gt;
 *       &lt;/County&gt;
 *     &lt;/Duchy&gt;
 *   &lt;/Kingdom&gt;
 * &lt;/Feudalism&gt;
 * </code>
 * </summary>
 */
public static class FeudalStructureParser
{
    private const string IdAttribute = "id";
    private const string NameAttribute = "name";
    private const string SeatAttribute = "seat";
    private const string HolderClanIdAttribute = "holderClanId";
    private const string KingClanIdAttribute = "kingClanId";

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

        return forest;
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

        var children = new List<FeudalTitleNode>();
        foreach (XElement child in element.Elements())
        {
            children.Add(ParseTitleElement(child, seenTitleIds, seenSeats));
        }

        return new FeudalTitleNode(titleId, name, rank, seatSettlementId, initialHolderClanId, children);
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
                return TitleRank.Count;
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
