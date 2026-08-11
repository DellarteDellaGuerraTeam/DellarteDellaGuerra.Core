using System;
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
            var resolver = new WarSideResolver();
            var getSuzerain = Suzerain(("A_vassal", "A"), ("D_vassal", "D"));

            Assert.Equal(WarSide.Attacker, resolver.ResolveSide("A", war, getSuzerain));
            Assert.Equal(WarSide.Attacker, resolver.ResolveSide("A_vassal", war, getSuzerain));
            Assert.Equal(WarSide.Defender, resolver.ResolveSide("D", war, getSuzerain));
            Assert.Equal(WarSide.Defender, resolver.ResolveSide("D_vassal", war, getSuzerain));
            Assert.Null(resolver.ResolveSide("X", war, getSuzerain));
        }

        [Fact]
        public void ResolveSide_AttackerIsDefendersVassal_NearestBelligerentAncestorWins()
        {
            // Hierarchy: A is a vassal of D, and A presses a claim against its own liege D.
            // A's own subtree must follow A (the nearer principal), not D.
            var war = PrivateWarTestData.War(attacker: "A", defender: "D");
            var resolver = new WarSideResolver();
            var getSuzerain = Suzerain(
                ("A", "D"),
                ("A_vassal", "A"),
                ("D_other_vassal", "D"));

            Assert.Equal(WarSide.Attacker, resolver.ResolveSide("A", war, getSuzerain));
            Assert.Equal(WarSide.Attacker, resolver.ResolveSide("A_vassal", war, getSuzerain));
            Assert.Equal(WarSide.Defender, resolver.ResolveSide("D", war, getSuzerain));
            Assert.Equal(WarSide.Defender, resolver.ResolveSide("D_other_vassal", war, getSuzerain));
        }

        [Fact]
        public void ResolveSide_CyclicChain_TerminatesAndReturnsNull()
        {
            var war = PrivateWarTestData.War(attacker: "A", defender: "D");
            var resolver = new WarSideResolver();
            var getSuzerain = Suzerain(("P", "Q"), ("Q", "P")); // neither reaches a principal

            Assert.Null(resolver.ResolveSide("P", war, getSuzerain));
        }

        private static Func<string, string?> Suzerain(params (string clan, string suzerain)[] links)
            => new MapSuzerainProvider(links).GetSuzerain;
    }
}
