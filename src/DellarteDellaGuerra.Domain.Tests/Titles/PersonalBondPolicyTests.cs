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
            .AddHero("wife", "clan_e", isFemale: true)
            .AddHero("stranger", "clan_d")
            .Married("son", "wife"));

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
        public void Spouses_Are_Bonded()
        {
            Assert.True(_policy.IsBonded("son", "wife", 0f));
            Assert.True(_policy.IsBonded("wife", "son", 0f));
        }

        [Fact]
        public void A_Father_In_Law_Is_Bonded_Both_Ways()
        {
            Assert.True(_policy.IsBonded("wife", "father", 0f));
            Assert.True(_policy.IsBonded("father", "wife", 0f));
        }

        [Fact]
        public void A_Sibling_In_Law_Is_Bonded_Both_Ways()
        {
            Assert.True(_policy.IsBonded("wife", "brother", 0f));
            Assert.True(_policy.IsBonded("brother", "wife", 0f));
        }

        [Fact]
        public void A_Firm_Friend_Is_Bonded()
        {
            Assert.True(_policy.IsBonded("stranger", "son", 51f));
        }

        [Fact]
        public void A_Relation_Of_Exactly_50_Is_Not_Yet_Friendship()
        {
            Assert.False(_policy.IsBonded("stranger", "son", 50f));
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
            Assert.True(_policy.IsBonded("ghost", "son", 51f));
        }
    }
}
