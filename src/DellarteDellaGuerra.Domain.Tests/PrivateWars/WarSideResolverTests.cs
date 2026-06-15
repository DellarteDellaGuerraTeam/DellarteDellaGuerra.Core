using System.Collections.Generic;
using DellarteDellaGuerra.Domain.PrivateWars;
using DellarteDellaGuerra.Domain.PrivateWars.Model;

namespace DellarteDellaGuerra.Domain.Tests.PrivateWars
{
    public class WarSideResolverTests
    {
        private readonly WarSideResolver _resolver = new();

        [Fact]
        public void ResolveSide_UnrelatedSubtrees_ResolveToTheirPrincipal()
        {
            var war = PrivateWarTestData.War(attacker: "A", defender: "D");
            // A_vassal -> A ; D_vassal -> D ; X is uninvolved.
            var suzerain = Suzerain(("A_vassal", "A"), ("D_vassal", "D"));

            Assert.Equal(WarSide.Attacker, _resolver.ResolveSide("A", war, suzerain));
            Assert.Equal(WarSide.Attacker, _resolver.ResolveSide("A_vassal", war, suzerain));
            Assert.Equal(WarSide.Defender, _resolver.ResolveSide("D", war, suzerain));
            Assert.Equal(WarSide.Defender, _resolver.ResolveSide("D_vassal", war, suzerain));
            Assert.Null(_resolver.ResolveSide("X", war, suzerain));
        }

        [Fact]
        public void ResolveSide_AttackerIsDefendersVassal_NearestBelligerentAncestorWins()
        {
            // Hierarchy: A is a vassal of D, and A presses a claim against its own liege D.
            // A's own subtree must follow A (the nearer principal), not D.
            var war = PrivateWarTestData.War(attacker: "A", defender: "D");
            var suzerain = Suzerain(
                ("A", "D"),
                ("A_vassal", "A"),
                ("D_other_vassal", "D"));

            Assert.Equal(WarSide.Attacker, _resolver.ResolveSide("A", war, suzerain));
            Assert.Equal(WarSide.Attacker, _resolver.ResolveSide("A_vassal", war, suzerain));
            Assert.Equal(WarSide.Defender, _resolver.ResolveSide("D", war, suzerain));
            Assert.Equal(WarSide.Defender, _resolver.ResolveSide("D_other_vassal", war, suzerain));
        }

        [Fact]
        public void ResolveSide_CyclicChain_TerminatesAndReturnsNull()
        {
            var war = PrivateWarTestData.War(attacker: "A", defender: "D");
            var suzerain = Suzerain(("P", "Q"), ("Q", "P")); // neither reaches a principal

            Assert.Null(_resolver.ResolveSide("P", war, suzerain));
        }

        private static System.Func<string, string?> Suzerain(params (string clan, string suzerain)[] links)
        {
            var map = new Dictionary<string, string>();
            foreach (var (clan, suzerain) in links) map[clan] = suzerain;
            return clan => map.TryGetValue(clan, out var s) ? s : null;
        }
    }
}
