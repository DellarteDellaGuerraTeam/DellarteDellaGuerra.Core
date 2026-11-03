using System.Collections.Generic;
using System.Linq;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.ViewModels;
using DellarteDellaGuerra.Domain.Titles.Model;
using DellarteDellaGuerra.Domain.Titles.Port;
using DellarteDellaGuerra.Domain.PoliticalMap.Overlay;
using DellarteDellaGuerra.PoliticalMap.Api.Campaign;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ScreenSystem;

namespace DellarteDellaGuerra.Integration.Titles.UI.Mixins
{
    /// <summary>
    /// Adds titles, title allegiance, current political allegiance, territorial nesting and
    /// direct title vassals to the clan encyclopedia page, together with a
    /// button opening the realm-wide hierarchy screen. Hidden for clans outside the
    /// feudal structure. A new page VM is created on every encyclopedia navigation,
    /// so computing the values once here stays correct.
    /// </summary>
    [ViewModelMixin]
    public sealed class EncyclopediaClanPageMixin : BaseViewModelMixin<EncyclopediaClanPageVM>
    {
        public EncyclopediaClanPageMixin(EncyclopediaClanPageVM vm) : base(vm)
        {
            FeudalSectionText = new TextObject("{=dadg_feudal_section}Feudal").ToString();
            FeudalTitlesLabel = new TextObject("{=dadg_feudal_titles}Titles:").ToString();
            FeudalSuzerainLabel = new TextObject("{=dadg_title_liege}Title Liege:").ToString();
            PoliticalLiegeLabel = new TextObject("{=dadg_political_liege}Current Political Liege:").ToString();
            FeudalLiegeChainLabel = new TextObject("{=dadg_territorial_chain}Territorial Nesting:").ToString();
            FeudalVassalsLabel = new TextObject("{=dadg_title_vassals}Direct Title Vassals:").ToString();
            ViewHierarchyText = new TextObject("{=dadg_view_territorial_hierarchy}View Territorial Hierarchy").ToString();
            string none = new TextObject("{=dadg_feudal_none}None").ToString();
            FeudalTitlesText = none;
            FeudalSuzerainText = none;
            PoliticalLiegeText = none;
            FeudalLiegeChainText = none;
            FeudalVassalsText = none;

            if (vm.Obj is not Clan clan || !FeudalUiServices.IsInitialised) return;

            IReadOnlyList<Title> titles =
                FeudalUiServices.Titles?.GetTitlesByClan(clan.StringId) ?? new List<Title>();
            string? suzerainClanId = FeudalUiServices.Suzerainty?.GetSuzerain(clan.StringId);
            Clan? suzerainClan = suzerainClanId is null
                ? null
                : Campaign.Current?.CampaignObjectManager.Find<Clan>(suzerainClanId);
            IReadOnlyList<string> vassalClanIds =
                FeudalUiServices.GetDirectVassals?.Execute(clan.StringId) ?? new List<string>();

            IsFeudalInfoVisible = titles.Count > 0 || suzerainClan is not null || vassalClanIds.Count > 0;
            if (titles.Count > 0)
                FeudalTitlesText = string.Join(", ", titles.Select(FormatTitleName));
            if (suzerainClan is not null)
                FeudalSuzerainText = suzerainClan.Name.ToString();
            if (FeudalUiServices.Structure is { } structure && FeudalUiServices.Titles is { } repository
                && FeudalUiServices.Genealogy is { } genealogy)
            {
                var political = new PoliticalMapVassalHierarchy(structure, repository, genealogy, new BannerlordPoliticalMapWorld());
                if (political.LiegeOf(clan.StringId) is { } politicalLiege)
                    PoliticalLiegeText = ResolveClanName(politicalLiege);
            }
            string liegeChain = BuildLiegeChainText(FeudalUiServices.Suzerainty?.GetPrimaryTitle(clan.StringId), FeudalUiServices.Structure);
            if (liegeChain.Length > 0)
                FeudalLiegeChainText = liegeChain;
            if (vassalClanIds.Count > 0)
                FeudalVassalsText = string.Join(", ", vassalClanIds.Select(ResolveClanName));

        }

        [DataSourceProperty] public bool IsFeudalInfoVisible { get; }
        [DataSourceProperty] public string FeudalSectionText { get; }
        [DataSourceProperty] public string FeudalTitlesLabel { get; }
        [DataSourceProperty] public string FeudalTitlesText { get; }
        [DataSourceProperty] public string FeudalSuzerainLabel { get; }
        [DataSourceProperty] public string FeudalSuzerainText { get; }
        [DataSourceProperty] public string PoliticalLiegeLabel { get; }
        [DataSourceProperty] public string PoliticalLiegeText { get; }
        [DataSourceProperty] public string FeudalLiegeChainLabel { get; }
        [DataSourceProperty] public string FeudalLiegeChainText { get; }
        [DataSourceProperty] public string FeudalVassalsLabel { get; }
        [DataSourceProperty] public string FeudalVassalsText { get; }
        [DataSourceProperty] public string ViewHierarchyText { get; }

        [DataSourceMethod]
        public void ExecuteViewFeudalHierarchy()
        {
            if (ScreenManager.TopScreen is not FeudalHierarchyScreen)
                ScreenManager.PushScreen(new FeudalHierarchyScreen());
        }

        /// <summary>
        /// Walks the de jure hierarchy upwards from the clan's highest-ranked title,
        /// e.g. "Duchy of York > England". Empty when the clan holds no title, sits at
        /// the top of the hierarchy, or the structure is unavailable.
        /// </summary>
        private static string BuildLiegeChainText(Title? highest, IFeudalStructure? structure)
        {
            if (highest is null || structure is null) return string.Empty;
            var liegeNames = new List<string>();
            var seen = new HashSet<string> { highest.Id };
            string? liegeTitleId = structure.GetDeJureSuzerainTitleId(highest.Id);
            while (liegeTitleId is not null && seen.Add(liegeTitleId))
            {
                liegeNames.Add(structure.GetTitleName(liegeTitleId) ?? liegeTitleId);
                liegeTitleId = structure.GetDeJureSuzerainTitleId(liegeTitleId);
            }
            return string.Join(" > ", liegeNames);
        }

        private static string ResolveClanName(string clanId) =>
            Campaign.Current?.CampaignObjectManager.Find<Clan>(clanId)?.Name?.ToString() ?? clanId;

        private static string FormatTitleName(Title title) =>
            title.Name + FeudalTitleDisplay.OccupiedSuffix(title.IsContested ? title.OccupantClanId : null);
    }
}
