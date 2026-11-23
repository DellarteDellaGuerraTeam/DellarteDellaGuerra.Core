using DellarteDellaGuerra.Domain.Titles;
using DellarteDellaGuerra.Domain.Titles.Model;

namespace DellarteDellaGuerra.Domain.Tests.Titles
{
    public class SolicitSupportUseCaseTests
    {
        private static readonly SolicitSupportUseCase UseCase = new();

        [Fact]
        public void Execute_EnlistsNobody_WhenAClanFavoursNeitherPrincipal()
        {
            var decision = UseCase.Execute(Opportunity(), new[] { Candidate() });

            Assert.Empty(decision.AttackerSupporters);
            Assert.Empty(decision.DefenderSupporters);
        }

        [Fact]
        public void Execute_RalliesAnUncommittedClan_ToWhicheverPrincipalItPrefers()
        {
            var decision = UseCase.Execute(Opportunity(), new[]
            {
                Candidate("friend_of_the_claimant", relationToClaimant: 50f),
                Candidate("friend_of_the_holder", relationToHolder: 50f)
            });

            Assert.Equal(new[] { "friend_of_the_claimant" }, decision.AttackerSupporters);
            Assert.Equal(new[] { "friend_of_the_holder" }, decision.DefenderSupporters);
        }

        [Fact]
        public void Execute_DefectsTheHoldersVassal_WhenItHatesHimAndFavoursTheClaimant()
        {
            var vassal = Candidate("vassal", FeudalAllegiance.Holder, relationToClaimant: 60f, relationToHolder: -60f);

            var decision = UseCase.Execute(Opportunity(), new[] { vassal });

            Assert.Equal(new[] { "vassal" }, decision.AttackerSupporters);
        }

        [Fact]
        public void Execute_KeepsTheHoldersVassal_WhenItMerelyPrefersTheClaimant()
        {
            var vassal = Candidate("vassal", FeudalAllegiance.Holder, relationToClaimant: 50f);

            var decision = UseCase.Execute(Opportunity(), new[] { vassal });

            Assert.Empty(decision.AttackerSupporters);
            Assert.Empty(decision.DefenderSupporters);
        }

        [Fact]
        public void Execute_NeverNamesALoyalVassal_AsADefenderSupporter()
        {
            var vassal = Candidate("vassal", FeudalAllegiance.Holder, relationToClaimant: -100f, relationToHolder: 100f);

            var decision = UseCase.Execute(Opportunity(), new[] { vassal });

            Assert.Empty(decision.AttackerSupporters);
            Assert.Empty(decision.DefenderSupporters);
        }

        [Fact]
        public void Execute_NeverNamesAClan_TheHierarchyAlreadyMustersForTheClaimant()
        {
            var vassal = Candidate("vassal", FeudalAllegiance.Claimant, relationToClaimant: 100f);

            var decision = UseCase.Execute(Opportunity(), new[] { vassal });

            Assert.Empty(decision.AttackerSupporters);
            Assert.Empty(decision.DefenderSupporters);
        }

        [Fact]
        public void Execute_KeepsTheClaimantsVassal_WhenItMerelyPrefersTheHolder()
        {
            var vassal = Candidate("vassal", FeudalAllegiance.Claimant, relationToHolder: 50f);

            var decision = UseCase.Execute(Opportunity(), new[] { vassal });

            Assert.Empty(decision.DefenderSupporters);
        }

        [Fact]
        public void Execute_DefectsTheClaimantsVassal_WhenItHatesHimAndFavoursTheHolder()
        {
            var vassal = Candidate("vassal", FeudalAllegiance.Claimant, relationToClaimant: -60f, relationToHolder: 60f);

            var decision = UseCase.Execute(Opportunity(), new[] { vassal });

            Assert.Equal(new[] { "vassal" }, decision.DefenderSupporters);
        }

        [Fact]
        public void Execute_CannotDefectAVassal_OnLegitimacyAndBandwagonAlone()
        {
            var opportunity = Opportunity(ClaimStrength.DeJure, attackerStrength: 100f, defenderStrength: 0f);

            var decision = UseCase.Execute(opportunity, new[] { Candidate(allegiance: FeudalAllegiance.Holder) });

            Assert.Empty(decision.AttackerSupporters);
        }

        [Fact]
        public void Execute_RalliesAWaveringClan_OnceTheClaimantsSideIsWinning()
        {
            var candidates = new[] { Candidate(relationToClaimant: 30f) };

            Assert.Empty(UseCase.Execute(Opportunity(), candidates).AttackerSupporters);
            Assert.Equal(
                new[] { "candidate" },
                UseCase.Execute(Opportunity(attackerStrength: 300f), candidates).AttackerSupporters);
        }

        [Fact]
        public void Execute_EnlistsNobody_WhenNeitherSideFieldsAnything()
        {
            var opportunity = Opportunity(attackerStrength: 0f, defenderStrength: 0f);

            var decision = UseCase.Execute(opportunity, new[] { Candidate() });

            Assert.Empty(decision.AttackerSupporters);
            Assert.Empty(decision.DefenderSupporters);
        }

        private static ClaimOpportunity Opportunity(
            ClaimStrength strength = ClaimStrength.Weak,
            float attackerStrength = 100f,
            float defenderStrength = 100f) =>
            new("title", "claimant_clan", "holder_clan", strength, attackerStrength, defenderStrength, false, 0f);

        private static SupportCandidate Candidate(
            string clanId = "candidate",
            FeudalAllegiance allegiance = FeudalAllegiance.Uncommitted,
            float relationToClaimant = 0f,
            float relationToHolder = 0f) =>
            new(clanId, allegiance, relationToClaimant, relationToHolder);
    }
}
