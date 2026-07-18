namespace DellarteDellaGuerra.Domain.PrivateWars.Model
{
    public record PrivateWarArmyParty(
        string PartyId,
        string ClanId,
        bool CanLeadArmy,
        bool CanJoinArmy,
        float MemberDesirability);
}
