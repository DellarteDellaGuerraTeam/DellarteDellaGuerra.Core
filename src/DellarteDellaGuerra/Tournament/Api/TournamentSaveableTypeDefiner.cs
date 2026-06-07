using TaleWorlds.SaveSystem;

namespace DellarteDellaGuerra.Tournament.Api
{
    public class TournamentSaveableTypeDefiner : SaveableTypeDefiner
    {
        public TournamentSaveableTypeDefiner()
            : base(674592248)
        {
        }
        protected override void DefineClassTypes()
        {
            AddClassDefinition(typeof(DadgFightingTournament), 1);
            AddClassDefinition(typeof(SaveableTournamentReward), 2);
        }
    }
}