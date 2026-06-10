using DellarteDellaGuerra.Domain.Levy.Model;
using DellarteDellaGuerra.Domain.Levy.Port;

namespace DellarteDellaGuerra.Domain.Tests.Levy
{
    internal sealed class FakeLevyRepository : ILevyRepository
    {
        private readonly Dictionary<string, LevyCall> _calls = new();

        public FakeLevyRepository(params LevyCall[] calls)
        {
            foreach (var call in calls)
            {
                _calls[call.Id] = call;
            }
        }

        public LevyCall? GetLevyCall(string id) => _calls.TryGetValue(id, out var call) ? call : null;

        public IReadOnlyList<LevyCall> GetLeviesIssuedBy(string issuingClanId) =>
            _calls.Values.Where(call => call.IssuingClanId == issuingClanId).ToList();

        public IReadOnlyList<LevyCall> GetPendingLeviesFor(string vassalClanId) =>
            _calls.Values
                .Where(call => call.VassalClanId == vassalClanId && call.Status == LevyStatus.Called)
                .ToList();

        public void SaveLevyCall(LevyCall call) => _calls[call.Id] = call;

        public void RemoveLevyCall(string id) => _calls.Remove(id);

        public IReadOnlyList<LevyCall> GetAllLevyCalls() => _calls.Values.ToList();

        public void Initialise(IEnumerable<LevyCall> calls)
        {
            _calls.Clear();
            foreach (var call in calls)
            {
                _calls[call.Id] = call;
            }
        }

        public IReadOnlyList<LevyCall> Snapshot() => _calls.Values.ToList();
    }
}
