using System;
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

        private float _progress;

        private readonly Agent _agent;

        private readonly WeaponReloadPhaseComponent _reloadPhase;
        private BoneAttachedWeapon _ramrod;

        public RammingReloadComponent(Agent agent)
        {
            _reloadPhase = new WeaponReloadPhaseComponent(() =>
                new BoneAttachedWeapon(agent, Agent.HandIndex.MainHand, HumanBone.HandL, TransformFirearmFrame));
            _agent = agent;
        }

        public void OnTick(float dt)
        {
            _reloadPhase.OnTick(dt);

            if (_progress > 0.45f)
            {
                ItemObject ramrodItem = Items.All.Find(item => item.StringId.StartsWith("arquebusramrod"));
                if (ramrodItem is not null && _ramrod == null)
                {
                    _ramrod = new BoneAttachedWeapon(_agent, Agent.HandIndex.MainHand, HumanBone.ItemR,
                        TransformRamrodFrame);
                    _ramrod.Initialise(new MissionWeapon(ramrodItem, null, null));
                }
            }

            _ramrod?.OnTick(dt);
        }

        public void OnReloadStart()
        {
            _reloadPhase.OnReloadStart();
        }

        public void OnReloadProgress(float progress)
        {
            _reloadPhase.OnReloadProgress(progress);
            _progress = progress;
        }

        public void OnReloadEnd()
        {
            _reloadPhase.OnReloadEnd();
            _ramrod.Remove();
            _ramrod = null;
        }

        public float ReloadingProgressStart => ProgressStart;
        public float ReloadingProgressEnd => ProgressEnd;

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
    }
}