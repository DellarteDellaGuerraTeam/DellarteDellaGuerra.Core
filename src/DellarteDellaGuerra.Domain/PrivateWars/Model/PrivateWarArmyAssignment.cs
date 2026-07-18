namespace DellarteDellaGuerra.Domain.PrivateWars.Model
{
    public record PrivateWarArmyAssignment(
        string PrivateWarId,
        WarSide Side,
        string GoalSettlementId,
        string LeaderPartyId);
}
