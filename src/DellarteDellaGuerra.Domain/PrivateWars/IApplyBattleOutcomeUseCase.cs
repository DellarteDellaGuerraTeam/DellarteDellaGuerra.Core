using DellarteDellaGuerra.Domain.PrivateWars.Model;

namespace DellarteDellaGuerra.Domain.PrivateWars
{
    public interface IApplyBattleOutcomeUseCase
    {
        PrivateWar Execute(PrivateWar war, BattleOutcome outcome);
    }
}
