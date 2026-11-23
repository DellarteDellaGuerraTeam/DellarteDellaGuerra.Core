using System;
using System.Collections.Generic;
using DellarteDellaGuerra.Domain.PoliticalMap.Grid;
using DellarteDellaGuerra.Domain.Titles.Model;

namespace DellarteDellaGuerra.Domain.PoliticalMap.Overlay
{
    /// <summary>
    /// Folds each demesne into a county. A demesne is a territory seated above the counties (a
    /// duchy seated at a castle, a barony straight under a duchy): left alone it would show as a
    /// small island of another colour inside the counties around it. It is painted as the county
    /// that shares the longest border with it, read from the grid when the map loads.
    /// </summary>
    /// <remarks>
    /// The border is the demesne's outer rim: the cells just outside it, through the four sides of
    /// a cell, that belong to a county. The county owning most of them wins; a tie goes to the
    /// smaller id, so the fold is the same on every load. A demesne touching no county is not folded.
    /// </remarks>
    public static class PoliticalMapDemesneFold
    {
        /// <param name="grid">The settlement grid.</param>
        /// <param name="territoryOfSettlement">The territory of each grid settlement, by its 0-based index in <see cref="PoliticalMapGrid.SettlementIds"/>.</param>
        /// <param name="rankOf">A territory's title rank, null for a territory that is no title.</param>
        public static IReadOnlyDictionary<string, string> Fold(
            PoliticalMapGrid grid,
            IReadOnlyList<string?> territoryOfSettlement,
            Func<string, TitleRank?> rankOf)
        {
            var isDemesne = new Dictionary<string, bool>();
            bool IsDemesne(string territory)
            {
                if (!isDemesne.TryGetValue(territory, out bool demesne))
                {
                    TitleRank? rank = rankOf(territory);
                    isDemesne[territory] = demesne = rank is not null && rank != TitleRank.Earl;
                }

                return demesne;
            }

            string? TerritoryAt(int index)
            {
                ushort cell = grid.Cells[index];
                return cell == PoliticalMapGrid.None ? null : territoryOfSettlement[cell - 1];
            }

            var rims = new Dictionary<string, HashSet<int>>();
            for (int row = 0; row < grid.Height; row++)
            {
                for (int column = 0; column < grid.Width; column++)
                {
                    int index = row * grid.Width + column;
                    string? territory = TerritoryAt(index);
                    if (territory is null || !IsDemesne(territory)) continue;

                    if (!rims.TryGetValue(territory, out HashSet<int>? rim)) rims[territory] = rim = new HashSet<int>();
                    if (column > 0) AddRim(index - 1);
                    if (column < grid.Width - 1) AddRim(index + 1);
                    if (row > 0) AddRim(index - grid.Width);
                    if (row < grid.Height - 1) AddRim(index + grid.Width);

                    void AddRim(int neighbour)
                    {
                        string? other = TerritoryAt(neighbour);
                        if (other is not null && other != territory && rankOf(other) == TitleRank.Earl) rim.Add(neighbour);
                    }
                }
            }

            var folds = new Dictionary<string, string>();
            foreach (KeyValuePair<string, HashSet<int>> entry in rims)
            {
                var counts = new Dictionary<string, int>();
                foreach (int cell in entry.Value)
                {
                    string county = TerritoryAt(cell)!;
                    counts[county] = counts.TryGetValue(county, out int count) ? count + 1 : 1;
                }

                string? best = null;
                foreach (KeyValuePair<string, int> count in counts)
                {
                    if (best is null || count.Value > counts[best]
                        || (count.Value == counts[best] && string.CompareOrdinal(count.Key, best) < 0))
                    {
                        best = count.Key;
                    }
                }

                if (best is not null) folds[entry.Key] = best;
            }

            return folds;
        }
    }
}
