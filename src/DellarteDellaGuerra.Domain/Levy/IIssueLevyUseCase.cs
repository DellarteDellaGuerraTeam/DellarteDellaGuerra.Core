using System.Collections.Generic;
using DellarteDellaGuerra.Domain.Levy.Model;

namespace DellarteDellaGuerra.Domain.Levy
{
    public interface IIssueLevyUseCase
    {
        IReadOnlyList<LevyCall> Execute(string issuingClanId, IEnumerable<string> vassalClanIds, float currentDay);
    }
}
