using System;
using System.Collections.Generic;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Firearm.Reload.DellarteDellaGuerra.Firearm.Reload;
using Force.DeepCloner;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.Firearm.Reload
{
    public class RammingReloadComponent : IReloadPhase
    {
        private const float ProgressStart = 0.416f;
        private const float ProgressEnd = 1f;
        private const float RamrodMinimumProgress = 0.45f;
        private const string RamrodItemId = "arquebusramrod";

        private readonly ItemObject _ramrodItem = Items.All.Find(item => item.StringId.StartsWith(RamrodItemId));
        
        private float _progress;

        private readonly WeaponReloadPhaseComponent _reloadPhase;

        public RammingReloadComponent(Agent agent, ItemObject firearmItem, ILoggerFactory loggerFactory)
        {
            var weaponCreators = new List<Func<BoneAttachedItem>>
            {
                () => new BoneAttachedItem(agent, HumanBone.HandL, TransformFirearmFrame,
                    firearmItem,
                    ProgressStart)
            };

            if (_ramrodItem is null)
            {
                loggerFactory.CreateLogger<RammingReloadComponent>()
                    .Error($"Could not find item '{RamrodItemId}'. Ramrod will not appear during reload");
            }
            else
            {
                weaponCreators.Add(() => new BoneAttachedItem(agent, HumanBone.ItemR,
                    TransformRamrodFrame, _ramrodItem, RamrodMinimumProgress));
            }

            _reloadPhase = new WeaponReloadPhaseComponent(weaponCreators.ToArray());
        }

        public void OnTick(float dt)
        {
            _reloadPhase.OnTick(dt);
        }

        public void OnReloadPhaseStart()
        {
            _reloadPhase.OnReloadPhaseStart();
        }

        public void OnReloadProgress(float progress)
        {
            _reloadPhase.OnReloadProgress(progress);
            _progress = progress;
        }

        public void OnReloadPhaseEnd()
        {
            _reloadPhase.OnReloadPhaseEnd();
        }

        public void OnAgentBuild()
        {
            _reloadPhase.OnAgentBuild();
        }
        
        public float PhaseProgressStart => ProgressStart;
        public float PhaseProgressEnd => ProgressEnd;

        private MatrixFrame TransformFirearmFrame(MatrixFrame weaponFrame)
        {
            var newWeaponFrame = weaponFrame.DeepClone();
            newWeaponFrame.rotation.RotateAboutSide(MathF.PI / 2 + 0.2f);
            newWeaponFrame.rotation.RotateAboutForward(MathF.PI);

            newWeaponFrame.rotation.RotateAboutUp(-0.2f);
            newWeaponFrame.rotation.RotateAboutForward(-0.2f);

            newWeaponFrame.Advance(0.03f);
            newWeaponFrame.Elevate(-0.01f);

            newWeaponFrame.Strafe(0.5f);

            return newWeaponFrame;
        }

        private MatrixFrame TransformRamrodFrame(MatrixFrame weaponFrame)
        {
            var frame = weaponFrame.DeepClone();
            frame.rotation.RotateAboutForward(MathF.PI / 2f - 0.08f);
            frame.rotation.RotateAboutUp(-0.08f);

            frame = AdjustFrameForProgress(0.47f, 0.48f, frame, matrixFrame =>
            {
                matrixFrame.Strafe(-0.2f);
                return matrixFrame;
            });
            frame = AdjustFrameForProgress(0.49f, 0.51f, frame, matrixFrame =>
            {
                matrixFrame.Strafe(-0.25f);
                return matrixFrame;
            });


            frame = AdjustFrameForProgress(0.68f, 0.72f, frame, matrixFrame =>
            {
                matrixFrame.Strafe(0.4f);
                return matrixFrame;
            });

            frame = AdjustFrameForProgress(0.90f, 0.91f, frame, matrixFrame =>
            {
                matrixFrame.Strafe(-0.4f);
                return matrixFrame;
            });

            frame = AdjustFrameForProgress(0.96f, 0.98f, frame, matrixFrame =>
            {
                matrixFrame.Strafe(0.4f);
                return matrixFrame;
            });

            return frame;
        }

        private MatrixFrame AdjustFrameForProgress(
            float progressStart,
            float progressEnd,
            MatrixFrame weaponFrame,
            Func<MatrixFrame, MatrixFrame> transformer)
        {
            if (_progress >= progressStart)
            {
                float lerpProgress = (_progress - progressStart) / (progressEnd - progressStart);
                lerpProgress = Math.Min(1f, lerpProgress);

                var transformedFrame = transformer.Invoke(weaponFrame);

                return LerpMatrixFrame(weaponFrame, transformedFrame, lerpProgress);
            }

            return weaponFrame;
        }

        private static MatrixFrame LerpMatrixFrame(MatrixFrame from, MatrixFrame to, float t)
        {
            Vec3 pos = Vec3.Lerp(from.origin, to.origin, t);
            Mat3 rot = Mat3.Lerp(from.rotation, to.rotation, t);
            return new MatrixFrame(rot, pos);
        }

        public void OnAgentRemoved()
        {
            _reloadPhase.OnAgentRemoved();
        }
    }
}