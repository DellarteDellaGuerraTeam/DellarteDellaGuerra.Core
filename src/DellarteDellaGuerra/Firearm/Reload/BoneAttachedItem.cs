using System;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.Firearm.Reload
{
    namespace DellarteDellaGuerra.Firearm.Reload
    {
        public class BoneAttachedItem : ITickable
        {
            private readonly Agent _agent;
            private readonly HumanBone _targetBone;
            private readonly Func<MatrixFrame, MatrixFrame> _weaponFrameTransformer;
            private readonly ItemObject _itemObject;
            private readonly float _minimumProgress;

            private MetaMesh _attachedMetaMesh;
            private MetaMesh _mirrorMetaMesh;

            private GameEntity _attachedVisual;
            private GameEntity _mirrorVisual;

            private bool _isInitialised;
            private bool _isRemoved;

            public BoneAttachedItem(Agent agent, HumanBone targetBone,
                Func<MatrixFrame, MatrixFrame> weaponFrameTransformer, ItemObject itemObject, float minimumProgress)
            {
                _agent = agent;
                _targetBone = targetBone;
                _weaponFrameTransformer = weaponFrameTransformer;
                _itemObject = itemObject;
                _minimumProgress = minimumProgress;
            }

            public void InitialiseAtProgress(float progress)
            {
                if (_minimumProgress > progress) return;

                if (_isInitialised) return;

                var weapon = CreateCustomWeapon();
                var baseFrame = GetOriginalWeaponFrame(weapon);

                _attachedVisual = CreateVisualEntity(weapon, baseFrame);
                _mirrorVisual = CreateVisualEntity(weapon, baseFrame);

                AttachToBone(weapon, _attachedVisual, baseFrame);

                _attachedMetaMesh = _attachedVisual.GetMetaMesh(0);
                _mirrorMetaMesh = _mirrorVisual.GetMetaMesh(0);

                HideWeaponVisual(_attachedMetaMesh);

                _isInitialised = true;
            }

            public void OnTick(float dt)
            {
                if (!_isInitialised || _isRemoved) return;

                UpdateVisualFrame(_weaponFrameTransformer.Invoke(GetAttachedFrame()));
            }

            public void Remove()
            {
                if (!_isInitialised || _isRemoved) return;

                _attachedVisual.Remove(0);
                _mirrorVisual.Remove(0);
                _isRemoved = true;
            }

            private MissionWeapon CreateCustomWeapon()
            {
                return new MissionWeapon(_itemObject, null, null);
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
                return _attachedMetaMesh.Frame;
            }

            private void UpdateVisualFrame(MatrixFrame newFrame)
            {
                _mirrorVisual.SetGlobalFrame(_attachedVisual.GetGlobalFrame());
                _mirrorMetaMesh.Frame = newFrame;
            }

            private void AttachToBone(MissionWeapon weapon, GameEntity entity, MatrixFrame frame)
            {
                _agent.AttachWeaponToBone(weapon, entity, (sbyte)_targetBone, ref frame);
            }

            private static void HideWeaponVisual(MetaMesh metaMesh)
            {
                metaMesh?.ClearMeshes();
            }
        }
    }
}