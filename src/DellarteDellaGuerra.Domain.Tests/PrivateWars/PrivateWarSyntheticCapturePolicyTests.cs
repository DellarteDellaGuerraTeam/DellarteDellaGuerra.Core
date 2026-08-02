using Bannerlord.PrivateWars.Domain;
using Bannerlord.PrivateWars.Domain.Siege;

namespace DellarteDellaGuerra.Domain.Tests.PrivateWars
{
    public class PrivateWarSyntheticCapturePolicyTests
    {
        private readonly SyntheticCapturePolicy _policy = new();

        [Fact]
        public void GetLosingSide_ApprovesAttackerCaptureOfDefenderHeldFrozenGoal()
        {
            var war = PrivateWarTestData.War(mainGoal: "goal");

            var losingSide = _policy.GetLosingSide(
                war, "goal", preparationComplete: true, isPlayerLed: false,
                hasUsableSiegeLeader: true, WarSide.Attacker, WarSide.Defender);

            Assert.Equal(WarSide.Defender, losingSide);
        }

        [Fact]
        public void GetLosingSide_ApprovesDefenderReclaimOfAttackerHeldFrozenGoal()
        {
            var war = PrivateWarTestData.War(mainGoal: "goal");

            var losingSide = _policy.GetLosingSide(
                war, "goal", preparationComplete: true, isPlayerLed: false,
                hasUsableSiegeLeader: true, WarSide.Defender, WarSide.Attacker);

            Assert.Equal(WarSide.Attacker, losingSide);
        }

        [Theory]
        [InlineData(false, true, false)]
        [InlineData(true, false, false)]
        [InlineData(true, true, true)]
        public void GetLosingSide_RejectsIncompletePlayerLedOrUnusableCapture(
            bool preparationComplete,
            bool hasUsableSiegeLeader,
            bool isPlayerLed)
        {
            var war = PrivateWarTestData.War(mainGoal: "goal");

            var losingSide = _policy.GetLosingSide(
                war, "goal", preparationComplete, isPlayerLed,
                hasUsableSiegeLeader, WarSide.Attacker, WarSide.Defender);

            Assert.Null(losingSide);
        }

        [Fact]
        public void GetLosingSide_RejectsInactiveWarOrDifferentSettlement()
        {
            var inactive = PrivateWarTestData.War(mainGoal: "goal") with
            {
                Status = PrivateWarStatus.Concluded
            };

            Assert.Null(_policy.GetLosingSide(
                inactive, "goal", true, false, true, WarSide.Attacker, WarSide.Defender));
            Assert.Null(_policy.GetLosingSide(
                PrivateWarTestData.War(mainGoal: "goal"), "other", true, false, true,
                WarSide.Attacker, WarSide.Defender));
        }

        [Theory]
        [InlineData(null, WarSide.Defender)]
        [InlineData(WarSide.Attacker, null)]
        [InlineData(WarSide.Attacker, WarSide.Attacker)]
        public void GetLosingSide_RejectsUnresolvedNeutralOrSameSideOwnership(
            WarSide? besiegerSide,
            WarSide? ownerSide)
        {
            var losingSide = _policy.GetLosingSide(
                PrivateWarTestData.War(mainGoal: "goal"), "goal", true, false, true,
                besiegerSide, ownerSide);

            Assert.Null(losingSide);
        }
    }
}
