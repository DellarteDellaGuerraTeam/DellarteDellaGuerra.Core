namespace DellarteDellaGuerra.Domain.Titles
{
    public interface IComputeFeudalSupportUseCase
    {
        float Execute(string voterClanId, string settlementId);
    }
}
