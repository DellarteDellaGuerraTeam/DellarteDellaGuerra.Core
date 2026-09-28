using System.Collections.Generic;
using DellarteDellaGuerra.Domain.Titles.Model;

namespace DellarteDellaGuerra.Domain.Titles
{
    public interface IExecuteSuccessionUseCase
    {
        IReadOnlyList<SuccessionResult> Execute(string deceasedHeroId, float currentDay);
    }
}
