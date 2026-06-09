using System.Collections.Generic;

namespace DellarteDellaGuerra.Domain.Titles
{
    public interface IGetDirectVassalsUseCase
    {
        IReadOnlyList<string> Execute(string clanId);
    }
}
