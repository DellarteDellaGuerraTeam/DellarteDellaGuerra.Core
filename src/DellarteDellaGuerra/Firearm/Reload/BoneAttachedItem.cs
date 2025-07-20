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
            private readonly Func<MatrixFrame, MatrixFrame> _weaponFrameTransformer;
            private readonly float _minimumProgress;

            private readonly MetaMesh _attachedMetaMesh;
            private readonly MetaMesh _mirrorMetaMesh;

            private readonly GameEntity _attachedVisual;
            private readonly GameEntity _mirrorVisual;

            private bool _isInitialised;

            public BoneAttachedItem(Agent agent, HumanBone targetBone,
                Func<MatrixFrame, MatrixFrame> weaponFrameTransformer, ItemObject itemObject, float minimumProgress)
            {
                _agent = agent;
                _weaponFrameTransformer = weaponFrameTransformer;
                _minimumProgress = minimumProgress;
                _mirrorVisual = CreateWeaponEntity(itemObject);
                _mirrorMetaMesh = _mirrorVisual.GetMetaMesh(0);
                _attachedVisual = CreateWeaponEntity(itemObject);
                _attachedMetaMesh = _attachedVisual.GetMetaMesh(0);

                InitialiseBoneAttachedEntity(_attachedVisual, itemObject, targetBone);
                InitialiseMirrorEntity(_mirrorVisual);
            }

            public void InitialiseAtProgress(float progress)
            {
                if (_minimumProgress > progress) return;
                if (_isInitialised) return;
                _isInitialised = true;
            }

            public void OnTick(float dt)
            {
                if (_isInitialised)
                {
                    UpdateVisualFrame(_weaponFrameTransformer.Invoke(GetAttachedFrame()));
                    if (!_mirrorVisual.GetVisibilityExcludeParents())
                        SetEntityVisibility(_mirrorVisual, true);
                }
            }

            public void Remove()
            {
                SetEntityVisibility(_mirrorVisual, false);

                _isInitialised = false;
            }

            private void InitialiseBoneAttachedEntity(GameEntity attachedEntity, ItemObject itemObject,
                HumanBone humanBone)
            {
                AttachEntityToBone(attachedEntity, itemObject, humanBone);
                attachedEntity.GetMetaMesh(0).ClearMeshes();
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
                // var script = entity.GetScriptComponents<SpawnedItemEntity>().ToList().First();
                // script.IsVisible = isVisible;
                entity.SetVisibilityExcludeParents(isVisible);
                entity.UpdateVisibilityMask();
            }
        }
    }
}