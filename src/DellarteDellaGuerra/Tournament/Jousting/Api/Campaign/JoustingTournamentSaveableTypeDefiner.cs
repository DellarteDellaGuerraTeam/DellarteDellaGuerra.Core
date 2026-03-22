using TaleWorlds.SaveSystem;

namespace DellarteDellaGuerra.Tournament.Jousting.Api.Campaign
{
    public class JoustingTournamentSaveableTypeDefiner : SaveableTypeDefiner
    {
        public JoustingTournamentSaveableTypeDefiner()
            : base(674592155)
        {
        }

        protected override void DefineClassTypes()
        {
            AddClassDefinition(typeof(JoustTournament), 3);
        }
    }
}