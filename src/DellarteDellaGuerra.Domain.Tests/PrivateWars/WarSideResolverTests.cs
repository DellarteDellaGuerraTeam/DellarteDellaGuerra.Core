using Bannerlord.PrivateWars.Domain;
using Bannerlord.PrivateWars.Domain.Model;

namespace DellarteDellaGuerra.Domain.Tests.PrivateWars
{
    public class WarSideResolverTests
    {
        [Fact]
        public void ResolveSide_UnrelatedSubtrees_ResolveToTheirPrincipal()
        {
            var war = PrivateWarTestData.War(attacker: "A", defender: "D");
            // A_vassal -> A ; D_vassal -> D ; X is uninvolved.
            var resolver = Resolver(("A_vassal", "A"), ("D_vassal", "D"));

            Assert.Equal(WarSide.Attacker, resolver.ResolveSide("A", war));
            Assert.Equal(WarSide.Attacker, resolver.ResolveSide("A_vassal", war));
            Assert.Equal(WarSide.Defender, resolver.ResolveSide("D", war));
            Assert.Equal(WarSide.Defender, resolver.ResolveSide("D_vassal", war));
            Assert.Null(resolver.ResolveSide("X", war));
        }

        [Fact]
        public void ResolveSide_AttackerIsDefendersVassal_NearestBelligerentAncestorWins()
        {
            // Hierarchy: A is a vassal of D, and A presses a claim against its own liege D.
            // A's own subtree must follow A (the nearer principal), not D.
            var war = PrivateWarTestData.War(attacker: "A", defender: "D");
            var resolver = Resolver(
                ("A", "D"),
                ("A_vassal", "A"),
                ("D_other_vassal", "D"));

            Assert.Equal(WarSide.Attacker, resolver.ResolveSide("A", war));
            Assert.Equal(WarSide.Attacker, resolver.ResolveSide("A_vassal", war));
            Assert.Equal(WarSide.Defender, resolver.ResolveSide("D", war));
            Assert.Equal(WarSide.Defender, resolver.ResolveSide("D_other_vassal", war));
        }

        [Fact]
        public void ResolveSide_CyclicChain_TerminatesAndReturnsNull()
        {
            var war = PrivateWarTestData.War(attacker: "A", defender: "D");
            var resolver = Resolver(("P", "Q"), ("Q", "P")); // neither reaches a principal

            Assert.Null(resolver.ResolveSide("P", war));
        }

        private static WarSideResolver Resolver(params (string clan, string suzerain)[] links)
            => new(new MapSuzerainProvider(links));
    }
}
