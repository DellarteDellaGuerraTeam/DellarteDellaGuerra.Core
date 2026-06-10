using System.Collections.Generic;
using System.Linq;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.ViewModels;
using DellarteDellaGuerra.Domain.Levy.Model;
using DellarteDellaGuerra.Domain.Titles.Model;
using DellarteDellaGuerra.Domain.Titles.Port;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ScreenSystem;

namespace DellarteDellaGuerra.Integration.Titles.UI.Mixins
{
    /// <summary>
    /// Adds the full feudal summary (titles, suzerain, de jure liege chain, direct
    /// vassals and active levies) to the clan encyclopedia page, together with a
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
            FeudalSuzerainLabel = new TextObject("{=dadg_feudal_suzerain}Suzerain:").ToString();
            FeudalLiegeChainLabel = new TextObject("{=dadg_feudal_liege_chain}De Jure Lieges:").ToString();
            FeudalVassalsLabel = new TextObject("{=dadg_feudal_vassals}Direct Vassals:").ToString();
            FeudalLeviesLabel = new TextObject("{=dadg_feudal_levies}Levies:").ToString();
            ViewHierarchyText = new TextObject("{=dadg_feudal_view_hierarchy}View Feudal Hierarchy").ToString();
            string none = new TextObject("{=dadg_feudal_none}None").ToString();
            FeudalTitlesText = none;
            FeudalSuzerainText = none;
            FeudalLiegeChainText = none;
            FeudalVassalsText = none;
            FeudalLeviesText = none;

            if (vm.Obj is not Clan clan || !FeudalUiServices.IsInitialised) return;

            IReadOnlyList<Title> titles =
                FeudalUiServices.Titles?.GetTitlesByClan(clan.StringId) ?? new List<Title>();
            string? suzerainClanId = FeudalUiServices.GetSuzerain?.Execute(clan.StringId);
            Clan? suzerainClan = suzerainClanId is null
                ? null
                : Campaign.Current?.CampaignObjectManager.Find<Clan>(suzerainClanId);
            IReadOnlyList<string> vassalClanIds =
                FeudalUiServices.GetDirectVassals?.Execute(clan.StringId) ?? new List<string>();

            IsFeudalInfoVisible = titles.Count > 0 || suzerainClan is not null || vassalClanIds.Count > 0;
            if (titles.Count > 0)
                FeudalTitlesText = string.Join(", ", titles.Select(title => title.Name));
            if (suzerainClan is not null)
                FeudalSuzerainText = suzerainClan.Name.ToString();
            string liegeChain = BuildLiegeChainText(titles, FeudalUiServices.Structure);
            if (liegeChain.Length > 0)
                FeudalLiegeChainText = liegeChain;
            if (vassalClanIds.Count > 0)
                FeudalVassalsText = string.Join(", ", vassalClanIds.Select(ResolveClanName));

            int issued = FeudalUiServices.Levies?.GetLeviesIssuedBy(clan.StringId)
                .Count(call => call.Status == LevyStatus.Called) ?? 0;
            int owed = FeudalUiServices.Levies?.GetPendingLeviesFor(clan.StringId).Count ?? 0;
            if (issued > 0 || owed > 0)
            {
                TextObject leviesText = new TextObject("{=dadg_feudal_levies_counts}{ISSUED} issued, {OWED} owed");
                leviesText.SetTextVariable("ISSUED", issued);
                leviesText.SetTextVariable("OWED", owed);
                FeudalLeviesText = leviesText.ToString();
            }
        }

        [DataSourceProperty] public bool IsFeudalInfoVisible { get; }
        [DataSourceProperty] public string FeudalSectionText { get; }
        [DataSourceProperty] public string FeudalTitlesLabel { get; }
        [DataSourceProperty] public string FeudalTitlesText { get; }
        [DataSourceProperty] public string FeudalSuzerainLabel { get; }
        [DataSourceProperty] public string FeudalSuzerainText { get; }
        [DataSourceProperty] public string FeudalLiegeChainLabel { get; }
        [DataSourceProperty] public string FeudalLiegeChainText { get; }
        [DataSourceProperty] public string FeudalVassalsLabel { get; }
        [DataSourceProperty] public string FeudalVassalsText { get; }
        [DataSourceProperty] public string FeudalLeviesLabel { get; }
        [DataSourceProperty] public string FeudalLeviesText { get; }
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
        private static string BuildLiegeChainText(IReadOnlyList<Title> titles, IFeudalStructure? structure)
        {
            if (titles.Count == 0 || structure is null) return string.Empty;
            Title highest = titles.OrderByDescending(title => title.Rank).First();
            var liegeNames = new List<string>();
            string? liegeTitleId = structure.GetDeJureSuzerainTitleId(highest.Id);
            while (liegeTitleId is not null)
            {
                liegeNames.Add(structure.GetTitleName(liegeTitleId) ?? liegeTitleId);
                liegeTitleId = structure.GetDeJureSuzerainTitleId(liegeTitleId);
            }
            return string.Join(" > ", liegeNames);
        }

        private static string ResolveClanName(string clanId) =>
            Campaign.Current?.CampaignObjectManager.Find<Clan>(clanId)?.Name?.ToString() ?? clanId;
    }
}