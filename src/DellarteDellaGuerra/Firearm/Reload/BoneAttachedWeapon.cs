using System;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.Firearm.Reload
{
    public class BoneAttachedWeapon : ITickable
    {
        private readonly Agent _agent;
        private readonly Agent.HandIndex _handIndex;
        private readonly HumanBone _targetBone;
        private readonly Func<MatrixFrame, MatrixFrame> _weaponFrameTransformer;

        private GameEntity _attachedVisual;
        private GameEntity _mirrorVisual;

        private bool _isInitialised;
        private bool _isRemoved;

        public BoneAttachedWeapon(Agent agent, Agent.HandIndex handIndex, HumanBone targetBone,
            Func<MatrixFrame, MatrixFrame> weaponFrameTransformer)
        {
            _agent = agent;
            _handIndex = handIndex;
            _targetBone = targetBone;
            _weaponFrameTransformer = weaponFrameTransformer;
        }

        public void Initialise()
        {
            if (_isInitialised) return;

            var weapon = GetWieldedWeapon();
            var baseFrame = GetOriginalWeaponFrame(weapon);

            _attachedVisual = CreateVisualEntity(weapon, baseFrame);
            _mirrorVisual = CreateVisualEntity(weapon, baseFrame);

            AttachToBone(_attachedVisual, baseFrame);
            HideWieldedWeapon();
            HideWeaponVisual(_attachedVisual);

            _isInitialised = true;
        }

        public void OnTick(float dt)
        {
            if (!_isInitialised || _isRemoved) return;

            UpdateVisualFrame(_weaponFrameTransformer.Invoke(GetAttachedFrame()));
        }

        public void Remove()
        {
            if (_isRemoved) return;

            _attachedVisual.Remove(0);
            _mirrorVisual.Remove(0);
            _isRemoved = true;
        }

        private MissionWeapon GetWieldedWeapon()
        {
            return _handIndex == Agent.HandIndex.MainHand ? _agent.WieldedWeapon : _agent.WieldedOffhandWeapon;
        }

        private static MatrixFrame GetOriginalWeaponFrame(MissionWeapon weapon)
        {
            return weapon.GetWeaponData(false).WeaponFrame;
        }

        private static GameEntity CreateVisualEntity(MissionWeapon weapon, MatrixFrame frame)
        {
            return Mission.Current.SpawnWeaponWithNewEntity(ref weapon, Mission.WeaponSpawnFlags.None, frame);
        }

        private MatrixFrame GetAttachedFrame()
        {
            return _attachedVisual.GetMetaMesh(0).Frame;
        }

        private void UpdateVisualFrame(MatrixFrame newFrame)
        {
            _mirrorVisual.SetGlobalFrame(_attachedVisual.GetGlobalFrame());
            _mirrorVisual.GetMetaMesh(0).Frame = newFrame;
        }

        private void AttachToBone(GameEntity entity, MatrixFrame frame)
        {
            _agent.AttachWeaponToBone(_agent.WieldedWeapon, entity, (sbyte)_targetBone, ref frame);
        }

        private void HideWieldedWeapon()
        {
            var wieldedWeapon = _agent.GetWeaponEntityFromEquipmentSlot(_agent.GetWieldedItemIndex(_handIndex));
            wieldedWeapon.GetMetaMesh(0)?.ClearMeshes();
        }

        private static void HideWeaponVisual(GameEntity weaponEntity)
        {
            weaponEntity.GetMetaMesh(0)?.ClearMeshes();
        }
    }
}