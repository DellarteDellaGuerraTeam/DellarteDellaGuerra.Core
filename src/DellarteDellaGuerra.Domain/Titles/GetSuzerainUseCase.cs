namespace DellarteDellaGuerra.Domain.Titles
{
    public class GetSuzerainUseCase : IGetSuzerainUseCase
    {
        private readonly SuzeraintyPolicy _suzeraintyPolicy;

        public GetSuzerainUseCase(SuzeraintyPolicy suzeraintyPolicy)
        {
            _suzeraintyPolicy = suzeraintyPolicy;
        }

        public string? Execute(string clanId) => _suzeraintyPolicy.GetSuzerain(clanId);
    }
}
