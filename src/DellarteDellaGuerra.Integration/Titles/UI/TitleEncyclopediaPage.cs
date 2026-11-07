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
    public string TitleId { get; }
    public TitleEncyclopediaEntry(string id)
    {
        TitleId = id;
        StringId = Prefix + id;
        Title = new TextObject(FeudalServices.Structure?.GetTitleName(id) ?? id);
        Description = new TextObject("Lands, holders and claims.");
        FilterGroup = "DadgTitles";
    }
    public override TextObject GetName() => Title;

    public static void Open(string id)
    {
        if (FeudalServices.Structure?.GetTitleName(id) is not null)
            Campaign.Current?.EncyclopediaManager.GoToLink("Concept", Prefix + id);
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
            _entries.Add(stringID, entry = new TitleEncyclopediaEntry(stringID.Substring(TitleEncyclopediaEntry.Prefix.Length)));
        return entry;
    }
    public override bool IsValidEncyclopediaItem(object o) => o is TitleEncyclopediaEntry entry
        ? FeudalServices.Structure?.GetTitleName(entry.TitleId) is not null : base.IsValidEncyclopediaItem(o);
    protected override IEnumerable<EncyclopediaListItem> InitializeListItems()
    {
        foreach (string id in FeudalServices.Structure?.GetAllTitleIds() ?? Array.Empty<string>())
            yield return new EncyclopediaListItem(GetObject("Concept", TitleEncyclopediaEntry.Prefix + id),
                FeudalServices.Structure!.GetTitleName(id) ?? id, "", TitleEncyclopediaEntry.Prefix + id, "Concept", true);
        foreach (var item in base.InitializeListItems()) yield return item;
    }
    protected override IEnumerable<EncyclopediaFilterGroup> InitializeFilterItems()
    {
        yield return new EncyclopediaFilterGroup(new List<EncyclopediaFilterItem>
        {
            new(new TextObject("{=dadg_title_filter}Titles"), o => o is TitleEncyclopediaEntry),
            new(new TextObject("{=dadg_concept_filter}Concepts"), o => o is not TitleEncyclopediaEntry)
        }, new TextObject("{=dadg_entry_type}Entry type"));
    }
    protected override IEnumerable<EncyclopediaSortController> InitializeSortControllers() => Array.Empty<EncyclopediaSortController>();
}
