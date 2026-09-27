using System;
using System.Collections.Generic;
using System.Linq;
using Bannerlord.PrivateWars.Api;
using Bannerlord.PrivateWars.Domain;
using Bannerlord.PrivateWars.Domain.Model;
using DellarteDellaGuerra.Titles.Api.Campaign;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using PrivateWarOutcome = Bannerlord.PrivateWars.Api.PrivateWarOutcome;
using PrivateWarStatus = Bannerlord.PrivateWars.Api.PrivateWarStatus;

namespace DellarteDellaGuerra.Integration.PrivateWars
{
    // Adapts the private-wars submodule API to the IPrivateWarDeclaration port the claim
    // evaluator uses to check belligerency, pick a main goal, and start a war over a claim.
    public class PrivateWarDeclarationAdapter : IPrivateWarDeclaration
    {
        private readonly IPrivateWarsApi _privateWars;
        private readonly MainGoalSelector _mainGoalSelector;

        public PrivateWarDeclarationAdapter(IPrivateWarsApi privateWars, MainGoalSelector mainGoalSelector)
        {
            _privateWars = privateWars;
            _mainGoalSelector = mainGoalSelector;

            // Relayed rather than raised here, because the submodule announces every resolution
            // it makes, including the ones its own daily tick decides on score.
            _privateWars.WarResolved += (war, outcome) => WarConcluded?.Invoke(
                new PrivateWarConclusion(
                    war.AttackerClanId,
                    war.DefenderClanId,
                    war.TitleId,
                    outcome == PrivateWarOutcome.AttackerVictory));
        }

        public event Action<PrivateWarConclusion>? WarConcluded;

        public bool IsBelligerent(string clanId) =>
            _privateWars.GetWarsByClan(clanId).Any(w => w.Status == PrivateWarStatus.Active);

        public string? SelectMainGoal(string defenderClanId, IReadOnlyList<string> deJureSettlementIds)
        {
            var settlements = deJureSettlementIds
                .Select(Settlement.Find)
                .Where(s => s != null)
                .Select(s => new SettlementInfo(s!.StringId, s.OwnerClan?.StringId, s.IsTown, s.Town?.Prosperity ?? 0f));

            return _mainGoalSelector.Select(settlements, defenderClanId);
        }

        public void Declare(
            string attackerClanId,
            string defenderClanId,
            string titleId,
            string mainGoalSettlementId,
            float day,
            IReadOnlyCollection<string> attackerSupporters,
            IReadOnlyCollection<string> defenderSupporters)
        {
            Clan? defender = Campaign.Current?.CampaignObjectManager.Find<Clan>(defenderClanId);
            IReadOnlyDictionary<string, string> fiefSnapshot = defender == null
                ? new Dictionary<string, string>()
                : defender.Settlements
                    .Where(s => s.OwnerClan != null)
                    .ToDictionary(s => s.StringId, s => s.OwnerClan.StringId);

            _privateWars.DeclareWar(
                attackerClanId,
                defenderClanId,
                ClaimCasusBelli.ClaimType,
                titleId,
                mainGoalSettlementId,
                fiefSnapshot,
                day,
                attackerSupporters,
                defenderSupporters);
        }
    }
}
