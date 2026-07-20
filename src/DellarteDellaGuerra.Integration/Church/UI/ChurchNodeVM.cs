using System.Linq;
using DellarteDellaGuerra.Domain.Church.Hierarchy;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace DellarteDellaGuerra.Integration.Church.UI
{
    /// <summary>
    /// One row of the flattened church hierarchy tree. GauntletUI prefabs cannot nest
    /// recursively, so depth is expressed as a left indent on a flat list.
    /// The holder line is resolved live from the campaign: the settlement's living preacher
    /// notable with its clergy title, or "(vacant)".
    /// </summary>
    public class ChurchNodeVM : ViewModel
    {
        public ChurchNodeVM(ChurchMapEntry entry, int depth)
        {
            Settlement? settlement = Campaign.Current is null ? null : Settlement.Find(entry.SettlementId);
            RankText = entry.Rank.ToString();
            NameText = entry.Rank == ChurchNodeRank.See
                ? entry.Name
                : settlement?.Name.ToString() ?? entry.SettlementId;
            IsSee = depth == 0;
            IndentMargin = depth * 45f;
            HolderLineText = BuildHolderLine(settlement);
        }

        [DataSourceProperty] public string RankText { get; }
        [DataSourceProperty] public string NameText { get; }
        [DataSourceProperty] public bool IsSee { get; }
        [DataSourceProperty] public float IndentMargin { get; }
        [DataSourceProperty] public string HolderLineText { get; }

        private static string BuildHolderLine(Settlement? settlement)
        {
            if (settlement is null || ChurchUiServices.Settlements is null) return string.Empty;

            Hero? cleric = settlement.Notables.FirstOrDefault(notable => notable.IsPreacher && notable.IsAlive);
            if (cleric is null) return new TextObject("{=hZ4tKb9N}(vacant)").ToString();

            var line = new TextObject("{=mV3sXr8K}{TITLE} {NAME} of {SETTLEMENT}");
            line.SetTextVariable("TITLE", ChurchUiServices.Settlements.GetClergyTitle(settlement));
            line.SetTextVariable("NAME", cleric.Name);
            line.SetTextVariable("SETTLEMENT", settlement.Name);
            return line.ToString();
        }
    }
}
