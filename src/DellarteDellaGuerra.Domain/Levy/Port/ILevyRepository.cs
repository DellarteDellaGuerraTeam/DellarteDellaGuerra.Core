using System.Collections.Generic;
using DellarteDellaGuerra.Domain.Levy.Model;

namespace DellarteDellaGuerra.Domain.Levy.Port
{
    public interface ILevyRepository
    {
        LevyCall? GetLevyCall(string id);
        IReadOnlyList<LevyCall> GetLeviesIssuedBy(string issuingClanId);
        IReadOnlyList<LevyCall> GetPendingLeviesFor(string vassalClanId);
        void SaveLevyCall(LevyCall call);
        void RemoveLevyCall(string id);
        IReadOnlyList<LevyCall> GetAllLevyCalls();

        // Lifecycle hooks for save/load support
        void Initialise(IEnumerable<LevyCall> calls);
        IReadOnlyList<LevyCall> Snapshot();
    }
}
