using TaleWorlds.SaveSystem;

namespace DellarteDellaGuerra.Titles.Api.Election
{
    public class FeudalElectionSaveDefiner : SaveableTypeDefiner
    {
        public FeudalElectionSaveDefiner()
            : base(2_887_350)
        {
        }

        protected override void DefineClassTypes()
        {
            AddClassDefinition(typeof(FeudalSettlementClaimantDecision), 1);
            AddClassDefinition(typeof(FeudalPetitionDecision), 2);
            AddClassDefinition(typeof(FeudalPetitionDecision.GrantClaimOutcome), 3);
            AddClassDefinition(typeof(FeudalPetitionDecision.DenyClaimOutcome), 4);
            AddClassDefinition(typeof(FeudalAttainderDecision), 5);
            AddClassDefinition(typeof(FeudalAttainderDecision.AttaintOutcome), 6);
            AddClassDefinition(typeof(FeudalAttainderDecision.UpholdOutcome), 7);
        }
    }
}
