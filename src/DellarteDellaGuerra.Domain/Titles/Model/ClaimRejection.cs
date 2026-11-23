namespace DellarteDellaGuerra.Domain.Titles.Model
{
    /** <summary>Why a claim cannot become a war, in the order ScreenClaimUseCase checks it.</summary> */
    public enum ClaimRejection
    {
        TooWeak,
        PlayerClanInvolved,
        RoyalHouseInvolved,
        OwnDignityWithoutPretender,
        NotSameRealm,
        AlreadyAtPrivateWar
    }
}
