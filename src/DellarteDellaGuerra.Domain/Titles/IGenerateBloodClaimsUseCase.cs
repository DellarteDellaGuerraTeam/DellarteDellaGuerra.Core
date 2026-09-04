using System.Collections.Generic;
using DellarteDellaGuerra.Domain.Titles.Model;

namespace DellarteDellaGuerra.Domain.Titles
{
    public interface IGenerateBloodClaimsUseCase
    {
        IReadOnlyList<Claim> Execute();
    }
}
