namespace DellarteDellaGuerra.Firearm.Reload
{
    public interface IReloadPhase : ITickable, IOnAgentBuild, IOnAgentRemoved
    {
        void OnReloadPhaseStart();
        void OnReloadProgress(float progress);
        void OnReloadPhaseEnd();

        float PhaseProgressStart { get; }
        float PhaseProgressEnd { get; }
    }
}