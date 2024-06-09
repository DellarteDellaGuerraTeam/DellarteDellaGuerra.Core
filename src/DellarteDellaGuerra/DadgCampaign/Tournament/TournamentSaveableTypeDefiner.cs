using TaleWorlds.SaveSystem;

namespace DellarteDellaGuerra.DadgCampaign.Tournament
{
    public class TournamentSaveableTypeDefiner : SaveableTypeDefiner
    {
        public TournamentSaveableTypeDefiner()
            : base(67705654)
        {
        }

        protected override void DefineClassTypes()
	    {
		    AddClassDefinition(typeof(TourneyTournament), 3);
	    }
    }
}
