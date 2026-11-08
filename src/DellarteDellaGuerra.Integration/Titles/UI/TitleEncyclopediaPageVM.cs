using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using DellarteDellaGuerra.Infrastructure.Utils;
using DellarteDellaGuerra.Domain.Titles;
using DellarteDellaGuerra.Domain.Titles.Model;
using DellarteDellaGuerra.Domain.PoliticalMap.Overlay;
using DellarteDellaGuerra.Infrastructure.Titles;
using DellarteDellaGuerra.PoliticalMap.Api.Campaign;
using DellarteDellaGuerra.Titles.Api;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Items;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages;
using TaleWorlds.Library;

namespace DellarteDellaGuerra.Integration.Titles.UI;

[EncyclopediaViewModel(typeof(Concept))]
public sealed class TitleEncyclopediaPageVM : EncyclopediaContentPageVM
{
    private readonly string _id = "";
    private readonly string? _parent;
    private static readonly Lazy<Dictionary<string, XElement>> Articles = new(() =>
    {
        string? path = ResourceLocator.GetConfigurationFilePath("TitleArticles.xml");
        return path is null ? new() : XDocument.Load(path).Root!.Elements("Title")
            .ToDictionary(x => (string)x.Attribute("id")!);
    });

    public TitleEncyclopediaPageVM(EncyclopediaPageArgs args) : base(args)
    {
        if (args.Obj is not TitleEncyclopediaEntry entry)
        {
            var concept = (Concept)args.Obj;
            NameText = concept.Title.ToString();
            InformationText = concept.Description.ToString();
            IsBookmarked = Campaign.Current.EncyclopediaManager.ViewDataTracker.IsEncyclopediaBookmarked(concept);
            UpdateBookmarkHintText();
            IsLoadingOver = true;
            return;
        }
        IsTitle = true;
        _id = entry.TitleId;
        var structure = FeudalServices.Structure;
        var titles = FeudalServices.Titles;
        Title? title = titles?.GetTitle(_id);
        NameText = entry.GetName().ToString();
        Articles.Value.TryGetValue(_id, out var article);
        if ((string?)article?.Attribute("name") != NameText)
            article = Articles.Value.Values.FirstOrDefault(x => (string?)x.Attribute("name") == NameText);
        InformationText = (string?)article?.Element("Description")
            ?? NameText + " comprises the lands and rights of this title.";
        SourcesText = article is null ? "" : string.Join("\n", article.Elements("Source").Select(x => x.Value));
        _parent = structure?.GetDeJureSuzerainTitleId(_id);
        ParentText = _parent is null ? "No superior title" : "Jurisdiction: " + (structure?.GetTitleName(_parent) ?? _parent);
        var seat = string.IsNullOrEmpty(title?.SeatSettlementId) ? null : Settlement.Find(title!.SeatSettlementId);
        Holder = new HeroVM((entry.IsLocalTerritory ? seat?.OwnerClan?.Leader : FindHero(title?.HolderHeroId))!);
        HasHolder = Holder.Hero is not null;
        HolderText = entry.IsLocalTerritory ? "Lord of the settlement"
            : title?.HolderHeroId is null ? "Holder not recorded" : "Recognised holder";

        if (titles is not null && structure is not null)
        {
            var world = new BannerlordPoliticalMapWorld();
            var hierarchy = new PoliticalMapVassalHierarchy(structure, titles, new CampaignGenealogy(), world);
            var domains = new PoliticalMapTitleDomains(structure, world, hierarchy);
            foreach (var settlement in Settlement.All.Where(x => (x.IsTown || x.IsCastle)
                && (entry.IsLocalTerritory ? domains.ContainsLocal(_id, x.StringId) : domains.Contains(_id, x.StringId)))
                .OrderBy(x => x.Name.ToString()))
            {
                Settlements.Add(new EncyclopediaSettlementVM(settlement));
                if (settlement.OwnerClan is { } clan && Occupiers.All(x => x.Faction != clan))
                    Occupiers.Add(new EncyclopediaFactionVM(clan));
            }
        }
        SettlementsText = "Actual domain — towns and castles (" + Settlements.Count + ")";
        bool showsDignity = !entry.IsLocalTerritory || NameText == title?.Name;
        foreach (Claim claim in showsDignity ? FeudalServices.Claims?.GetClaimsOn(_id) ?? Array.Empty<Claim>() : Array.Empty<Claim>())
        {
            Clan? house = FindClan(claim.ClaimantClanId);
            Hero? hero = claim.ClaimantHeroId is { } heroId ? FindHero(heroId) : house?.Leader;
            Claimants.Add(new TitleClaimantVM(hero, claim, house));
        }
        ClaimsText = Claimants.Count == 0 ? "No recorded claims" : "Claimants (" + Claimants.Count + ")";
        if (showsDignity && title is not null)
            foreach (TitleHolder holder in title.Holders.Reverse())
            {
                if (holder.HeroId is null && holder.Acquisition == TitleAcquisition.Initial) continue;
                string name = FindHero(holder.HeroId)?.Name.ToString() ?? (holder.HeroId is null ? "Vacant" : "Unknown holder");
                // Initial dates record house tenure, not this person's accession.
                string date = holder.Acquisition == TitleAcquisition.Initial
                    ? "House tenure recorded from " + (int)(holder.SinceDay / 84)
                    : TaleWorlds.CampaignSystem.CampaignTime.Days(holder.SinceDay).ToString();
                History.Add(new TitleHistoryVM(name + " — " + AcquisitionText(holder.Acquisition) + ". " + date));
            }
        SettlementImageID = seat?.SettlementComponent?.WaitMeshName ?? "";
        HasSeatImage = SettlementImageID.Length > 0;
        SeatText = seat is null ? "No single seat" : "Seat: " + seat.Name;
        OccupancyText = entry.IsLocalTerritory ? "The towns and castles of this territory are listed with their present owners."
            : title?.OccupantClanId is { } occupant
            ? "The seat is occupied by " + (FindClan(occupant)?.Name.ToString() ?? occupant) + ". The recognised holder retains the title."
            : "The actual domain includes controlled holdings and attached vassals.";
        IsLoadingOver = true;
    }

    private static string AcquisitionText(TitleAcquisition acquisition) => acquisition switch
    {
        TitleAcquisition.Initial => "holder at the beginning of the campaign",
        TitleAcquisition.Inherited => "inherited",
        TitleAcquisition.Granted => "granted",
        TitleAcquisition.Conquered => "acquired by conquest",
        _ => "awarded"
    };
    private static Hero? FindHero(string? id) => id is null ? null : Campaign.Current?.CampaignObjectManager.Find<Hero>(id);
    private static Clan? FindClan(string id) => Campaign.Current?.CampaignObjectManager.Find<Clan>(id);
    public void ExecuteParentLink() { if (_parent is not null) TitleEncyclopediaEntry.Open(_parent, true); }
    public void ExecuteLink(string link) => Campaign.Current?.EncyclopediaManager.GoToLink(link);
    public override void ExecuteSwitchBookmarkedState()
    {
        if (IsTitle) return;
        base.ExecuteSwitchBookmarkedState();
        if (IsBookmarked) Campaign.Current.EncyclopediaManager.ViewDataTracker.AddEncyclopediaBookmarkToItem((Concept)Obj);
        else Campaign.Current.EncyclopediaManager.ViewDataTracker.RemoveEncyclopediaBookmarkFromItem((Concept)Obj);
    }
    public override string GetName() => NameText;
    public override string GetNavigationBarURL() => TaleWorlds.Core.HyperlinkTexts.GetGenericHyperlinkText("Home", "Encyclopedia")
        + " \\ " + TaleWorlds.Core.HyperlinkTexts.GetGenericHyperlinkText("ListPage-Concept", "Titles and concepts") + " \\ " + NameText;

    [DataSourceProperty] public string NameText { get; }
    [DataSourceProperty] public bool IsTitle { get; }
    [DataSourceProperty] public bool IsConcept => !IsTitle;
    [DataSourceProperty] public string TitleText => NameText;
    [DataSourceProperty] public string DescriptionText => InformationText;
    [DataSourceProperty] public string InformationText { get; }
    [DataSourceProperty] public string ParentText { get; } = "";
    [DataSourceProperty] public string SeatText { get; } = "";
    [DataSourceProperty] public string SettlementImageID { get; } = "";
    [DataSourceProperty] public bool HasSeatImage { get; }
    [DataSourceProperty] public HeroVM? Holder { get; }
    [DataSourceProperty] public bool HasHolder { get; }
    [DataSourceProperty] public string OccupancyText { get; } = "";
    [DataSourceProperty] public string HolderText { get; } = "";
    [DataSourceProperty] public string SourcesText { get; } = "";
    [DataSourceProperty] public string SettlementsText { get; } = "";
    [DataSourceProperty] public string ClaimsText { get; } = "";
    [DataSourceProperty] public string OccupiersText => "Owners within the actual domain";
    [DataSourceProperty] public string HistoryText => "Succession and transfers";
    [DataSourceProperty] public string SourcesHeading => "Historical references";
    [DataSourceProperty] public MBBindingList<EncyclopediaSettlementVM> Settlements { get; } = new();
    [DataSourceProperty] public MBBindingList<EncyclopediaFactionVM> Occupiers { get; } = new();
    [DataSourceProperty] public MBBindingList<TitleClaimantVM> Claimants { get; } = new();
    [DataSourceProperty] public MBBindingList<TitleHistoryVM> History { get; } = new();
}

public sealed class TitleClaimantVM : HeroVM
{
    public TitleClaimantVM(Hero? hero, Claim claim, Clan? house) : base(hero!)
    {
        ClaimText = claim.Strength + " — " + claim.Origin + (claim.ClaimantHeroId is null ? "\nHouse claim: " + (house?.Name.ToString() ?? claim.ClaimantClanId) : "\nPersonal claim");
    }
    [DataSourceProperty] public string ClaimText { get; }
}

public sealed class TitleHistoryVM(string text) : ViewModel
{
    [DataSourceProperty] public string Text { get; } = text;
}
