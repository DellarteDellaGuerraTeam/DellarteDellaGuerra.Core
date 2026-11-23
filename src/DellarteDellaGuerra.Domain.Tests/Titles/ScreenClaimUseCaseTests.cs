using System;
using DellarteDellaGuerra.Domain.Titles;
using DellarteDellaGuerra.Domain.Titles.Model;
using Xunit;

namespace DellarteDellaGuerra.Domain.Tests.Titles
{
    public class ScreenClaimUseCaseTests
    {
        private static readonly ScreenClaimUseCase UseCase = new();

        private static readonly HeroNode AdultMan = new("holder", false, true, "clan_b", Array.Empty<string>(), Age: 40f);

        private static ClaimCandidacy Feud(string claimant = "clan_a", string defender = "clan_b") =>
            new(claimant, defender, ClaimStrength.Strong, AdultMan, "player_clan", "clan_king",
                "england", "england", HasPretender: false, ClaimantAtPrivateWar: false);

        [Fact]
        public void A_Strong_Claim_Between_Two_Vassal_Houses_Of_One_Realm_Passes()
        {
            Assert.Null(UseCase.Execute(Feud()));
        }

        [Fact]
        public void A_Weak_Claim_Against_An_Adult_Man_Is_Too_Weak()
        {
            Assert.Equal(ClaimRejection.TooWeak, UseCase.Execute(Feud() with { Strength = ClaimStrength.Weak }));
        }

        [Fact]
        public void The_Player_Clan_Is_Never_Dragged_In()
        {
            Assert.Equal(ClaimRejection.PlayerClanInvolved, UseCase.Execute(Feud(defender: "player_clan")));
        }

        [Fact]
        public void The_Royal_House_Cannot_Start_A_Private_War()
        {
            Assert.Equal(ClaimRejection.RoyalHouseInvolved, UseCase.Execute(Feud(claimant: "clan_king")));
        }

        [Fact]
        public void No_House_Can_Start_A_Private_War_Against_The_Royal_House()
        {
            Assert.Equal(ClaimRejection.RoyalHouseInvolved, UseCase.Execute(Feud(defender: "clan_king")));
        }

        [Fact]
        public void A_Royal_Pretender_Cannot_Feud_With_The_Crown()
        {
            Assert.Equal(ClaimRejection.RoyalHouseInvolved,
                UseCase.Execute(Feud("clan_king", "clan_king") with { HasPretender = true }));
        }

        [Fact]
        public void A_Claim_On_The_Own_Dignity_Needs_A_Pretender()
        {
            Assert.Equal(ClaimRejection.OwnDignityWithoutPretender, UseCase.Execute(Feud("clan_a", "clan_a")));
            Assert.Null(UseCase.Execute(Feud("clan_a", "clan_a") with { HasPretender = true }));
        }

        [Fact]
        public void Houses_Of_Different_Realms_Do_Not_Feud()
        {
            Assert.Equal(ClaimRejection.NotSameRealm, UseCase.Execute(Feud() with { DefenderKingdomId = "scotland" }));
            Assert.Equal(ClaimRejection.NotSameRealm, UseCase.Execute(Feud() with { ClaimantKingdomId = null, DefenderKingdomId = null }));
        }

        [Fact]
        public void A_House_Already_At_Private_War_Waits()
        {
            Assert.Equal(ClaimRejection.AlreadyAtPrivateWar, UseCase.Execute(Feud() with { ClaimantAtPrivateWar = true }));
        }
    }
}
