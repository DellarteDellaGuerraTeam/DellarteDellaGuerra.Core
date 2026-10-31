namespace DellarteDellaGuerra.Domain.PoliticalMap.Overlay
{
    /// <summary>The display names the political map's legend and hover readout show.</summary>
    public interface IPoliticalMapNames
    {
        /// <summary>A settlement's name, null for an unknown settlement.</summary>
        string? SettlementName(string settlementId);

        /// <summary>A faction's (kingdom's, or kingdomless clan's) name, null for an unknown faction.</summary>
        string? FactionName(string factionId);

        /// <summary>A clan's name, null for an unknown clan.</summary>
        string? ClanName(string clanId);
    }
}
