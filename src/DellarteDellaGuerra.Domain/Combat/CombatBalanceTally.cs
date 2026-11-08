using System;
using System.Collections.Generic;
using System.Linq;

namespace DellarteDellaGuerra.Domain.Combat
{
    /// <summary>
    /// Totals the blows of one battle by a group the caller names.
    /// </summary>
    public class CombatBalanceTally
    {
        private class Row
        {
            public int Hits;
            public int RawDamage;
            public float Armour;
            public int Damage;
            public int MaxDamage;
            public int Kills;
        }

        private readonly Dictionary<string, Row> _rows = new();

        public void Add(string group, int rawDamage, float armour, int damage, bool killed)
        {
            if (!_rows.TryGetValue(group, out var row))
            {
                row = new Row();
                _rows[group] = row;
            }

            row.Hits++;
            row.RawDamage += rawDamage;
            row.Armour += armour;
            row.Damage += damage;
            row.MaxDamage = Math.Max(row.MaxDamage, damage);
            if (killed) row.Kills++;
        }

        public IReadOnlyList<string> Report()
        {
            return _rows.OrderBy(entry => entry.Key, StringComparer.Ordinal)
                .Select(entry => Format(entry.Key, entry.Value))
                .ToList();
        }

        private static string Format(string group, Row row)
        {
            float hits = row.Hits;
            return $"{group}: hits={row.Hits} raw={row.RawDamage / hits:F1} armour={row.Armour / hits:F1} " +
                   $"damage={row.Damage / hits:F1} max={row.MaxDamage} kills={row.Kills}";
        }
    }
}
