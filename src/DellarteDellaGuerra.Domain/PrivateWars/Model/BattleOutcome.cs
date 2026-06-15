namespace DellarteDellaGuerra.Domain.PrivateWars.Model
{
    /// <summary>
    /// The result of one resolved battle between the two sides of a private war, fed into
    /// <see cref="DellarteDellaGuerra.Domain.PrivateWars.ApplyBattleOutcomeUseCase"/>. The battle
    /// bump is normalized by the *losing side's total strength* so a decisive engagement is worth
    /// far more than many skirmishes (design §18.B).
    /// </summary>
    public record BattleOutcome(
        WarSide Winner,
        float EnemyForceDefeated,
        float LosingSideTotalStrength);
}
