using System;
using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Firearm.Reload.DellarteDellaGuerra.Firearm.Reload;

namespace DellarteDellaGuerra.Firearm.Reload
{
    public class WeaponReloadPhaseComponent : IReloadPhase
    {
        private readonly List<Func<BoneAttachedItem>> _weaponVisualCreators;
        private List<BoneAttachedItem> _visualWeapons = new();

        public WeaponReloadPhaseComponent(params Func<BoneAttachedItem>[] createVisual)
        {
            _weaponVisualCreators = createVisual.ToList();
        }

        public void OnReloadPhaseStart()
        {
        }

        public void OnReloadProgress(float progress)
        {
            _visualWeapons.ForEach(visualWeapon => visualWeapon.InitialiseAtProgress(progress));
        }

        public void OnReloadPhaseEnd()
        {
            _visualWeapons.ForEach(visualWeapon => visualWeapon.Remove());
        }

        public void OnTick(float dt)
        {
            _visualWeapons.ForEach(visualWeapon => visualWeapon.OnTick(dt));
        }

        public void OnAgentBuild()
        {
            _visualWeapons = _weaponVisualCreators.Select(weaponVisualCreator => weaponVisualCreator.Invoke())
                .ToList();
            _visualWeapons.ForEach(visualWeapon => visualWeapon.OnAgentBuild());
        }
        
        public float PhaseProgressStart => 0f;
        public float PhaseProgressEnd => 1f;
    }
}