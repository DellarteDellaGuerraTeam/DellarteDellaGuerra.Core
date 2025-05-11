using DellarteDellaGuerra.Firearm.Reload;

public interface IReloadPhase : ITickable
{
    void OnReloadStart();
    void OnReloadProgress(float progress);
    void OnReloadEnd();

    float ReloadingProgressStart { get; }
    float ReloadingProgressEnd { get; }
}