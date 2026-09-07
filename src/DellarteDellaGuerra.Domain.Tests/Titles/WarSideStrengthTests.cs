using DellarteDellaGuerra.Domain.Titles;

namespace DellarteDellaGuerra.Domain.Tests.Titles
{
    public class WarSideStrengthTests
    {
        private static readonly string[] None = Array.Empty<string>();

        [Fact]
        public void Sum_CountsEachPrincipalsOwnStrength()
        {
            var strengths = new Dictionary<string, float> { ["attacker"] = 100f, ["defender"] = 50f };

            var (attacker, defender) = WarSideStrength.Sum(strengths, _ => null, "attacker", "defender", None, None);

            Assert.Equal(100f, attacker);
            Assert.Equal(50f, defender);
        }

        [Fact]
        public void Sum_CountsAVassalOfTheAttackerForTheAttacker()
        {
            var strengths = new Dictionary<string, float> { ["attacker"] = 100f, ["vassal"] = 30f, ["defender"] = 50f };
            string? GetSuzerain(string clanId) => clanId == "vassal" ? "attacker" : null;

            var (attacker, defender) = WarSideStrength.Sum(strengths, GetSuzerain, "attacker", "defender", None, None);

            Assert.Equal(130f, attacker);
            Assert.Equal(50f, defender);
        }

        [Fact]
        public void Sum_AssignsAClanToTheNearestBelligerentAncestor()
        {
            var strengths = new Dictionary<string, float> { ["clan"] = 20f, ["attacker"] = 100f, ["defender"] = 50f };
            string? GetSuzerain(string clanId) => clanId switch
            {
                "clan" => "attacker",
                "attacker" => "defender",
                _ => null
            };

            var (attacker, defender) = WarSideStrength.Sum(strengths, GetSuzerain, "attacker", "defender", None, None);

            Assert.Equal(120f, attacker);
            Assert.Equal(50f, defender);
        }

        [Fact]
        public void Sum_CountsAClanUnderNeitherSideForNeither()
        {
            var strengths = new Dictionary<string, float> { ["attacker"] = 100f, ["defender"] = 50f, ["neutral"] = 40f };

            var (attacker, defender) = WarSideStrength.Sum(strengths, _ => null, "attacker", "defender", None, None);

            Assert.Equal(100f, attacker);
            Assert.Equal(50f, defender);
        }

        [Fact]
        public void Sum_TerminatesAndContributesNothing_ForASuzerainCycle()
        {
            var strengths = new Dictionary<string, float>
            {
                ["attacker"] = 100f, ["defender"] = 50f, ["a"] = 10f, ["b"] = 20f
            };
            string? GetSuzerain(string clanId) => clanId switch
            {
                "a" => "b",
                "b" => "a",
                _ => null
            };

            var (attacker, defender) = WarSideStrength.Sum(strengths, GetSuzerain, "attacker", "defender", None, None);

            Assert.Equal(100f, attacker);
            Assert.Equal(50f, defender);
        }

        [Fact]
        public void Sum_MovesADefectingVassalAndItsSubtreeToTheAttacker()
        {
            var strengths = new Dictionary<string, float>
            {
                ["attacker"] = 100f, ["defender"] = 50f, ["vassal"] = 30f, ["subvassal"] = 10f
            };
            string? GetSuzerain(string clanId) => clanId switch
            {
                "vassal" => "defender",
                "subvassal" => "vassal",
                _ => null
            };

            var (attacker, defender) = WarSideStrength.Sum(
                strengths, GetSuzerain, "attacker", "defender", new[] { "vassal" }, None);

            Assert.Equal(140f, attacker);
            Assert.Equal(50f, defender);
        }

        [Fact]
        public void Sum_CountsARalliedNeutralAndItsSubtreeForTheSideItPledgedTo()
        {
            var strengths = new Dictionary<string, float>
            {
                ["attacker"] = 100f, ["defender"] = 50f, ["neutral"] = 40f, ["its_vassal"] = 5f
            };
            string? GetSuzerain(string clanId) => clanId == "its_vassal" ? "neutral" : null;

            var (attacker, defender) = WarSideStrength.Sum(
                strengths, GetSuzerain, "attacker", "defender", None, new[] { "neutral" });

            Assert.Equal(100f, attacker);
            Assert.Equal(95f, defender);
        }
    }
}
