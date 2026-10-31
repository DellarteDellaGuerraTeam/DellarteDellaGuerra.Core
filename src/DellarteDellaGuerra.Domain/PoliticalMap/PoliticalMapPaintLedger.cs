using System.Collections.Generic;
using System.Linq;

namespace DellarteDellaGuerra.Domain.PoliticalMap
{
    /// <summary>
    /// Remembers each territory's last paint signature (<see cref="PoliticalMapPalette.Signature"/>),
    /// so the daily backstop repaints only the territories whose colours moved without an event
    /// saying so: a holder who changed clan, a clan that changed colour.
    /// </summary>
    public class PoliticalMapPaintLedger
    {
        private readonly Dictionary<string, IReadOnlyList<uint>> _signatureByTerritoryId = new();

        /// <summary>Records the signature and says whether it differs from the last one, or is the first.</summary>
        public bool Record(string territoryId, IReadOnlyList<uint> signature)
        {
            bool changed = !_signatureByTerritoryId.TryGetValue(territoryId, out IReadOnlyList<uint> previous)
                           || !previous.SequenceEqual(signature);
            _signatureByTerritoryId[territoryId] = signature;
            return changed;
        }

        public void Clear() => _signatureByTerritoryId.Clear();
    }
}
