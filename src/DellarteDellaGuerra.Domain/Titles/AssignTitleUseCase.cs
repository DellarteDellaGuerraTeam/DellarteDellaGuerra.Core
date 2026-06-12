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

        public AssignmentResult? Execute(
            string settlementId,
            string? newClanId,
            SeatTransferKind transferKind,
            float currentDay)
        {
            var title = _titleRepository.GetTitleBySeat(settlementId);
            if (title is null)
            {
                _logger.Debug($"No title found for settlement '{settlementId}'");
                return null;
            }

            // Conquest of a held title does not move the dignity; everything else does.
            // A vacant title has no holder to dispossess, so conquest transfers it directly.
            if (transferKind == SeatTransferKind.Conquest && title.HolderClanId is not null)
            {
                return ExecuteConquest(title, newClanId, currentDay);
            }

            return ExecuteTransfer(title, newClanId);
        }

        private AssignmentResult ExecuteConquest(Title title, string? occupantClanId, float currentDay)
        {
            if (occupantClanId == title.HolderClanId)
            {
                // The de jure holder retook (or already holds) its own seat: contest resolved.
                if (title.OccupantClanId is not null || title.ContestedSinceDay is not null)
                {
                    _titleRepository.SaveTitle(title.WithOccupant(null, null));
                    _logger.Info($"Title '{title.Id}' contest resolved: holder '{title.HolderClanId}' retook the seat");
                }

                return new AssignmentResult(title.Id, title.HolderClanId, title.HolderClanId, false);
            }

            if (occupantClanId == title.OccupantClanId)
            {
                return new AssignmentResult(title.Id, title.HolderClanId, title.HolderClanId, false, true);
            }

            // A new occupant restarts the contested clock.
            _titleRepository.SaveTitle(title.WithOccupant(occupantClanId, currentDay));
            _logger.Info($"Title '{title.Id}' contested: '{occupantClanId ?? "<vacant>"}' occupies the seat of '{title.HolderClanId}'");

            return new AssignmentResult(title.Id, title.HolderClanId, title.HolderClanId, false, true);
        }

        private AssignmentResult ExecuteTransfer(Title title, string? newHolderClanId)
        {
            string? previousHolderClanId = title.HolderClanId;
            if (previousHolderClanId == newHolderClanId)
            {
                if (title.OccupantClanId is not null || title.ContestedSinceDay is not null)
                {
                    _titleRepository.SaveTitle(title.WithOccupant(null, null));
                }

                return new AssignmentResult(title.Id, previousHolderClanId, newHolderClanId, false);
            }

            _titleRepository.SaveTitle(title.WithHolder(newHolderClanId).WithOccupant(null, null));
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