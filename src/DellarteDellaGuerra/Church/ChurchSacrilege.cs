using System.Linq;
using DellarteDellaGuerra.Domain.Church.Port;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace DellarteDellaGuerra.Church
{
    public class ChurchSacrilege
    {
        private readonly ChurchSettlements _churchSettlements;
        private readonly IChurchSettingsProvider _churchSettingsProvider;

        public ChurchSacrilege(ChurchSettlements churchSettlements, IChurchSettingsProvider churchSettingsProvider)
        {
            _churchSettlements = churchSettlements;
            _churchSettingsProvider = churchSettingsProvider;
        }

        public void Apply(Hero offender, Settlement site)
        {
            var settings = _churchSettingsProvider.GetSettings();
            var isPlayerOffender = offender == Hero.MainHero;

            foreach (var settlement in Settlement.All)
            {
                if (!_churchSettlements.IsChurchSettlement(settlement)) continue;

                var relationChange = settlement == site
                    ? settings.SacrilegeRelationLocal
                    : settings.SacrilegeRelationOthers;
                foreach (var abbot in settlement.Notables.Where(notable => notable.IsPreacher && notable.IsAlive))
                {
                    if (isPlayerOffender) ChangeRelationAction.ApplyPlayerRelation(abbot, relationChange);
                    else ChangeRelationAction.ApplyRelationChangeBetweenHeroes(offender, abbot, relationChange);
                }
            }

            if (!isPlayerOffender) return;
            var message = new TextObject(
                "{=gB2xLj4C}Word of your sacrilege at {SETTLEMENT} spreads among the clergy of England.");
            message.SetTextVariable("SETTLEMENT", site.Name);
            InformationManager.DisplayMessage(new InformationMessage(message.ToString()));
        }
    }
}
