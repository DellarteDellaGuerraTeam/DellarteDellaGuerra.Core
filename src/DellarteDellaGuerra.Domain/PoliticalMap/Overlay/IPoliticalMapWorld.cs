namespace DellarteDellaGuerra.Domain.PoliticalMap.Overlay
{
    /// <summary>The campaign facts the political map paints from that the title tree does not hold.</summary>
    public interface IPoliticalMapWorld
    {
        /// <summary>The town or castle a settlement is painted with: itself, or a village's bound settlement.</summary>
        string? SeatOf(string settlementId);

        /// <summary>The clan owning a settlement, a village answering for its bound settlement's owner.</summary>
        string? OwnerClanOf(string settlementId);

        /// <summary>The faction (kingdom, or a kingdomless clan) controlling a settlement.</summary>
        string? FactionOf(string settlementId);

        /// <summary>A clan's colour as ARGB, null for an unknown clan.</summary>
        uint? ClanColor(string clanId);

        /// <summary>A faction's colour as ARGB, null for an unknown faction.</summary>
        uint? FactionColor(string factionId);

        /// <summary>The kingdom a clan is sworn to, null for a kingdomless clan or an unknown one.</summary>
        string? KingdomOfClan(string clanId);

        /// <summary>The clan ruling a kingdom, null for an unknown kingdom or one without a ruler.</summary>
        string? RulerClanOf(string kingdomId);
    }
}
