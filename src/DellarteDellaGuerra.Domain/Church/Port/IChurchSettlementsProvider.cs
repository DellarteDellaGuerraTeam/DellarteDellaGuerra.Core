using System.Collections.Generic;

namespace DellarteDellaGuerra.Domain.Church.Port
{
    public interface IChurchSettlementsProvider
    {
        IReadOnlyCollection<ChurchSettlementData> GetChurchSettlements();
    }
}
