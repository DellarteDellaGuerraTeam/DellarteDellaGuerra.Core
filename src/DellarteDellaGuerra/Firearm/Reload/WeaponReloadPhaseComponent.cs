using System;
using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Firearm.Reload.DellarteDellaGuerra.Firearm.Reload;
using TaleWorlds.Core;

namespace DellarteDellaGuerra.Firearm.Reload
{
    public class WeaponReloadPhaseComponent : IReloadPhase
    {
        private readonly List<Func<BoneAttachedItem>> _weaponVisualCreators;
        private List<BoneAttachedItem> _visualWeapons = new();
        private float _progress;

        public WeaponReloadPhaseComponent(params Func<BoneAttachedItem>[] createVisual)
        {
            _weaponVisualCreators = createVisual.ToList();
        }

        public void OnReloadPhaseStart()
        {
        }

        public void OnReloadProgress(float progress)
        {
            _progress = progress;

            if (_visualWeapons.IsEmpty())
                _visualWeapons = _weaponVisualCreators.Select(weaponVisualCreator => weaponVisualCreator.Invoke())
                    .ToList();

            _visualWeapons.ForEach(visualWeapon => visualWeapon.InitialiseAtProgress(_progress));
        }

        public void OnReloadPhaseEnd()
        {
            _visualWeapons.ForEach(visualWeapon => visualWeapon.Remove());
            _visualWeapons.RemoveAll(_ => true);
        }

        public void OnTick(float dt)
        {
            _visualWeapons.ForEach(visualWeapon => visualWeapon.OnTick(dt));
        }

        public float PhaseProgressStart => 0f;
        public float PhaseProgressEnd => 1f;
    }
}