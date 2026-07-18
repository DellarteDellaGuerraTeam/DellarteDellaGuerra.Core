namespace DellarteDellaGuerra.Domain.PrivateWars
{
    public enum ReinforcementSide
    {
        None,
        Player,
        Enemy
    }

    public class PrivateWarInteractionPolicy
    {
        public int RestrictRecruitableIndex(bool arePrivateEnemies, int vanillaMaximumIndex)
            => arePrivateEnemies ? -1 : vanillaMaximumIndex;

        public bool ShouldRunRecruitmentEntry(bool arePrivateEnemies)
            => !arePrivateEnemies;

        public bool AllowSettlementVisit(bool vanillaSuitable, bool arePrivateEnemies)
            => vanillaSuitable && !arePrivateEnemies;

        public ReinforcementSide ResolveReinforcementSide(
            bool vanillaEnemyOfPlayer,
            bool vanillaEnemyOfEncounteredParty,
            bool privateEnemyOfPlayer,
            bool privateEnemyOfEncounteredParty)
        {
            var enemyOfPlayer = vanillaEnemyOfPlayer || privateEnemyOfPlayer;
            var enemyOfEncounteredParty = vanillaEnemyOfEncounteredParty || privateEnemyOfEncounteredParty;

            if (!enemyOfPlayer && enemyOfEncounteredParty) return ReinforcementSide.Player;
            if (enemyOfPlayer && !enemyOfEncounteredParty) return ReinforcementSide.Enemy;
            return ReinforcementSide.None;
        }
    }
}
