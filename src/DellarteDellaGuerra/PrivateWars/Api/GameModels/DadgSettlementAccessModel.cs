using DellarteDellaGuerra.Titles.Api;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;

namespace DellarteDellaGuerra.PrivateWars.Api.GameModels
{
    // Treat a same-kingdom private-war rival's town or castle as hostile (no entry) for the player.
    //
    // DefaultSettlementAccessModel.CanMainHeroEnterSettlement grants FullAccess to same-faction
    // settlements via DiplomacyHelper.IsSameFactionAndNotEliminated — two clans in the same kingdom
    // share a MapFaction, so the player walks unchallenged into the rival's town/castle. Override to
    // return NoAccess/HostileFaction when the settlement's owner clan is a registered private-war
    // enemy of the player's clan. All non-enemy settlements fall through to vanilla unchanged.
    public class DadgSettlementAccessModel : DefaultSettlementAccessModel
    {
        public override void CanMainHeroEnterSettlement(Settlement settlement, out AccessDetails accessDetails)
        {
            if (settlement.IsFortification
                && IsPrivateWarEnemy(settlement.OwnerClan))
            {
                accessDetails = new AccessDetails
                {
                    AccessLevel = AccessLevel.NoAccess,
                    AccessMethod = AccessMethod.ByRequest,
                    AccessLimitationReason = AccessLimitationReason.HostileFaction
                };
                return;
            }

            base.CanMainHeroEnterSettlement(settlement, out accessDetails);
        }

        private static bool IsPrivateWarEnemy(Clan? ownerClan)
        {
            if (!FeudalServices.IsInitialised || FeudalServices.PrivateWarHostility is null) return false;
            var playerClan = Hero.MainHero?.Clan;
            if (playerClan is null || ownerClan is null) return false;
            return FeudalServices.PrivateWarHostility.AreEnemies(playerClan.StringId, ownerClan.StringId);
        }
    }
}
