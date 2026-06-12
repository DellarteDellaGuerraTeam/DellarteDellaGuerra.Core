namespace DellarteDellaGuerra.Domain.Titles.Model
{
    public record AssignmentResult(
        string TitleId,
        string? PreviousHolderClanId,
        string? NewHolderClanId,
        bool ClaimGenerated,
        bool Contested = false);
}