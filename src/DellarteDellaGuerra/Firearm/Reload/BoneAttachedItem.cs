using System;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.Firearm.Reload
{
    namespace DellarteDellaGuerra.Firearm.Reload
    {
        public class BoneAttachedItem : ITickable, IOnAgentBuild
        {
            private readonly Agent _agent;
            private readonly Func<MatrixFrame, MatrixFrame> _weaponFrameTransformer;
            private readonly float _minimumProgress;

            private MetaMesh _attachedMetaMesh;
            private MetaMesh _mirrorMetaMesh;

            private GameEntity _attachedVisual;
            private GameEntity _mirrorVisual;

            private bool _isInitialised;
            private readonly ItemObject _itemObject;
            private readonly HumanBone _targetBone;
            private bool _isDisposed;

            public BoneAttachedItem(Agent agent, HumanBone targetBone,
                Func<MatrixFrame, MatrixFrame> weaponFrameTransformer, ItemObject itemObject, float minimumProgress)
            {
                _agent = agent;
                _weaponFrameTransformer = weaponFrameTransformer;
                _minimumProgress = minimumProgress;
                _itemObject = itemObject;
                _targetBone = targetBone;
            }

            public void InitialiseAtProgress(float progress)
            {
                if (_isDisposed || _minimumProgress > progress || _isInitialised) return;
                _isInitialised = true;
            }

            public void OnTick(float dt)
            {
                if (!_isInitialised || _isDisposed) return;

                UpdateVisualFrame(_weaponFrameTransformer.Invoke(GetAttachedFrame()));
                if (!_mirrorVisual.GetVisibilityExcludeParents())
                    SetEntityVisibility(_mirrorVisual, true);
            }

            public void Remove()
            {
                if (_isDisposed) return;
                
                SetEntityVisibility(_mirrorVisual, false);

                _isInitialised = false;
            }

            public void Dispose()
            {
                _attachedVisual.Remove(0);
                _mirrorVisual.Remove(0);
                _isDisposed = true;
            }

            public void OnAgentBuild()
            {
                _attachedVisual = CreateWeaponEntity(_itemObject);
                _attachedMetaMesh = _attachedVisual.GetMetaMesh(0);
                _mirrorVisual = CreateWeaponEntity(_itemObject);
                _mirrorMetaMesh = _mirrorVisual.GetMetaMesh(0);

                InitialiseBoneAttachedEntity(_attachedVisual, _itemObject, _targetBone);
                InitialiseMirrorEntity(_mirrorVisual);
            }

            private void InitialiseBoneAttachedEntity(GameEntity attachedEntity, ItemObject itemObject,
                HumanBone humanBone)
            {
                attachedEntity.GetMetaMesh(0).ClearMeshes();
                AttachEntityToBone(attachedEntity, itemObject, humanBone);
            }

            private void InitialiseMirrorEntity(GameEntity entity)
            {
                SetEntityVisibility(entity, false);
            }

            private void AttachEntityToBone(GameEntity attachedEntity, ItemObject itemObject, HumanBone humanBone)
            {
                var weapon = CreateCustomWeapon(itemObject);
                var baseFrame = GetOriginalWeaponFrame(weapon);
                AttachToBone(weapon, attachedEntity, baseFrame, humanBone);
            }

            private MissionWeapon CreateCustomWeapon(ItemObject itemObject)
            {
                return new MissionWeapon(itemObject, null, null);
            }

            private static MatrixFrame GetOriginalWeaponFrame(MissionWeapon weapon)
            {
                return weapon.GetWeaponData(false).WeaponFrame;
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

            private void AttachToBone(MissionWeapon weapon, GameEntity entity, MatrixFrame frame, HumanBone humanBone)
            {
                _agent.AttachWeaponToBone(weapon, entity, (sbyte)humanBone, ref frame);
            }

            private GameEntity CreateWeaponEntity(ItemObject itemObject)
            {
                var missionWeapon = new MissionWeapon(itemObject, null, null);
                return Mission.Current.SpawnWeaponWithNewEntity(ref missionWeapon,
                    Mission.WeaponSpawnFlags.None, MatrixFrame.Identity);
            }

            private void SetEntityVisibility(GameEntity entity, bool isVisible)
            {
                entity.SetVisibilityExcludeParents(isVisible);
                entity.UpdateVisibilityMask();
            }
        }
    }
}