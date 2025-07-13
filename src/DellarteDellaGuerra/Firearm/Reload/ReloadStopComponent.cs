using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.Firearm.Reload
{
    public class ReloadStopComponent : IReloadPhase
    {
        private readonly Agent _agent;

        public ReloadStopComponent(Agent agent)
        {
            _agent = agent;
        }

        public void OnTick(float dt)
        {
        }

        public void OnReloadPhaseStart()
        {
        }

        public void OnReloadProgress(float progress)
        {
        }

        public void OnReloadPhaseEnd()
        {
            WieldOriginalWeapon();
        }

        public float PhaseProgressStart => 0;
        public float PhaseProgressEnd => 1;

        private void WieldOriginalWeapon()
        {
            var firearmEquipmentIndex = _agent.GetWieldedItemIndex(Agent.HandIndex.MainHand);
            var firearmWeapon = _agent.Equipment[firearmEquipmentIndex];

            if (firearmWeapon.CurrentUsageItem?.WeaponClass != WeaponClass.Musket) return;

            _agent.EquipWeaponWithNewEntity(firearmEquipmentIndex, ref firearmWeapon);
            _agent.TryToWieldWeaponInSlot(firearmEquipmentIndex, Agent.WeaponWieldActionType.Instant, false);
        }
    }
}