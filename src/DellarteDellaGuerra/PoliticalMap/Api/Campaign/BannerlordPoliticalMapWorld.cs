using DellarteDellaGuerra.Domain.PoliticalMap;
using DellarteDellaGuerra.Domain.PoliticalMap.Overlay;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using BannerlordCampaign = TaleWorlds.CampaignSystem.Campaign;

namespace DellarteDellaGuerra.PoliticalMap.Api.Campaign
{
    /// <summary>The running campaign's settlements, clans and kingdoms, as the political map paints and names them.</summary>
    public class BannerlordPoliticalMapWorld : IPoliticalMapWorld, IPoliticalMapNames
    {
        public string? SeatOf(string settlementId)
        {
            Settlement? settlement = Settlement.Find(settlementId);
            if (settlement is null) return null;
            return settlement.IsVillage ? settlement.Village?.Bound?.StringId : settlement.StringId;
        }

        public string? OwnerClanOf(string settlementId)
        {
            string? seat = SeatOf(settlementId);
            return seat is null ? null : Settlement.Find(seat)?.OwnerClan?.StringId;
        }

        public string? FactionOf(string settlementId) => Settlement.Find(settlementId)?.MapFaction?.StringId;

        public uint? ClanColor(string clanId) => FindClan(clanId)?.Color;

        public uint? FactionColor(string factionId) => PoliticalMapPalette.KingdomColor(
            factionId, (BannerlordCampaign.Current?.CampaignObjectManager.Find<Kingdom>(factionId)?.Color) ?? ClanColor(factionId));

        public string? KingdomOfClan(string clanId) => FindClan(clanId)?.Kingdom?.StringId;

        public string? RulerClanOf(string kingdomId) =>
            BannerlordCampaign.Current?.CampaignObjectManager.Find<Kingdom>(kingdomId)?.RulingClan?.StringId;

        public string? SettlementName(string settlementId) => Settlement.Find(settlementId)?.Name?.ToString();

        public string? FactionName(string factionId) =>
            (BannerlordCampaign.Current?.CampaignObjectManager.Find<Kingdom>(factionId)?.Name ?? FindClan(factionId)?.Name)?.ToString();

        public string? ClanName(string clanId) => FindClan(clanId)?.Name?.ToString();

        private static Clan? FindClan(string clanId) => BannerlordCampaign.Current?.CampaignObjectManager.Find<Clan>(clanId);
    }
}
