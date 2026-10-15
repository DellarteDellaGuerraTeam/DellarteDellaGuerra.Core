namespace DellarteDellaGuerra.Domain.Titles
{
    /**
     * <summary>
     *  Whether a claim war still has a quarrel to settle. It was declared against the house that
     *  held the title; once the dignity has passed to anyone else, by grant, inheritance into
     *  another house or another claimant's victory, there is nothing left to win from him.
     * </summary>
     * <remarks>
     *  Holding is de jure: a title whose seat another house occupies is still held, so an
     *  occupation never voids the war.
     * </remarks>
     */
    public static class ClaimWarValidity
    {
        public static bool IsVoid(string defenderClanId, string? holderClanId) =>
            holderClanId != defenderClanId;
    }
}
