using System;
using System.Globalization;
using DellarteDellaGuerra.Domain.Church.Favour;
using DellarteDellaGuerra.Domain.Church.Ledger;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace DellarteDellaGuerra.Integration.Church.UI
{
    /// <summary>
    /// Data source for the standalone church hierarchy screen: the diocese forest
    /// flattened depth-first into indented rows.
    /// </summary>
    public class ChurchHierarchyScreenVM : ViewModel
    {
        private readonly Action _close;
        private ChurchDetailsVM? _details;
        private ChurchNodeVM? _selectedNode;

        public ChurchHierarchyScreenVM(Action close)
        {
            _close = close;
            Nodes = new MBBindingList<ChurchNodeVM>();
            TitleText = new TextObject("{=xQ7mVe2J}The Church in England").ToString();
            LedgerTitleText = new TextObject("{=qA5vZt8G}Dioceses and foundations").ToString();
            DetailsTitleText = new TextObject("{=eN3kWs7R}Foundation details").ToString();
            CloseText = GameTexts.FindText("str_done").ToString();
            ChurchLedger? ledger = ChurchUiServices.BuildChurchLedger?.Execute();
            if (ledger is null)
            {
                FavourText = new TextObject("{=xD6pFq2L}Standing unavailable").ToString();
                return;
            }

            SetFavourHeader(ledger.Favour);
            foreach (ChurchLedgerEntry see in ledger.Roots) Append(see, 0);
            if (Nodes.Count > 0) SelectNode(Nodes[0]);
        }

        [DataSourceProperty] public MBBindingList<ChurchNodeVM> Nodes { get; }
        [DataSourceProperty] public string TitleText { get; }
        [DataSourceProperty] public string LedgerTitleText { get; }
        [DataSourceProperty] public string DetailsTitleText { get; }
        [DataSourceProperty] public string FavourText { get; private set; } = string.Empty;
        [DataSourceProperty] public string FavourProgressText { get; private set; } = string.Empty;
        [DataSourceProperty] public string CloseText { get; }

        [DataSourceProperty]
        public ChurchDetailsVM? Details
        {
            get => _details;
            private set
            {
                if (value == _details) return;
                _details = value;
                OnPropertyChangedWithValue(value, nameof(Details));
            }
        }

        public void ExecuteClose() => _close();

        private void Append(ChurchLedgerEntry entry, int depth)
        {
            Nodes.Add(new ChurchNodeVM(entry, depth, SelectNode));
            foreach (ChurchLedgerEntry child in entry.Children) Append(child, depth + 1);
        }

        private void SelectNode(ChurchNodeVM node)
        {
            if (_selectedNode != null) _selectedNode.IsSelected = false;
            _selectedNode = node;
            _selectedNode.IsSelected = true;
            Details = new ChurchDetailsVM(node.Entry);
        }

        private void SetFavourHeader(ChurchFavourProgress status)
        {
            var average = status.AverageRelation.ToString("+0.0;-0.0;0.0", CultureInfo.InvariantCulture);
            var summary = new TextObject("{=cJ4mNv8T}Standing: {RANK} ({AVERAGE} average relation)");
            summary.SetTextVariable("RANK", FormatRank(status.Rank));
            summary.SetTextVariable("AVERAGE", average);
            FavourText = summary.ToString();

            if (status.NextBoundary.HasValue)
            {
                var threshold = status.NextBoundary.Value.ToString("+0.0;-0.0;0.0", CultureInfo.InvariantCulture);
                var progress = status.NextBoundaryComparison == ChurchFavourBoundaryComparison.GreaterThan
                    ? new TextObject("{=gW7rKp3B}Next standing: above {THRESHOLD} average relation")
                    : new TextObject("{=uR9bLq4F}Next standing: {THRESHOLD} average relation");
                progress.SetTextVariable("THRESHOLD", threshold);
                FavourProgressText = progress.ToString();
            }
            else
            {
                FavourProgressText = new TextObject("{=sM2hQc9V}Highest standing attained").ToString();
            }
        }

        private static string FormatRank(ChurchFavourRank rank)
        {
            switch (rank)
            {
                case ChurchFavourRank.Reviled:
                    return new TextObject("{=rP6tYx3K}Reviled").ToString();
                case ChurchFavourRank.IllRegarded:
                    return new TextObject("{=vH8nLm4Q}Ill-Regarded").ToString();
                case ChurchFavourRank.Favoured:
                    return new TextObject("{=dC5kRs7W}Favoured").ToString();
                case ChurchFavourRank.Beloved:
                    return new TextObject("{=nB3qJf9S}Beloved").ToString();
                default:
                    return new TextObject("{=zT4mVw6G}Indifferent").ToString();
            }
        }
    }
}
