using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Domain.Titles.Model;
using DellarteDellaGuerra.Domain.Titles.Port;

namespace DellarteDellaGuerra.Domain.Tests.Titles
{
    internal sealed class FakeTitleRepository : ITitleRepository
    {
        private readonly Dictionary<string, Title> _titles = new();

        public FakeTitleRepository(params Title[] titles)
        {
            foreach (var title in titles)
            {
                _titles[title.Id] = title;
            }
        }

        public Title? GetTitle(string titleId) => _titles.TryGetValue(titleId, out var title) ? title : null;

        public Title? GetTitleBySeat(string settlementId) =>
            _titles.Values.FirstOrDefault(title => title.SeatSettlementId == settlementId);

        public IReadOnlyList<Title> GetTitlesByClan(string clanId) =>
            _titles.Values.Where(title => title.HolderClanId == clanId).ToList();

        public IReadOnlyList<Title> GetAllTitles() => _titles.Values.ToList();

        public void SaveTitle(Title title) => _titles[title.Id] = title;
    }

    internal sealed class FakeClaimRepository : IClaimRepository
    {
        private readonly Dictionary<string, Claim> _claims = new();

        public IReadOnlyList<Claim> GetClaimsFor(string claimantClanId) =>
            _claims.Values.Where(claim => claim.ClaimantClanId == claimantClanId).ToList();

        public IReadOnlyList<Claim> GetClaimsOn(string titleId) =>
            _claims.Values.Where(claim => claim.TitleId == titleId).ToList();

        public void AddClaim(Claim claim) => _claims[claim.Id] = claim;

        public void RemoveClaim(string claimId) => _claims.Remove(claimId);

        public IReadOnlyList<Claim> AllClaims => _claims.Values.ToList();
    }

    internal sealed class FakeTensionRepository : ITensionRepository
    {
        private readonly Dictionary<(string, string), FeudalTension> _tensions = new();

        public FeudalTension? GetTension(string claimantClanId, string titleId) =>
            _tensions.TryGetValue((claimantClanId, titleId), out var tension) ? tension : null;

        public IReadOnlyList<FeudalTension> GetTensionsFor(string claimantClanId) =>
            _tensions.Values.Where(tension => tension.ClaimantClanId == claimantClanId).ToList();

        public void SetTension(FeudalTension tension) =>
            _tensions[(tension.ClaimantClanId, tension.TitleId)] = tension;

        public void ResetTension(string claimantClanId, string titleId) =>
            _tensions.Remove((claimantClanId, titleId));
    }

    internal sealed class FakeFeudalStructure : IFeudalStructure
    {
        private readonly Dictionary<string, string?> _parents = new();
        private readonly Dictionary<string, string> _seats = new();
        private readonly Dictionary<string, TitleRank> _ranks = new();
        private readonly Dictionary<string, string> _names = new();

        public FakeFeudalStructure AddTitle(
            string titleId,
            TitleRank rank,
            string? parentTitleId = null,
            string? seatSettlementId = null,
            string? name = null)
        {
            _parents[titleId] = parentTitleId;
            _ranks[titleId] = rank;
            _names[titleId] = name ?? titleId;
            if (seatSettlementId != null)
            {
                _seats[seatSettlementId] = titleId;
            }

            return this;
        }

        public string? GetDeJureSuzerainTitleId(string titleId) =>
            _parents.TryGetValue(titleId, out var parentTitleId) ? parentTitleId : null;

        public IReadOnlyList<string> GetDeJureVassalTitleIds(string titleId) =>
            _parents.Where(entry => entry.Value == titleId).Select(entry => entry.Key).ToList();

        public string? GetTitleIdBySeat(string settlementId) =>
            _seats.TryGetValue(settlementId, out var titleId) ? titleId : null;

        public IReadOnlyList<string> GetAllTitleIds() => _parents.Keys.ToList();

        public TitleRank? GetRank(string titleId) =>
            _ranks.TryGetValue(titleId, out var rank) ? rank : null;

        public string? GetTitleName(string titleId) =>
            _names.TryGetValue(titleId, out var name) ? name : null;
    }

    internal sealed class FakeLogger : ILogger
    {
        public void Debug(string message, Exception? exception = null) { }
        public void Info(string message, Exception? exception = null) { }
        public void Warn(string message, Exception? exception = null) { }
        public void Error(string message, Exception? exception = null) { }
        public void Fatal(string message, Exception? exception = null) { }
    }

    internal sealed class FakeLoggerFactory : ILoggerFactory
    {
        public ILogger CreateLogger<T>() => new FakeLogger();
    }
}
