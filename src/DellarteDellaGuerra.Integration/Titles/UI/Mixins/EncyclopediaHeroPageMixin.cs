using System.Collections.Generic;
using System.Linq;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.ViewModels;
using DellarteDellaGuerra.Domain.Titles.Model;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace DellarteDellaGuerra.Integration.Titles.UI.Mixins
{
    /// <summary>
    /// Adds a brief feudal summary (clan titles and suzerain) to the hero encyclopedia
    /// page. Hidden for heroes whose clan is outside the feudal structure.
    /// A new page VM is created on every encyclopedia navigation, so computing the
    /// values once here stays correct.
    /// </summary>
    [ViewModelMixin]
    public sealed class EncyclopediaHeroPageMixin : BaseViewModelMixin<EncyclopediaHeroPageVM>
    {
        public EncyclopediaHeroPageMixin(EncyclopediaHeroPageVM vm) : base(vm)
        {
            FeudalSectionText = new TextObject("{=dadg_feudal_section}Feudal").ToString();
            FeudalTitlesLabel = new TextObject("{=dadg_feudal_clan_titles}Clan Titles:").ToString();
            FeudalSuzerainLabel = new TextObject("{=dadg_feudal_suzerain}Suzerain:").ToString();
            string none = new TextObject("{=dadg_feudal_none}None").ToString();
            FeudalTitlesText = none;
            FeudalSuzerainText = none;

            Clan? clan = (vm.Obj as Hero)?.Clan;
            if (clan is null || !FeudalUiServices.IsInitialised) return;

            IReadOnlyList<Title> titles =
                FeudalUiServices.Titles?.GetTitlesByClan(clan.StringId) ?? new List<Title>();
            string? suzerainClanId = FeudalUiServices.GetSuzerain?.Execute(clan.StringId);
            Clan? suzerainClan = suzerainClanId is null
                ? null
                : Campaign.Current?.CampaignObjectManager.Find<Clan>(suzerainClanId);

            IsFeudalInfoVisible = titles.Count > 0 || suzerainClan is not null;
            if (titles.Count > 0)
                FeudalTitlesText = string.Join(", ", titles.Select(FormatTitleName));
            if (suzerainClan is not null)
                FeudalSuzerainText = suzerainClan.Name.ToString();
        }

        private static string FormatTitleName(Title title) =>
            title.Name + FeudalTitleDisplay.OccupiedSuffix(title.IsContested ? title.OccupantClanId : null);

        [DataSourceProperty] public bool IsFeudalInfoVisible { get; }
        [DataSourceProperty] public string FeudalSectionText { get; }
        [DataSourceProperty] public string FeudalTitlesLabel { get; }
        [DataSourceProperty] public string FeudalTitlesText { get; }
        [DataSourceProperty] public string FeudalSuzerainLabel { get; }
        [DataSourceProperty] public string FeudalSuzerainText { get; }
    }
}