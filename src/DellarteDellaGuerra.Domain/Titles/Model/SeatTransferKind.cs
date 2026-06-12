namespace DellarteDellaGuerra.Domain.Titles.Model
{
    /// <summary>
    /// How a title's seat settlement changed hands, collapsed from the engine's
    /// ChangeOwnerOfSettlementDetail at the adapter boundary.
    /// </summary>
    public enum SeatTransferKind
    {
        /// <summary>Taken by force (siege, rebellion): the dignity does not move.</summary>
        Conquest,

        /// <summary>Conveyed by decision (king's grant, gift, barter): the dignity follows.</summary>
        Grant,

        /// <summary>Bookkeeping change (clan destroyed, faction left): the dignity follows.</summary>
        Administrative
    }
}