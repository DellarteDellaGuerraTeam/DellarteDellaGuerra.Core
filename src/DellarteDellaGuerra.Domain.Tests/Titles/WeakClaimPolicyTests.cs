using System;
using DellarteDellaGuerra.Domain.Titles;
using DellarteDellaGuerra.Domain.Titles.Model;
using Xunit;

namespace DellarteDellaGuerra.Domain.Tests.Titles
{
    public class WeakClaimPolicyTests
    {
        private static HeroNode Holder(bool isFemale, float age) =>
            new("holder", isFemale, true, "clan_a", Array.Empty<string>(), Age: age);

        [Fact]
        public void A_Weak_Claim_Is_Not_Pressable_Against_An_Adult_Man()
        {
            Assert.False(WeakClaimPolicy.IsPressable(ClaimStrength.Weak, Holder(false, 40f)));
        }

        [Fact]
        public void A_Weak_Claim_Is_Pressable_Against_A_Boy()
        {
            Assert.True(WeakClaimPolicy.IsPressable(ClaimStrength.Weak, Holder(false, 17.9f)));
        }

        [Fact]
        public void A_Weak_Claim_Is_Not_Pressable_Against_A_Man_Of_Exactly_Eighteen()
        {
            Assert.False(WeakClaimPolicy.IsPressable(ClaimStrength.Weak, Holder(false, 18f)));
        }

        [Fact]
        public void A_Weak_Claim_Is_Pressable_Against_A_Woman()
        {
            Assert.True(WeakClaimPolicy.IsPressable(ClaimStrength.Weak, Holder(true, 40f)));
        }

        [Fact]
        public void A_Weak_Claim_Is_Not_Pressable_Against_An_Unknown_Holder()
        {
            Assert.False(WeakClaimPolicy.IsPressable(ClaimStrength.Weak, null));
        }

        [Theory]
        [InlineData(ClaimStrength.Strong)]
        [InlineData(ClaimStrength.DeJure)]
        public void Stronger_Claims_Are_Pressable_Against_An_Adult_Man(ClaimStrength strength)
        {
            Assert.True(WeakClaimPolicy.IsPressable(strength, Holder(false, 40f)));
        }
    }
}
