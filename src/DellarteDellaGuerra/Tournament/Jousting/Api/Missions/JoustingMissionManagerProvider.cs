namespace DellarteDellaGuerra.Tournament.Jousting.Api.Missions
{
    /// <summary>
    /// Holds the DI built <see cref="JoustingMissionManager"/> for JoustTournament, which Bannerlord
    /// deserialises without running its constructor and therefore cannot keep injected dependencies.
    /// The composition root pushes the instance in at start-up.
    /// </summary>
    public static class JoustingMissionManagerProvider
    {
        private static JoustingMissionManager _joustingMissionManager;

        public static JoustingMissionManager Instance => _joustingMissionManager;

        /// <summary>
        /// Initialises the dependency for the JoustTournament.
        /// </summary>
        /// <param name="joustingMissionManager"></param>
        public static void Init(JoustingMissionManager joustingMissionManager)
        {
            _joustingMissionManager = joustingMissionManager;
        }
    }
}
