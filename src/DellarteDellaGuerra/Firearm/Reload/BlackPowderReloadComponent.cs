using System;
using Force.DeepCloner;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.Firearm.Reload
{
    public class BlackPowderReloadComponent : IReloadPhase
    {
        private const float ProgressStart = 0.074f;
        private const float ProgressEnd = 0.422f;

        private float _progress;

        private readonly WeaponReloadPhaseComponent _reloadPhase;

        public BlackPowderReloadComponent(Agent agent)
        {
            _reloadPhase = new WeaponReloadPhaseComponent(() =>
                new BoneAttachedWeapon(agent, Agent.HandIndex.MainHand, HumanBone.HandR, TransformWeaponFrame));
        }

        public void OnTick(float dt)
        {
            _reloadPhase.OnTick(dt);
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
        }

        public float ReloadingProgressStart => ProgressStart;
        public float ReloadingProgressEnd => ProgressEnd;

        private MatrixFrame TransformWeaponFrame(MatrixFrame weaponFrame)
        {
            var newWeaponFrame = weaponFrame.DeepClone();
            newWeaponFrame.rotation.RotateAboutSide(MathF.PI + MathF.PI / 4f);
            newWeaponFrame.rotation.RotateAboutForward(-0.4f);
            newWeaponFrame.rotation.RotateAboutUp(0.1f);
            newWeaponFrame.Strafe(0.62f);
            newWeaponFrame.Elevate(-0.1f);

            var frame = AdjustFrameForProgress(ProgressStart, ProgressStart + 0.02f, newWeaponFrame, f =>
            {
                f.rotation.RotateAboutUp(-0.13f);
                f.Advance(-0.085f);
                return f;
            });

            frame = AdjustFrameForProgress(ProgressStart + 0.06f, ProgressStart + 0.08f, frame, f =>
            {
                f.Elevate(-0.05f);
                f.rotation.RotateAboutForward(0.1f);

                f.Advance(-0.05f);
                f.rotation.RotateAboutUp(-0.1f);
                f.Strafe(0.02f);
                return f;
            });

            frame = AdjustFrameForProgress(ProgressEnd - 0.02f, ProgressEnd, frame, f =>
            {
                // f.Advance(0.05f);

                f.rotation.RotateAboutForward(-0.05f);

                f.rotation.RotateAboutUp(0.05f);
                f.Strafe(-0.2f);
                return f;
            });

            // frame = AdjustFrameForProgress(ProgressEnd - 0.020f, ProgressEnd - 0.01f, frame, f =>
            // {
            //     // f.Advance(0.05f);
            //
            //     f.Strafe(-0.05f);
            //     return f;
            // });
            //
            // frame = AdjustFrameForProgress(ProgressEnd - 0.01f, ProgressEnd, frame, f =>
            // {
            //     // f.Advance(0.05f);
            //
            //     f.Strafe(0.05f);
            //     return f;
            // });
            
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

                var transformedFrame = transformer.Invoke(weaponFrame.DeepClone());

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