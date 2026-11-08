using System;
using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Titles.Api;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encyclopedia;
using TaleWorlds.CampaignSystem.Encyclopedia.Pages;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace DellarteDellaGuerra.Integration.Titles.UI;

// Encyclopedia identities are transient; all campaign state stays in the title repositories.
public sealed class TitleEncyclopediaEntry : Concept
{
    public const string Prefix = "dadg_title_";
    public const string Group = "DadgTitles";
    private const string LocalPrefix = "local:";
    public string TitleId { get; }
    public bool IsLocalTerritory { get; }
    public TitleEncyclopediaEntry(string id, bool localTerritory = false)
    {
        TitleId = id;
        IsLocalTerritory = localTerritory;
        StringId = Prefix + (localTerritory ? LocalPrefix : "") + id;
        Title = new TextObject((localTerritory ? FeudalServices.Structure?.GetTitleName(id)
            : FeudalServices.Titles?.GetTitle(id)?.Name) ?? FeudalServices.Structure?.GetTitleName(id) ?? id);
        Description = new TextObject("Lands, holders and claims.");
        FilterGroup = Group;
    }
    public override TextObject GetName() => Title;

    public static void Open(string id, bool localTerritory = false)
    {
        if (FeudalServices.Structure?.GetTitleName(id) is not null)
            Campaign.Current?.EncyclopediaManager.GoToLink("Concept", Prefix + (localTerritory ? LocalPrefix : "") + id);
    }

    public static TitleEncyclopediaEntry FromLink(string stringId)
    {
        string id = stringId.Substring(Prefix.Length);
        bool local = id.StartsWith(LocalPrefix, StringComparison.Ordinal);
        return new TitleEncyclopediaEntry(local ? id.Substring(LocalPrefix.Length) : id, local);
    }
}

[OverrideEncyclopediaModel(new[] { typeof(Concept) })]
public sealed class TitleEncyclopediaPage : DefaultEncyclopediaConceptPage
{
    private readonly Dictionary<string, TitleEncyclopediaEntry> _entries = new();
    public TitleEncyclopediaPage() => HomePageOrderIndex = 110;
    public override string GetViewFullyQualifiedName() => "DadgTitleEncyclopediaPage";
    public override string GetStringID() => "EncyclopediaSettlement";
    public override TextObject GetName() => new("{=dadg_title_index}Titles and concepts");
    public override TextObject GetDescriptionText() => new("{=dadg_title_index_description}The earldoms, lordships, estates and other territories of the realm.");
    public override MBObjectBase GetObject(string typeName, string stringID)
    {
        if (!stringID.StartsWith(TitleEncyclopediaEntry.Prefix, StringComparison.Ordinal)) return base.GetObject(typeName, stringID);
        if (!_entries.TryGetValue(stringID, out var entry))
            _entries.Add(stringID, entry = TitleEncyclopediaEntry.FromLink(stringID));
        return entry;
    }
    public override bool IsValidEncyclopediaItem(object o) => o is TitleEncyclopediaEntry entry
        ? FeudalServices.Structure?.GetTitleName(entry.TitleId) is not null : base.IsValidEncyclopediaItem(o);
    protected override IEnumerable<EncyclopediaListItem> InitializeListItems()
    {
        foreach (string id in FeudalServices.Structure?.GetAllTitleIds() ?? Array.Empty<string>())
            yield return new EncyclopediaListItem(GetObject("Concept", TitleEncyclopediaEntry.Prefix + id),
                FeudalServices.Titles?.GetTitle(id)?.Name ?? FeudalServices.Structure!.GetTitleName(id) ?? id,
                "", TitleEncyclopediaEntry.Prefix + id, "Concept", true);
        foreach (var item in base.InitializeListItems()) yield return item;
    }
    protected override IEnumerable<EncyclopediaFilterGroup> InitializeFilterItems()
    {
        yield return new EncyclopediaFilterGroup(new List<EncyclopediaFilterItem>
        {
            // The mod's own concepts about titles (dadg_concepts.xml) share the titles' group.
            new(new TextObject("{=dadg_title_filter}Titles"), o => o is Concept { FilterGroup: TitleEncyclopediaEntry.Group }),
            new(new TextObject("{=dadg_concept_filter}Concepts"), o => o is Concept { FilterGroup: not TitleEncyclopediaEntry.Group })
        }, new TextObject("{=dadg_entry_type}Entry type"));
    }
    protected override IEnumerable<EncyclopediaSortController> InitializeSortControllers() => Array.Empty<EncyclopediaSortController>();
}
