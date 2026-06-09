using System.Linq;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Domain.Titles.Model;
using DellarteDellaGuerra.Domain.Titles.Port;

namespace DellarteDellaGuerra.Domain.Titles
{
    public class AssignTitleUseCase : IAssignTitleUseCase
    {
        private readonly ITitleRepository _titleRepository;
        private readonly IClaimRepository _claimRepository;
        private readonly ILogger _logger;

        public AssignTitleUseCase(
            ITitleRepository titleRepository,
            IClaimRepository claimRepository,
            ILoggerFactory loggerFactory)
        {
            _titleRepository = titleRepository;
            _claimRepository = claimRepository;
            _logger = loggerFactory.CreateLogger<AssignTitleUseCase>();
        }

        public AssignmentResult? Execute(string settlementId, string? newHolderClanId)
        {
            var title = _titleRepository.GetTitleBySeat(settlementId);
            if (title is null)
            {
                _logger.Debug($"No title found for settlement '{settlementId}'");
                return null;
            }

            string? previousHolderClanId = title.HolderClanId;
            if (previousHolderClanId == newHolderClanId)
            {
                return new AssignmentResult(title.Id, previousHolderClanId, newHolderClanId, false);
            }

            _titleRepository.SaveTitle(title.WithHolder(newHolderClanId));
            _logger.Info($"Title '{title.Id}' reassigned from '{previousHolderClanId ?? "<vacant>"}' to '{newHolderClanId ?? "<vacant>"}'");

            bool claimGenerated = false;
            if (previousHolderClanId != null)
            {
                string claimId = $"{title.Id}:{previousHolderClanId}:conquest";
                bool claimExists = _claimRepository
                    .GetClaimsOn(title.Id)
                    .Any(claim => claim.Id == claimId);
                if (!claimExists)
                {
                    _claimRepository.AddClaim(
                        new Claim(claimId, previousHolderClanId, title.Id, ClaimStrength.Strong, ClaimOrigin.Conquest));
                    claimGenerated = true;
                }
            }

            return new AssignmentResult(title.Id, previousHolderClanId, newHolderClanId, claimGenerated);
        }
    }
}
