using System.Collections.Generic;

namespace DellarteDellaGuerra.Domain.Titles.Model
{
    /**
     * <summary>
     *  What a won claim war moved: the seats of the titles the winner took, and the clans whose
     *  primary title went with the won title into the winner's realm.
     * </summary>
     */
    public record WonClaimAward(IReadOnlyList<string> MovedSeatIds, IReadOnlyList<string> ClansJoiningWinnersRealm);
}
