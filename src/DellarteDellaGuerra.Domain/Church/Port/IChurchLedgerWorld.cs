using System.Collections.Generic;
using DellarteDellaGuerra.Domain.Church.Ledger;

namespace DellarteDellaGuerra.Domain.Church.Port
{
    public interface IChurchLedgerWorld
    {
        ChurchFoundationFacts? GetFoundation(string settlementId);

        IReadOnlyCollection<int> GetLivingClergyRelations();
    }
}
