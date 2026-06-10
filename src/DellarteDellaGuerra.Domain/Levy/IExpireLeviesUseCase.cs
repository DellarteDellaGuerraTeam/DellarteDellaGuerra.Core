using System.Collections.Generic;
using DellarteDellaGuerra.Domain.Levy.Model;

namespace DellarteDellaGuerra.Domain.Levy
{
    public interface IExpireLeviesUseCase
    {
        IReadOnlyList<LevyCall> Execute(float currentDay, float expiryDays);
    }
}
