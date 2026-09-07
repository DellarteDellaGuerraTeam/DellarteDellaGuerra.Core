using System.Collections.Generic;
using DellarteDellaGuerra.Domain.Titles.Model;

namespace DellarteDellaGuerra.Domain.Titles
{
    public interface ISolicitSupportUseCase
    {
        SupportDecision Execute(ClaimOpportunity opportunity, IReadOnlyCollection<SupportCandidate> candidates);
    }
}
