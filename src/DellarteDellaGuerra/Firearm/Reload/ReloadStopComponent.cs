using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.Firearm.Reload
{
    public class ReloadStopComponent : IReloadPhase
    {
        private readonly IWeaponEntityRepository _weaponEntityRepository;
        private readonly Agent _agent;

        public ReloadStopComponent(Agent agent, IWeaponEntityRepository weaponEntityRepository)
        {
            _agent = agent;
            _weaponEntityRepository = weaponEntityRepository;
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
            // WieldOriginalWeapon();
        }

        public float PhaseProgressStart => 0f;
        public float PhaseProgressEnd => 1;

        private void WieldOriginalWeapon()
        {
            var firearmEquipmentIndex = _agent.GetWieldedItemIndex(Agent.HandIndex.MainHand);

            var firearmWeapon = _agent.Equipment[firearmEquipmentIndex];
            if (firearmWeapon.CurrentUsageItem?.WeaponClass != WeaponClass.Musket) return;

            GameEntity? weaponEntity = _weaponEntityRepository.GetWeaponEntity(_agent.Index.ToString());

            weaponEntity.SetVisibilityExcludeParents(true);
            weaponEntity.UpdateVisibilityMask();

            var frame = MatrixFrame.Identity;
            _agent.AttachWeaponToWeapon(firearmEquipmentIndex,
                new MissionWeapon(firearmWeapon.Item, firearmWeapon.ItemModifier, null),
                weaponEntity, ref frame);
        }
    }
}