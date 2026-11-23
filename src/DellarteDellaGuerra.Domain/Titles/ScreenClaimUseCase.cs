using DellarteDellaGuerra.Domain.Titles.Model;

namespace DellarteDellaGuerra.Domain.Titles
{
    /**
     * <summary>
     *  Whether a claim could become a private war at all, before anyone prices it. Returns why
     *  not, or null when the claim may go on to be priced.
     * </summary>
     * <remarks>
     *  The royal house is out of it on both sides: the crown settles its claims as king, not by
     *  feud, and raising a private war against the crown is treason. A royal pretender pressing
     *  a claim on the crown's own dignity is attacking the crown too.
     *
     *  A clan cannot go to war with itself, so a claim on its own dignity is only actionable if
     *  a hero of the house is behind it to leave with.
     * </remarks>
     */
    public class ScreenClaimUseCase : IScreenClaimUseCase
    {
        public ClaimRejection? Execute(ClaimCandidacy candidacy)
        {
            if (!WeakClaimPolicy.IsPressable(candidacy.Strength, candidacy.Holder))
                return ClaimRejection.TooWeak;
            if (candidacy.PlayerClanId is not null
                && (candidacy.ClaimantClanId == candidacy.PlayerClanId || candidacy.DefenderClanId == candidacy.PlayerClanId))
                return ClaimRejection.PlayerClanInvolved;
            if (candidacy.RulingClanId is not null
                && (candidacy.ClaimantClanId == candidacy.RulingClanId || candidacy.DefenderClanId == candidacy.RulingClanId))
                return ClaimRejection.RoyalHouseInvolved;
            if (candidacy.ClaimantClanId == candidacy.DefenderClanId && !candidacy.HasPretender)
                return ClaimRejection.OwnDignityWithoutPretender;
            if (candidacy.ClaimantKingdomId is null || candidacy.ClaimantKingdomId != candidacy.DefenderKingdomId)
                return ClaimRejection.NotSameRealm;
            if (candidacy.ClaimantAtPrivateWar)
                return ClaimRejection.AlreadyAtPrivateWar;
            return null;
        }
    }
}
