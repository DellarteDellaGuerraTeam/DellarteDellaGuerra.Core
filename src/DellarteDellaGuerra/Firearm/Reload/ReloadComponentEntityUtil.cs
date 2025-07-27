using System;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.Firearm.Reload
{
    public class ReloadComponentEntityUtil : IDisposable
    {
        private readonly Agent _agent;
        private readonly IWeaponEntityRepository _weaponEntityRepository;
        private GameEntity? _emptyGameEntity;

        public ReloadComponentEntityUtil(Agent agent, IWeaponEntityRepository weaponEntityRepository)
        {
            _agent = agent;
            _weaponEntityRepository = weaponEntityRepository;
            weaponEntityRepository.Remove(agent.Index.ToString());
        }

        public void OnFirearmDropped(SpawnedItemEntity spawnedItemEntity)
        {
            WeaponEntity? weaponEntity = _weaponEntityRepository.GetWeaponEntity(_agent.Index.ToString());
            if (weaponEntity?.GameEntity == spawnedItemEntity.GameEntity) Dispose();
        }

        public void HideWieldedWeapon()
        {
            var equipmentIndex = GetFirearmEquipmentIndex();
            var originalWeaponEntity = _agent.GetWeaponEntityFromEquipmentSlot(equipmentIndex);
            WeaponEntity? weaponEntity = _weaponEntityRepository.GetWeaponEntity(_agent.Index.ToString());

            if (weaponEntity is null)
                _weaponEntityRepository.SaveWeaponEntity(
                    new WeaponEntity(originalWeaponEntity,
                        originalWeaponEntity.GetMetaMesh(0)), _agent.Index.ToString());

            originalWeaponEntity.RemoveMultiMesh(originalWeaponEntity.GetMetaMesh(0));
            RefreshAgentVisuals();
        }

        private EquipmentIndex GetFirearmEquipmentIndex()
        {
            EquipmentIndex equipmentIndex = EquipmentIndex.None;
            for (EquipmentIndex index = EquipmentIndex.WeaponItemBeginSlot;
                 index < EquipmentIndex.NumAllWeaponSlots;
                 index++)
                if (_agent.Equipment[index].CurrentUsageItem?.WeaponClass.Equals(WeaponClass.Musket) ?? false)
                    equipmentIndex = index;

            return equipmentIndex;
        }

        public void WieldOriginalWeapon()
        {
            ResetFirearmMetaMeshToWeaponEntity();
            RefreshAgentVisuals();
        }

        private void RefreshAgentVisuals()
        {
            if (_emptyGameEntity is null)
            {
                _emptyGameEntity = GameEntity.CreateEmpty(Mission.Current.Scene);
                _agent.AgentVisuals.AddChildEntity(_emptyGameEntity);
            }
            else
            {
                _agent.AgentVisuals.RemoveChildEntity(_emptyGameEntity, 14);
                _emptyGameEntity = null;
            }
        }

        private void ResetFirearmMetaMeshToWeaponEntity()
        {
            WeaponEntity? weaponEntity = _weaponEntityRepository.GetWeaponEntity(_agent.Index.ToString());

            if (weaponEntity is null || weaponEntity.GameEntity.GetMetaMesh(0) is not null) return;

            weaponEntity.GameEntity.AddMultiMesh(weaponEntity.MetaMesh);
        }

        public void Dispose()
        {
            ResetFirearmMetaMeshToWeaponEntity();
            _weaponEntityRepository.Remove(_agent.Index.ToString());
        }
    }
}