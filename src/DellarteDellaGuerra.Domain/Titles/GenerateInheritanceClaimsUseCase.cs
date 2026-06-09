using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.Titles.Model;
using DellarteDellaGuerra.Domain.Titles.Port;

namespace DellarteDellaGuerra.Domain.Titles
{
    public class GenerateInheritanceClaimsUseCase : IGenerateInheritanceClaimsUseCase
    {
        private readonly ITitleRepository _titleRepository;
        private readonly IClaimRepository _claimRepository;

        public GenerateInheritanceClaimsUseCase(ITitleRepository titleRepository, IClaimRepository claimRepository)
        {
            _titleRepository = titleRepository;
            _claimRepository = claimRepository;
        }

        public IReadOnlyList<Claim> Execute(string deceasedClanId, IReadOnlyList<string> passedOverHeirClanIds)
        {
            var createdClaims = new List<Claim>();
            foreach (var title in _titleRepository.GetTitlesByClan(deceasedClanId))
            {
                var existingClaimIds = _claimRepository
                    .GetClaimsOn(title.Id)
                    .Select(claim => claim.Id)
                    .ToList();

                foreach (string heirClanId in passedOverHeirClanIds)
                {
                    if (heirClanId == deceasedClanId) continue;

                    string claimId = $"{title.Id}:{heirClanId}:inheritance";
                    if (existingClaimIds.Contains(claimId)) continue;

                    var claim = new Claim(claimId, heirClanId, title.Id, ClaimStrength.Strong, ClaimOrigin.Inheritance);
                    _claimRepository.AddClaim(claim);
                    createdClaims.Add(claim);
                }
            }

            return createdClaims;
        }
    }
}
