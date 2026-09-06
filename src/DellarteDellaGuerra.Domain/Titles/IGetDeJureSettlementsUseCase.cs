using System.Collections.Generic;

namespace DellarteDellaGuerra.Domain.Titles
{
    public interface IGetDeJureSettlementsUseCase
    {
        IReadOnlyList<string> Execute(string titleId);
    }
}
