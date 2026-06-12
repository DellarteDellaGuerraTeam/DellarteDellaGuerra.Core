using DellarteDellaGuerra.Domain.Titles.Model;

namespace DellarteDellaGuerra.Domain.Titles
{
    public interface IAssignTitleUseCase
    {
        AssignmentResult? Execute(
            string settlementId,
            string? newClanId,
            SeatTransferKind transferKind,
            float currentDay);
    }
}