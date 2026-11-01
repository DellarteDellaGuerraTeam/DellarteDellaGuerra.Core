using System;
using System.Collections.Generic;
using System.Linq;

namespace DellarteDellaGuerra.Domain.PoliticalMap.Overlay
{
    /// <summary>Paint revisions for one cached map: stale worker results cannot become visible.</summary>
    public sealed class PoliticalMapMeshPlan
    {
        private readonly IReadOnlyList<string> _territories;
        private readonly IReadOnlyList<(int A, int B)> _neighbours;
        private readonly HashSet<string> _dirty = new();
        private PoliticalMapSettlementPaint[] _paints = Array.Empty<PoliticalMapSettlementPaint>();
        private int _revision;

        public PoliticalMapMeshPlan(IReadOnlyList<string> territories, IReadOnlyList<(int A, int B)> neighbours)
        {
            _territories = territories;
            _neighbours = neighbours;
        }

        public bool IsReady => _revision > 0 && _dirty.Count == 0;

        public bool Update(IReadOnlyList<PoliticalMapSettlementPaint> paints)
        {
            if (paints.Count != _territories.Count) throw new ArgumentException("One paint per settlement is required.", nameof(paints));
            var changed = new bool[paints.Count];
            bool any = false;
            for (int index = 0; index < paints.Count; index++)
            {
                if (index < _paints.Length && _paints[index].HasSameGeometry(paints[index])) continue;
                changed[index] = any = true;
                _dirty.Add(_territories[index]);
            }
            if (!any) return false;

            var touched = (bool[])changed.Clone();
            foreach ((int a, int b) in _neighbours)
            {
                if (changed[a]) { _dirty.Add(_territories[b]); touched[b] = true; }
                if (changed[b]) { _dirty.Add(_territories[a]); touched[a] = true; }
            }
            // A changed junction can alter dash phase further along the same political boundary.
            var affected = new HashSet<(string, string)>();
            foreach ((int a, int b) in _neighbours)
            {
                if (!touched[a] && !touched[b]) continue;
                if (Boundary(paints, a, b) is { } current) affected.Add(current);
                if (Boundary(_paints, a, b) is { } old) affected.Add(old);
            }
            // Rephase the affected border chains, keeping the kingdoms' unrelated interior cached.
            foreach ((int a, int b) in _neighbours)
                if (Boundary(paints, a, b) is { } current && affected.Contains(current)
                    || Boundary(_paints, a, b) is { } old && affected.Contains(old))
                {
                    _dirty.Add(_territories[a]);
                    _dirty.Add(_territories[b]);
                }
            _paints = paints.ToArray();
            _revision++;
            return true;
        }

        private static (string, string)? Boundary(IReadOnlyList<PoliticalMapSettlementPaint> paints, int a, int b)
        {
            if (a >= paints.Count || b >= paints.Count) return null;
            PoliticalMapSettlementPaint x = paints[a], y = paints[b];
            if (x.Fill == 0 || y.Fill == 0 || x.FrontierKey is not { } first || y.FrontierKey is not { } second || first == second)
                return null;
            return string.CompareOrdinal(first, second) < 0 ? (first, second) : (second, first);
        }

        public PoliticalMapMeshRequest Capture() => new(_revision, _paints.ToArray(), _dirty.ToArray());

        public bool IsCurrent(PoliticalMapMeshRequest request) => request.Revision == _revision;

        public bool Complete(PoliticalMapMeshRequest request)
        {
            if (!IsCurrent(request)) return false;
            _dirty.Clear();
            return true;
        }
    }

    public sealed record PoliticalMapMeshRequest(
        int Revision, IReadOnlyList<PoliticalMapSettlementPaint> Paints, IReadOnlyList<string> Territories);
}
