using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using DellarteDellaGuerra.Infrastructure.Utils;
using DellarteDellaGuerra.Domain.Titles;
using DellarteDellaGuerra.Domain.Titles.Model;
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
        NameText = structure?.GetTitleName(_id) ?? title?.Name ?? _id;
        InformationText = Articles.Value.TryGetValue(_id, out var article)
            && (string?)article.Attribute("name") == NameText
            ? (string?)article.Element("Description") ?? "" : NameText + " comprises the lands and rights of this territory.";
        SourcesText = article is null ? "" : string.Join("\n", article.Elements("Source").Select(x => x.Value));
        _parent = structure?.GetDeJureSuzerainTitleId(_id);
        ParentText = _parent is null ? "No superior title" : structure?.GetTitleName(_parent) ?? _parent;
        Holder = new HeroVM(FindHero(title?.HolderHeroId)!);
        HasHolder = Holder.Hero is not null;
        HolderText = title?.HolderHeroId is null ? "Holder not recorded" : "Recognised holder";

        if (titles is not null && structure is not null)
            foreach (var settlement in new GetDeJureSettlementsUseCase(titles, structure).Execute(_id)
                .Distinct().Select(Settlement.Find).Where(x => x is not null && (x.IsTown || x.IsCastle))
                .OrderBy(x => x.Name.ToString()))
            {
                Settlements.Add(new EncyclopediaSettlementVM(settlement));
                if (settlement.OwnerClan is { } clan && Occupiers.All(x => x.Faction != clan))
                    Occupiers.Add(new EncyclopediaFactionVM(clan));
            }
        SettlementsText = "Towns and castles (" + Settlements.Count + ")";
        foreach (Claim claim in FeudalServices.Claims?.GetClaimsOn(_id) ?? Array.Empty<Claim>())
        {
            Clan? house = FindClan(claim.ClaimantClanId);
            Hero? hero = claim.ClaimantHeroId is { } heroId ? FindHero(heroId) : house?.Leader;
            Claimants.Add(new TitleClaimantVM(hero, claim, house));
        }
        ClaimsText = Claimants.Count == 0 ? "No recorded claims" : "Claimants (" + Claimants.Count + ")";
        if (title is not null)
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
        var seat = string.IsNullOrEmpty(title?.SeatSettlementId) ? null : Settlement.Find(title!.SeatSettlementId);
        SettlementImageID = seat?.SettlementComponent?.WaitMeshName ?? "";
        HasSeatImage = SettlementImageID.Length > 0;
        SeatText = seat is null ? "No single seat" : "Seat: " + seat.Name;
        OccupancyText = title?.OccupantClanId is { } occupant
            ? "The seat is occupied by " + (FindClan(occupant)?.Name.ToString() ?? occupant) + ". The recognised holder retains the title."
            : "Possession of the towns and castles is shown separately from the recognised titleholder.";
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
    public void ExecuteParentLink() { if (_parent is not null) TitleEncyclopediaEntry.Open(_parent); }
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
    [DataSourceProperty] public string OccupiersText => "Territorial possession";
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
