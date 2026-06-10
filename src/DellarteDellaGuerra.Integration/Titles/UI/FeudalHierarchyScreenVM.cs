using System;
using DellarteDellaGuerra.Domain.Titles.Model;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace DellarteDellaGuerra.Integration.Titles.UI
{
    /// <summary>
    /// Data source for the standalone feudal hierarchy screen: the whole de jure forest
    /// flattened depth-first into indented rows.
    /// </summary>
    public class FeudalHierarchyScreenVM : ViewModel
    {
        private readonly Action _close;

        public FeudalHierarchyScreenVM(Action close)
        {
            _close = close;
            Nodes = new MBBindingList<FeudalTitleNodeVM>();
            TitleText = new TextObject("{=dadg_feudal_hierarchy_title}Feudal Hierarchy").ToString();
            CloseText = GameTexts.FindText("str_done").ToString();

            FeudalMap? map = FeudalUiServices.BuildFeudalMap?.Execute();
            if (map is null) return;
            foreach (FeudalMapEntry realm in map.Realms) Append(realm, 0);
        }

        [DataSourceProperty] public MBBindingList<FeudalTitleNodeVM> Nodes { get; }
        [DataSourceProperty] public string TitleText { get; }
        [DataSourceProperty] public string CloseText { get; }

        public void ExecuteClose() => _close();

        private void Append(FeudalMapEntry entry, int depth)
        {
            Nodes.Add(new FeudalTitleNodeVM(entry, depth));
            foreach (FeudalMapEntry vassal in entry.Vassals) Append(vassal, depth + 1);
        }
    }
}