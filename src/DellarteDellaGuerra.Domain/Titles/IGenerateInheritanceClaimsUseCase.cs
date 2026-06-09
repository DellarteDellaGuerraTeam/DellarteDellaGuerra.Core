using System.Collections.Generic;
using DellarteDellaGuerra.Domain.Titles.Model;

namespace DellarteDellaGuerra.Domain.Titles
{
    public interface IGenerateInheritanceClaimsUseCase
    {
        IReadOnlyList<Claim> Execute(string deceasedClanId, IReadOnlyList<string> passedOverHeirClanIds);
    }
}
