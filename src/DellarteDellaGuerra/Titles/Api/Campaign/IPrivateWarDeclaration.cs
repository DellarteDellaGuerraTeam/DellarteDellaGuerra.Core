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
         *  Whether the attacker's side could besiege the main goal today: someone on it can raise
         *  an army that beats the goal's walled defence. A war it could not fight would sit declared
         *  and unprosecuted. The attacker may be a house not founded yet, in which case only the
         *  supporters count.
         * </summary>
         */
        bool CanProsecute(
            string attackerClanId,
            string defenderClanId,
            string mainGoalSettlementId,
            IReadOnlyCollection<string> attackerSupporters,
            IReadOnlyCollection<string> defenderSupporters);

        /**
         * <summary>
         *  Opens the war. The supporter sets name the clans that pledged to a side against what the
         *  feudal hierarchy alone would put them on — a vassal defecting from its liege, or an
         *  uninvolved clan rallying to a principal. Empty sets leave every side to the hierarchy.
         *  The foreign supporters, those of either set from outside the principals' kingdom, fight
         *  alone: their vassals are not brought in with them.
         * </summary>
         */
        void Declare(
            string attackerClanId,
            string defenderClanId,
            string titleId,
            string mainGoalSettlementId,
            float day,
            IReadOnlyCollection<string> attackerSupporters,
            IReadOnlyCollection<string> defenderSupporters,
            IReadOnlyCollection<string> foreignSupporters);

        /**
         * <summary>
         *  Whether the clan fought on the attacker's side of the war, by pledge or through its
         *  liege. Answers for a war that has already ended.
         * </summary>
         */
        bool IsOnAttackerSide(string warId, string clanId);

        /**
         * <summary>
         *  Every claim war still being fought, with the clan it was declared against.
         * </summary>
         */
        IReadOnlyList<ActiveClaimWar> GetActiveClaimWars();

        /**
         * <summary>
         *  Ends the war in a white peace through the mechanism's own resolution, so captured fiefs
         *  revert and <see cref="WarConcluded"/> is raised as for any other ending.
         * </summary>
         */
        void EndInWhitePeace(string warId);

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
    public record PrivateWarConclusion(string WarId, string AttackerClanId, string DefenderClanId, string TitleId, bool AttackerWon);

    public record ActiveClaimWar(string WarId, string DefenderClanId, string TitleId);
}
