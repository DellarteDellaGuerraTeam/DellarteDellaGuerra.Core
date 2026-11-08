using System.Collections.Generic;

namespace DellarteDellaGuerra.Domain.Titles
{
    public interface IGetInheritanceSettlementsUseCase
    {
        IReadOnlyList<string> Execute(string titleId);
    }
}
