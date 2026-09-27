using System.Collections.Generic;
using DellarteDellaGuerra.Domain.Church.Favour;

namespace DellarteDellaGuerra.Domain.Church.Ledger
{
    public class ChurchLedger
    {
        public ChurchLedger(
            ChurchFavourProgress favour,
            IReadOnlyList<ChurchLedgerEntry> roots)
        {
            Favour = favour;
            Roots = roots;
        }

        public ChurchFavourProgress Favour { get; }
        public IReadOnlyList<ChurchLedgerEntry> Roots { get; }
    }
}
