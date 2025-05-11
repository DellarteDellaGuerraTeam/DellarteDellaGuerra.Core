using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.Firearm.Reload
{
    public class ReloadStopComponent : IReloadPhase
    {
        private readonly Agent _agent;
        private readonly WeaponReloadPhaseComponent _weaponReloadPhaseComponent;

        public ReloadStopComponent(Agent agent)
        {
            _agent = agent;
            _weaponReloadPhaseComponent = new WeaponReloadPhaseComponent();
        }

        public void OnTick(float dt)
        {
            _weaponReloadPhaseComponent.OnTick(dt);
        }

        public void OnReloadStart()
        {
        }

        public void OnReloadProgress(float progress)
        {
        }

        public void OnReloadEnd()
        {
            WieldOriginalWeapon();
        }

        public float ReloadingProgressStart => _weaponReloadPhaseComponent.ReloadingProgressStart;
        public float ReloadingProgressEnd => _weaponReloadPhaseComponent.ReloadingProgressEnd;

        private void WieldOriginalWeapon()
        {
            var firearmEquipmentIndex = _agent.GetWieldedItemIndex(Agent.HandIndex.MainHand);
            var firearmWeapon = _agent.Equipment[firearmEquipmentIndex];

            _agent.EquipWeaponWithNewEntity(firearmEquipmentIndex, ref firearmWeapon);
            _agent.TryToWieldWeaponInSlot(firearmEquipmentIndex, Agent.WeaponWieldActionType.Instant, false);
        }
    }
}