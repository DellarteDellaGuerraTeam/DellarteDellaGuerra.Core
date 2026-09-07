namespace DellarteDellaGuerra.Titles.Api.Campaign
{
    /**
     * <summary>
     *  Splits a cadet branch off a clan and folds it back in. A hero with a claim on his own
     *  house's dignity has no clan to press it with, because a clan cannot go to war with
     *  itself; this gives him one, and takes it away again if he loses.
     * </summary>
     */
    public interface ICadetBranch
    {
        /**
         * <summary>
         *  Sets the claimant up at the head of a new house styled after the seat he claims,
         *  inside his old clan's kingdom, taking his children and the men he leads with him.
         *  Returns the new clan's id, or null if the house could not be founded.
         * </summary>
         */
        string? Split(string claimantHeroId, string parentClanId, string seatSettlementId);

        /**
         * <summary>
         *  Returns the cadet branch's surviving members to the parent clan and destroys the
         *  empty shell.
         * </summary>
         */
        void Reabsorb(string cadetClanId, string parentClanId);
    }
}
