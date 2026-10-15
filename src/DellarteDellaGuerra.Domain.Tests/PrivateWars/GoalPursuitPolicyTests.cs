using Bannerlord.PrivateWars.Domain;
using Bannerlord.PrivateWars.Domain.Model;

namespace DellarteDellaGuerra.Domain.Tests.PrivateWars
{
    public class GoalPursuitPolicyTests
    {
        [Theory]
        [InlineData(WarSide.Attacker, WarSide.Defender)]
        [InlineData(WarSide.Defender, WarSide.Attacker)]
        public void ShouldBesiegeGoal_OpposingSideHoldsIt_True(WarSide mySide, WarSide ownerSide)
        {
            Assert.True(PrivateWarGoalPursuitPolicy.ShouldBesiegeGoal(mySide, ownerSide, atWarWithOwner: false));
        }

        [Theory]
        [InlineData(WarSide.Attacker)]
        [InlineData(WarSide.Defender)]
        public void ShouldBesiegeGoal_OwnSideHoldsIt_False(WarSide side)
        {
            Assert.False(PrivateWarGoalPursuitPolicy.ShouldBesiegeGoal(side, side, atWarWithOwner: true));
        }

        [Theory]
        [InlineData(WarSide.Attacker)]
        [InlineData(WarSide.Defender)]
        public void ShouldBesiegeGoal_OutsiderAtWarWithMyRealmHoldsIt_True(WarSide side)
        {
            Assert.True(PrivateWarGoalPursuitPolicy.ShouldBesiegeGoal(side, ownerSide: null, atWarWithOwner: true));
        }

        [Theory]
        [InlineData(WarSide.Attacker)]
        [InlineData(WarSide.Defender)]
        public void ShouldBesiegeGoal_OutsiderNotAtWarHoldsIt_False(WarSide side)
        {
            // A clan of the same realm on neither side of the feud, or a realm at peace with mine.
            Assert.False(PrivateWarGoalPursuitPolicy.ShouldBesiegeGoal(side, ownerSide: null, atWarWithOwner: false));
        }
    }
}
