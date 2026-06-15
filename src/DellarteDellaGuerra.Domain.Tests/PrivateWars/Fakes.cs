using System.Collections.Generic;
using DellarteDellaGuerra.Domain.PrivateWars.Model;
using DellarteDellaGuerra.Domain.PrivateWars.Port;

namespace DellarteDellaGuerra.Domain.Tests.PrivateWars
{
    internal sealed class FakePrivateWarRepository : IPrivateWarRepository
    {
        private readonly Dictionary<string, PrivateWar> _wars = new();

        public FakePrivateWarRepository(params PrivateWar[] wars)
        {
            foreach (var war in wars) _wars[war.Id] = war;
        }

        public PrivateWar? Get(string id) => _wars.TryGetValue(id, out var war) ? war : null;

        public IReadOnlyList<PrivateWar> GetByPrincipalPair(string attackerPrincipalClanId, string defenderPrincipalClanId) =>
            _wars.Values
                .Where(w => w.AttackerPrincipalClanId == attackerPrincipalClanId
                            && w.DefenderPrincipalClanId == defenderPrincipalClanId)
                .ToList();

        public IReadOnlyList<PrivateWar> GetByDefender(string defenderPrincipalClanId) =>
            _wars.Values.Where(w => w.DefenderPrincipalClanId == defenderPrincipalClanId).ToList();

        public IReadOnlyList<PrivateWar> GetByTitle(string titleId) =>
            _wars.Values.Where(w => w.TitleId == titleId).ToList();

        public IReadOnlyList<PrivateWar> GetByClan(string principalClanId) =>
            _wars.Values
                .Where(w => w.AttackerPrincipalClanId == principalClanId
                            || w.DefenderPrincipalClanId == principalClanId)
                .ToList();

        public IReadOnlyList<PrivateWar> GetAll() => _wars.Values.ToList();

        public void Add(PrivateWar war) => _wars[war.Id] = war;

        public void Update(PrivateWar war) => _wars[war.Id] = war;

        public void Remove(string id) => _wars.Remove(id);

        public void Initialise(IEnumerable<PrivateWar> wars)
        {
            _wars.Clear();
            foreach (var war in wars) _wars[war.Id] = war;
        }

        public IReadOnlyList<PrivateWar> Snapshot() => _wars.Values.ToList();
    }

    /// <summary>Shared builders so tests state only the fields they care about.</summary>
    internal static class PrivateWarTestData
    {
        public static readonly PrivateWarObservations NoControl = new(
            AttackerHoldsMainGoal: false,
            DefenderSideTownsHeldByAttacker: 0,
            DefenderSideCastlesHeldByAttacker: 0,
            AttackerSideTownsHeldByDefender: 0,
            AttackerSideCastlesHeldByDefender: 0,
            DefenderClanPrisonersHeldByAttackerSide: 0,
            AttackerClanPrisonersHeldByDefenderSide: 0,
            AccumulatedBattleScore: 0f);

        public static PrivateWar War(
            string attacker = "clan_attacker",
            string defender = "clan_defender",
            string title = "title_county",
            string mainGoal = "settlement_goal",
            float battleScore = 0f,
            IReadOnlyDictionary<string, string>? fiefSnapshot = null,
            float startDay = 0f) =>
            new(
                Id: $"pw_{attacker}_{defender}",
                AttackerPrincipalClanId: attacker,
                DefenderPrincipalClanId: defender,
                CasusBelliType: "claim",
                TitleId: title,
                MainGoalSettlementId: mainGoal,
                OriginalFiefOwners: fiefSnapshot ?? new Dictionary<string, string>(),
                BattleScore: battleScore,
                Score: 0f,
                StartDay: startDay,
                Status: PrivateWarStatus.Active);
    }
}
