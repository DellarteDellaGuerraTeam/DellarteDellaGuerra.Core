using System.Collections.Generic;

namespace DellarteDellaGuerra.Domain.Titles.Model
{
    /**
     * <summary>
     *  What a won claim war moved: whether the winner took the title fought for, the seats of the
     *  titles the winner took, and the clans whose primary title went with the won title into
     *  the winner's realm.
     * </summary>
     */
    public record WonClaimAward(
        bool TitleWon,
        IReadOnlyList<string> MovedSeatIds,
        IReadOnlyList<string> ClansJoiningWinnersRealm);
}
