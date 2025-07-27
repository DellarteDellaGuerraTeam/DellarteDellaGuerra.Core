using System;

namespace DellarteDellaGuerra.Firearm.Reload
{
    public interface IReloadPhase : ITickable, IOnAgentBuild, IDisposable
    {
        void OnReloadPhaseStart();
        void OnReloadProgress(float progress);
        void OnReloadPhaseEnd();

        float PhaseProgressStart { get; }
        float PhaseProgressEnd { get; }
    }
}