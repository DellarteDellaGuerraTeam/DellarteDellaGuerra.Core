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

        void Declare(
            string attackerClanId,
            string defenderClanId,
            string titleId,
            string mainGoalSettlementId,
            float day);
    }
}
