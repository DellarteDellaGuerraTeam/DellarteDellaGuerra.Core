using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.Levy.Model;
using DellarteDellaGuerra.Domain.Levy.Port;

namespace DellarteDellaGuerra.Infrastructure.Levy
{
    public class InMemoryLevyRegistry : ILevyRepository
    {
        private readonly Dictionary<string, LevyCall> _callsById = new();

        public void Initialise(IEnumerable<LevyCall> calls)
        {
            _callsById.Clear();
            foreach (var call in calls)
                _callsById[call.Id] = call;
        }

        public IReadOnlyList<LevyCall> Snapshot() => _callsById.Values.ToList();

        public LevyCall? GetLevyCall(string id)
            => _callsById.TryGetValue(id, out var call) ? call : null;

        public IReadOnlyList<LevyCall> GetLeviesIssuedBy(string issuingClanId)
            => _callsById.Values.Where(c => c.IssuingClanId == issuingClanId).ToList();

        public IReadOnlyList<LevyCall> GetPendingLeviesFor(string vassalClanId)
            => _callsById.Values
                .Where(c => c.VassalClanId == vassalClanId && c.Status == LevyStatus.Called)
                .ToList();

        public void SaveLevyCall(LevyCall call) => _callsById[call.Id] = call;

        public void RemoveLevyCall(string id) => _callsById.Remove(id);

        public IReadOnlyList<LevyCall> GetAllLevyCalls() => _callsById.Values.ToList();
    }
}
