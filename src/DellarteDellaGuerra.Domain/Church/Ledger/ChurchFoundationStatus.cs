using System;

namespace DellarteDellaGuerra.Domain.Church.Ledger
{
    [Flags]
    public enum ChurchFoundationStatus
    {
        None = 0,
        PilgrimageShrine = 1,
        Vacant = 2,
        UnderRaid = 4
    }
}
