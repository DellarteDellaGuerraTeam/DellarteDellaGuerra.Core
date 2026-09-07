using System;
using System.Collections.Generic;

namespace DellarteDellaGuerra.Titles.Api.Campaign
{
    /**
     * <summary>
     *  The private-war mechanism as the claim evaluator needs it: is this clan already
     *  fighting one, what would the war be fought over, and start it. The Integration layer
     *  adapts the private-wars submodule API to this interface.
     * </summary>
     */
    public interface IPrivateWarDeclaration
    {
        bool IsBelligerent(string clanId);

        /**
         * <summary>
         *  The settlement whose control would drive the war, frozen at declaration: the
         *  highest-prosperity settlement of the claimed title the defender actually holds.
         *  Null when he holds none — with no main goal the claim cannot be pressed at all.
         * </summary>
         */
        string? SelectMainGoal(string defenderClanId, IReadOnlyList<string> deJureSettlementIds);

        /**
         * <summary>
         *  Opens the war. The supporter sets name the clans that pledged to a side against what the
         *  feudal hierarchy alone would put them on — a vassal defecting from its liege, or an
         *  uninvolved clan rallying to a principal. Empty sets leave every side to the hierarchy.
         * </summary>
         */
        void Declare(
            string attackerClanId,
            string defenderClanId,
            string titleId,
            string mainGoalSettlementId,
            float day,
            IReadOnlyCollection<string> attackerSupporters,
            IReadOnlyCollection<string> defenderSupporters);

        /**
         * <summary>
         *  Raised as each war ends, however it ended.
         * </summary>
         */
        event Action<PrivateWarConclusion> WarConcluded;
    }

    /**
     * <summary>
     *  How a private war ended, told from the side that started it. AttackerWon is false for a
     *  defender victory and for a white peace alike: in both the claim went unwon.
     * </summary>
     */
    public record PrivateWarConclusion(string AttackerClanId, bool AttackerWon);
}
