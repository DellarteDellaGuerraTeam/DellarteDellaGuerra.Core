using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.Firearm.Reload
{
    public class ReloadStartComponent : IReloadPhase
    {
        private readonly IWeaponEntityRepository _weaponEntityRepository;
        private readonly Agent _agent;

        public ReloadStartComponent(Agent agent, IWeaponEntityRepository weaponEntityRepository)
        {
            _agent = agent;
            _weaponEntityRepository = weaponEntityRepository;
        }

        public void OnTick(float dt)
        {
        }

        public void OnReloadPhaseStart()
        {
            // HideWieldedWeapon();
        }

        public void OnReloadProgress(float progress)
        {
        }

        public void OnReloadPhaseEnd()
        {
        }

        public float PhaseProgressStart => 0;
        public float PhaseProgressEnd => 1f;

        private void HideWieldedWeapon()
        {
            var equipmentIndex = _agent.GetWieldedItemIndex(Agent.HandIndex.MainHand);
            var wieldedWeapon =
                _agent.GetWeaponEntityFromEquipmentSlot(equipmentIndex);

            var firearmWeapon = _agent.Equipment[equipmentIndex];

            GameEntity? gameEntity = _weaponEntityRepository.GetWeaponEntity(_agent.Index.ToString());
            if (gameEntity == null)
            {
                var metamesh = wieldedWeapon.GetMetaMesh(0);
                var missionWeapon = new MissionWeapon(firearmWeapon.Item, firearmWeapon.ItemModifier, null);
                gameEntity = Mission.Current.SpawnWeaponWithNewEntity(ref missionWeapon,
                    Mission.WeaponSpawnFlags.None,
                    metamesh.Frame);
                _weaponEntityRepository.SaveWeaponEntity(gameEntity, _agent.Index.ToString());

                metamesh.ClearMeshes();
            }

            _agent.ClearAttachedWeapons();
            gameEntity.SetVisibilityExcludeParents(false);
            gameEntity.UpdateVisibilityMask();
        }
    }
}