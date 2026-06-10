using System;
using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.Levy.Model;
using DellarteDellaGuerra.Domain.Levy.Port;

namespace DellarteDellaGuerra.Domain.Levy
{
    // Creates a new LevyCall for each vassal that does not already have a pending one
    // from the same issuing lord. Skips vassals that have already answered or refused
    // but have a call still in the registry.
    public class IssueLevyUseCase : IIssueLevyUseCase
    {
        private readonly ILevyRepository _levyRepository;

        public IssueLevyUseCase(ILevyRepository levyRepository)
        {
            _levyRepository = levyRepository;
        }

        public IReadOnlyList<LevyCall> Execute(
            string issuingClanId,
            IEnumerable<string> vassalClanIds,
            float currentDay)
        {
            var issued = new List<LevyCall>();
            var existingActive = new HashSet<string>(
                _levyRepository.GetLeviesIssuedBy(issuingClanId)
                    .Where(c => c.Status == LevyStatus.Called)
                    .Select(c => c.VassalClanId));

            foreach (string vassalClanId in vassalClanIds)
            {
                if (existingActive.Contains(vassalClanId)) continue;

                var call = new LevyCall(
                    Id: $"levy_{issuingClanId}_{vassalClanId}_{Guid.NewGuid():N}",
                    IssuingClanId: issuingClanId,
                    VassalClanId: vassalClanId,
                    IssuedAtDay: currentDay,
                    Status: LevyStatus.Called);

                _levyRepository.SaveLevyCall(call);
                issued.Add(call);
            }

            return issued;
        }
    }
}
