using DellarteDellaGuerra.Domain.Titles.Model;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace DellarteDellaGuerra.Integration.Titles.UI
{
    /// <summary>
    /// One row of the flattened feudal hierarchy tree. GauntletUI prefabs cannot nest
    /// recursively, so depth is expressed as a left indent on a flat list.
    /// </summary>
    public class FeudalTitleNodeVM : ViewModel
    {
        public FeudalTitleNodeVM(FeudalMapEntry entry, int depth)
        {
            RankText = entry.Rank.ToString();
            NameText = entry.Name;
            IsRealm = depth == 0;
            IndentMargin = depth * 45f;
            HolderLineText = BuildHolderLine(entry);
        }

        [DataSourceProperty] public string RankText { get; }
        [DataSourceProperty] public string NameText { get; }
        [DataSourceProperty] public bool IsRealm { get; }
        [DataSourceProperty] public float IndentMargin { get; }
        [DataSourceProperty] public string HolderLineText { get; }

        private static string BuildHolderLine(FeudalMapEntry entry)
        {
            string holder = ResolveHolderName(entry.HolderClanId);
            string? seat = ResolveSeatName(entry.SeatSettlementId);
            return seat is null ? holder : $"{holder} - {seat}";
        }

        private static string ResolveHolderName(string? holderClanId)
        {
            if (holderClanId is null)
                return new TextObject("{=dadg_title_vacant}Vacant").ToString();

            Clan? clan = Campaign.Current?.CampaignObjectManager.Find<Clan>(holderClanId);
            return clan?.Name.ToString() ?? holderClanId;
        }

        private static string? ResolveSeatName(string seatSettlementId)
        {
            if (string.IsNullOrEmpty(seatSettlementId)) return null;
            return Settlement.Find(seatSettlementId)?.Name.ToString();
        }
    }
}