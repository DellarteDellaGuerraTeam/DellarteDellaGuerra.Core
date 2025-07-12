using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.Firearm.Reload
{
    public class ReloadStartComponent : IReloadPhase
    {
        private readonly Agent _agent;

        public ReloadStartComponent(Agent agent)
        {
            _agent = agent;
        }

        public void OnTick(float dt)
        {
        }

        public void OnReloadPhaseStart()
        {
            HideWieldedWeapon();
        }

        public void OnReloadProgress(float progress)
        {
        }

        public void OnReloadPhaseEnd()
        {
        }

        public float PhaseProgressStart => 0;
        public float PhaseProgressEnd => 1;

        private void HideWieldedWeapon()
        {
            var wieldedWeapon =
                _agent.GetWeaponEntityFromEquipmentSlot(_agent.GetWieldedItemIndex(Agent.HandIndex.MainHand));
            wieldedWeapon.GetMetaMesh(0)?.ClearMeshes();
        }
    }
}