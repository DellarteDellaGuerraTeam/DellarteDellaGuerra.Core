using System.Collections.Generic;
using DellarteDellaGuerra.Domain.Levy.Model;
using DellarteDellaGuerra.Domain.Levy.Port;

namespace DellarteDellaGuerra.Domain.Levy
{
    // Marks as Refused all Called levy calls that have been outstanding longer than expiryDays.
    // Returns the newly refused calls so callers can apply relation penalties.
    public class ExpireLeviesUseCase : IExpireLeviesUseCase
    {
        private readonly ILevyRepository _levyRepository;

        public ExpireLeviesUseCase(ILevyRepository levyRepository)
        {
            _levyRepository = levyRepository;
        }

        public IReadOnlyList<LevyCall> Execute(float currentDay, float expiryDays)
        {
            var refused = new List<LevyCall>();

            foreach (var call in _levyRepository.GetAllLevyCalls())
            {
                if (call.Status != LevyStatus.Called) continue;
                if (currentDay - call.IssuedAtDay < expiryDays) continue;

                var refusedCall = call with { Status = LevyStatus.Refused };
                _levyRepository.SaveLevyCall(refusedCall);
                refused.Add(refusedCall);
            }

            return refused;
        }
    }
}
