using System;

namespace DellarteDellaGuerra.Firearm.Reload
{
    public class WeaponReloadPhaseComponent : IReloadPhase
    {
        private readonly Func<BoneAttachedWeapon>? _createVisual;
        private BoneAttachedWeapon? _visualWeapon;

        public WeaponReloadPhaseComponent(Func<BoneAttachedWeapon>? createVisual = null)
        {
            _createVisual = createVisual;
        }

        public void OnReloadStart()
        {
            _visualWeapon = _createVisual?.Invoke();
            _visualWeapon?.Initialise();
        }

        public void OnReloadProgress(float progress)
        {
        }

        public void OnReloadEnd()
        {
            _visualWeapon?.Remove();
        }

        public void OnTick(float dt)
        {
            _visualWeapon?.OnTick(dt);
        }

        public float ReloadingProgressStart => 0f;
        public float ReloadingProgressEnd => 1f;
    }
}