namespace DellarteDellaGuerra.Domain.Titles
{
    public interface IComputeInfluenceTierBonusUseCase
    {
        float Execute(string clanId);
    }
}
