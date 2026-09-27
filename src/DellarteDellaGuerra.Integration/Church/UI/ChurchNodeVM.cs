using System;
using System.Collections.Generic;
using DellarteDellaGuerra.Domain.Church.Hierarchy;
using DellarteDellaGuerra.Domain.Church.Ledger;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace DellarteDellaGuerra.Integration.Church.UI
{
    /// <summary>
    /// One row of the flattened church hierarchy tree. GauntletUI prefabs cannot nest
    /// recursively, so depth is expressed as a left indent on a flat list.
    /// The holder line renders the domain ledger projection without querying campaign state.
    /// </summary>
    public class ChurchNodeVM : ViewModel
    {
        private readonly Action<ChurchNodeVM> _select;
        private bool _isSelected;

        public ChurchNodeVM(ChurchLedgerEntry entry, int depth, Action<ChurchNodeVM> select)
        {
            Entry = entry;
            _select = select;
            RankText = FormatRank(entry.Rank);
            NameText = entry.Name;
            IsSee = entry.Rank == ChurchNodeRank.See;
            IndentMargin = depth * 45f;
            HolderLineText = BuildHolderLine(entry);
            BadgeText = BuildBadgeText(entry);
            HasBadge = BadgeText.Length > 0;
        }

        internal ChurchLedgerEntry Entry { get; }

        [DataSourceProperty] public string RankText { get; }
        [DataSourceProperty] public string NameText { get; }
        [DataSourceProperty] public bool IsSee { get; }
        [DataSourceProperty] public float IndentMargin { get; }
        [DataSourceProperty] public string HolderLineText { get; }
        [DataSourceProperty] public string BadgeText { get; }
        [DataSourceProperty] public bool HasBadge { get; }

        [DataSourceProperty]
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (value == _isSelected) return;
                _isSelected = value;
                OnPropertyChangedWithValue(value, nameof(IsSelected));
            }
        }

        public void ExecuteSelect() => _select(this);

        private static string BuildHolderLine(ChurchLedgerEntry entry)
        {
            if (!entry.IsAvailable) return string.Empty;
            if (entry.ClergyName is null || !entry.ClergyOffice.HasValue)
                return new TextObject("{=hZ4tKb9N}(vacant)").ToString();

            var line = new TextObject("{=mV3sXr8K}{TITLE} {NAME} of {SETTLEMENT}");
            line.SetTextVariable("TITLE", ChurchUiText.FormatClergyOffice(entry.ClergyOffice.Value));
            line.SetTextVariable("NAME", entry.ClergyName);
            line.SetTextVariable("SETTLEMENT", entry.SettlementName);
            return line.ToString();
        }

        private static string BuildBadgeText(ChurchLedgerEntry entry)
        {
            if (!entry.IsAvailable) return string.Empty;

            var badges = new List<string>();
            if (entry.Status.HasFlag(ChurchFoundationStatus.PilgrimageShrine))
                badges.Add(new TextObject("{=cE8rTn4V}SHRINE").ToString());
            if (entry.Status.HasFlag(ChurchFoundationStatus.Vacant))
                badges.Add(new TextObject("{=pL3wQa7M}VACANT").ToString());
            if (entry.Status.HasFlag(ChurchFoundationStatus.UnderRaid))
                badges.Add(new TextObject("{=gH5kZs2P}UNDER RAID").ToString());
            return string.Join("  •  ", badges);
        }

        private static string FormatRank(ChurchNodeRank rank)
        {
            switch (rank)
            {
                case ChurchNodeRank.See:
                    return new TextObject("{=zR7xVp3N}See").ToString();
                case ChurchNodeRank.Cathedral:
                    return new TextObject("{=dM2kHs8Q}Cathedral").ToString();
                case ChurchNodeRank.Priory:
                    return new TextObject("{=yT4bWn6J}Priory").ToString();
                default:
                    return new TextObject("{=aQ9fLc5K}Abbey").ToString();
            }
        }
    }
}
