using System;
using DellarteDellaGuerra.Domain.Church.Hierarchy;
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

        public ChurchHierarchyScreenVM(Action close)
        {
            _close = close;
            Nodes = new MBBindingList<ChurchNodeVM>();
            TitleText = new TextObject("{=xQ7mVe2J}The Church in England").ToString();
            CloseText = GameTexts.FindText("str_done").ToString();

            ChurchMap? map = ChurchUiServices.BuildChurchMap?.Execute();
            if (map is null) return;
            foreach (ChurchMapEntry see in map.Roots) Append(see, 0);
        }

        [DataSourceProperty] public MBBindingList<ChurchNodeVM> Nodes { get; }
        [DataSourceProperty] public string TitleText { get; }
        [DataSourceProperty] public string CloseText { get; }

        public void ExecuteClose() => _close();

        private void Append(ChurchMapEntry entry, int depth)
        {
            Nodes.Add(new ChurchNodeVM(entry, depth));
            foreach (ChurchMapEntry child in entry.Children) Append(child, depth + 1);
        }
    }
}
