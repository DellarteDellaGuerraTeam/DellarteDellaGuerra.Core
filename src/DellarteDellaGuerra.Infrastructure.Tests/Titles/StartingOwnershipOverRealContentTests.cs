using System.Xml.Linq;
using DellarteDellaGuerra.Domain.Titles.Model;
using DellarteDellaGuerra.Infrastructure.Titles;

namespace DellarteDellaGuerra.Infrastructure.Tests.Titles;

public class StartingOwnershipOverRealContentTests
{
    [Fact]
    [Trait("Category", "DADG content integration")]
    public void EveryStartingTitleSeatHasValidOwnershipWithOnlyTheReviewedException()
    {
        var content = new MapModuleContentProvider();
        string modulesRoot = Directory.GetParent(content.MapDataRoot)!.Parent!.FullName;
        string titlesPath = Path.Combine(modulesRoot, "DellarteDellaGuerra.Core", "config", "titles.config.xml");
        string settlementsPath = Path.Combine(content.MapDataRoot, "settlements.xml");

        // Read both live files: XML edits must be checked even when running with --no-build.
        using Stream titlesStream = File.OpenRead(titlesPath);
        var structure = new XmlFeudalStructure(FeudalStructureParser.Parse(titlesStream));
        List<Title> titles = structure.BuildInitialTitles(new IdentityGenealogy())
            .Where(title => !string.IsNullOrEmpty(title.SeatSettlementId)).OrderBy(title => title.Id).ToList();
        Assert.NotEmpty(titles);

        Dictionary<string, XElement> settlements = XDocument.Load(settlementsPath)
            .Descendants("Settlement").ToDictionary(node => (string)node.Attribute("id")!, StringComparer.Ordinal);
        using Stream clansStream = content.OpenClans();
        HashSet<string> clans = XDocument.Load(clansStream).Descendants("Faction")
            .Select(node => (string)node.Attribute("id")!).ToHashSet(StringComparer.Ordinal);
        Assert.NotEmpty(clans);

        // St Andrews remains a Lindsay gameplay holding while its Crown title stands in for
        // the unrepresented bishopric. This permits only this exact pair, not arbitrary drift.
        var exceptions = new Dictionary<string, (string Holder, string Owner)>(StringComparer.Ordinal)
        {
            ["dadg_sco_st_andrews_town"] = ("clan_stewart", "clan_lindsay")
        };
        var observedExceptions = new HashSet<string>(StringComparer.Ordinal);
        var issues = new List<string>();
        foreach (Title title in titles)
        {
            string context = $"{title.Id} ({title.SeatSettlementId})";
            string? holder = title.HolderHeroId; // IdentityGenealogy maps each configured clan to itself.
            bool validHolder = holder is not null && clans.Contains(holder);
            if (!validHolder)
                issues.Add($"{context}: unknown title-holder clan '{holder ?? "<none>"}'.");

            if (!settlements.TryGetValue(title.SeatSettlementId, out XElement? settlement))
            {
                issues.Add($"{context}: title seat is missing from settlements.xml.");
                continue;
            }

            string? reference = (string?)settlement.Attribute("owner");
            string? owner = reference?.StartsWith("Faction.", StringComparison.Ordinal) == true
                ? reference.Substring("Faction.".Length) : null;
            if (owner is null || !clans.Contains(owner))
                issues.Add($"{context}: invalid settlement-owner reference '{reference ?? "<none>"}'.");
            else if (validHolder && owner != holder
                && exceptions.TryGetValue(title.SeatSettlementId, out var exception)
                && holder == exception.Holder && owner == exception.Owner)
                observedExceptions.Add(title.SeatSettlementId);
            else if (owner != holder)
                issues.Add($"{context}: title holder = {holder ?? "<none>"}; settlement owner = {owner}.");
        }

        foreach (string seat in exceptions.Keys.Where(seat => !observedExceptions.Contains(seat)))
            issues.Add($"{seat}: reviewed ownership exception no longer matches; remove or review the stale exception.");

        // Claims do not prove occupation; intentional differences need individual historical review.
        Assert.True(issues.Count == 0,
            $"Starting ownership: {issues.Count} issues across {titles.Count} title seats.{Environment.NewLine}"
            + $"Titles: {titlesPath}{Environment.NewLine}Settlements: {settlementsPath}{Environment.NewLine}"
            + string.Join(Environment.NewLine, issues));
    }
}
