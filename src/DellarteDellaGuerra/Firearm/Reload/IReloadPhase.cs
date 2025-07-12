namespace DellarteDellaGuerra.Firearm.Reload
{
    public interface IReloadPhase : ITickable
    {
        void OnReloadPhaseStart();
        void OnReloadProgress(float progress);
        void OnReloadPhaseEnd();

        float PhaseProgressStart { get; }
        float PhaseProgressEnd { get; }
    }
}