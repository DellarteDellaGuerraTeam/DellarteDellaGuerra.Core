using System;
using DellarteDellaGuerra.Firearm.Reload.DellarteDellaGuerra.Firearm.Reload;
using Force.DeepCloner;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.Firearm.Reload
{
    public class BlackPowderReloadComponent : IReloadPhase
    {
        private const float ProgressStart = 0.074f;
        private const float ProgressEnd = 0.416f;

        private float _progress;

        private readonly WeaponReloadPhaseComponent _reloadPhase;

        public BlackPowderReloadComponent(Agent agent, ItemObject firearmItem)
        {
            _reloadPhase = new WeaponReloadPhaseComponent(() => new BoneAttachedItem(agent,
                HumanBone.HandR,
                TransformWeaponFrame, firearmItem,
                ProgressStart));
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
                f.Advance(0.01f);
                f.Elevate(0.08f);
                f.rotation.RotateAboutForward(-0.2f);
                f.Strafe(-0.07f);
                f.rotation.RotateAboutUp(0.1f);
                
                return f;
            });

            // try to counter unwanted rotation offset
            frame = AdjustFrameForProgress(ProgressEnd - 0.005f, ProgressEnd, frame, f =>
            {
                f.Elevate(-0.09f);
                f.rotation.RotateAboutForward(0.3f);

                return f;
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