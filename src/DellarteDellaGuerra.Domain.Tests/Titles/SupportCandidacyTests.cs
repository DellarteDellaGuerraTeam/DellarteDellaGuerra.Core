using DellarteDellaGuerra.Domain.Titles;
using DellarteDellaGuerra.Domain.Titles.Model;

namespace DellarteDellaGuerra.Domain.Tests.Titles
{
    public class SupportCandidacyTests
    {
        [Fact]
        public void Select_AsksAVassalOfEitherPrincipalFromOutsideTheRealm()
        {
            string? GetSuzerain(string clanId) => clanId switch
            {
                "foreign_vassal_of_attacker" => "attacker",
                "foreign_vassal_of_defender" => "defender",
                _ => null
            };

            var candidates = SupportCandidacy.Select(
                new[] { "foreign_vassal_of_attacker", "foreign_vassal_of_defender" },
                _ => false,
                GetSuzerain,
                "attacker",
                "defender").ToList();

            Assert.Equal(
                new[]
                {
                    ("foreign_vassal_of_attacker", FeudalAllegiance.Claimant),
                    ("foreign_vassal_of_defender", FeudalAllegiance.Holder)
                },
                candidates);
        }

        [Fact]
        public void Select_AsksAnUncommittedHouseOnlyFromInsideTheRealm()
        {
            var candidates = SupportCandidacy.Select(
                new[] { "neighbour", "stranger" },
                clanId => clanId == "neighbour",
                _ => null,
                "attacker",
                "defender").ToList();

            Assert.Equal(new[] { ("neighbour", FeudalAllegiance.Uncommitted) }, candidates);
        }

        [Fact]
        public void Select_PutsAClanWithTheNearestBelligerentAncestorLikeWarSideStrength()
        {
            string? GetSuzerain(string clanId) => clanId switch
            {
                "clan" => "attacker",
                "attacker" => "defender",
                _ => null
            };

            var candidates = SupportCandidacy.Select(new[] { "clan" }, _ => true, GetSuzerain, "attacker", "defender");

            Assert.Equal(new[] { ("clan", FeudalAllegiance.Claimant) }, candidates);
        }

        [Fact]
        public void Select_CountsNobodyForAnUnfoundedCadetBranch()
        {
            // An internal war's claimant side is a house that does not exist yet, so nobody can be
            // mustered for it by the chain; the defender's vassals still owe him.
            string? GetSuzerain(string clanId) => clanId == "vassal" ? "defender" : null;

            var candidates = SupportCandidacy.Select(
                new[] { "vassal", "neighbour" },
                _ => true,
                GetSuzerain,
                "",
                "defender").ToList();

            Assert.Equal(
                new[] { ("vassal", FeudalAllegiance.Holder), ("neighbour", FeudalAllegiance.Uncommitted) },
                candidates);
        }
    }
}
