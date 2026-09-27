using DellarteDellaGuerra.Domain.Titles;
using Xunit;

namespace DellarteDellaGuerra.Domain.Tests.Titles
{
    public class PersonalBondPolicyTests
    {
        private readonly PersonalBondPolicy _policy = new(new FakeGenealogy()
            .AddHero("father", "clan_a", childIds: new[] { "son", "brother" })
            .AddHero("son", "clan_b", fatherId: "father")
            .AddHero("brother", "clan_c", fatherId: "father")
            .AddHero("stranger", "clan_d"));

        [Fact]
        public void A_Parent_Is_Bonded_To_His_Child()
        {
            Assert.True(_policy.IsBonded("father", "son", 0f));
        }

        [Fact]
        public void A_Child_Is_Bonded_To_His_Parent()
        {
            Assert.True(_policy.IsBonded("son", "father", 0f));
        }

        [Fact]
        public void Siblings_Are_Bonded()
        {
            Assert.True(_policy.IsBonded("son", "brother", 0f));
        }

        [Fact]
        public void A_Firm_Friend_Is_Bonded()
        {
            Assert.True(_policy.IsBonded("stranger", "son", 50f));
        }

        [Fact]
        public void A_Stranger_Short_Of_Friendship_Is_Not_Bonded()
        {
            Assert.False(_policy.IsBonded("stranger", "son", 49f));
        }

        [Fact]
        public void Two_Fatherless_Heroes_Are_Not_Siblings()
        {
            var policy = new PersonalBondPolicy(new FakeGenealogy()
                .AddHero("a", "clan_a")
                .AddHero("b", "clan_b"));

            Assert.False(policy.IsBonded("a", "b", 0f));
        }

        [Fact]
        public void An_Unknown_Hero_Is_Bonded_Only_By_Friendship()
        {
            Assert.False(_policy.IsBonded("ghost", "son", 0f));
            Assert.True(_policy.IsBonded("ghost", "son", 50f));
        }
    }
}
