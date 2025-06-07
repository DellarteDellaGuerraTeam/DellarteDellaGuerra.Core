using Force.DeepCloner;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.Firearm.Reload
{
    public class BlackPowderReloadComponent : IReloadPhase
    {
        private const float ProgressStart = 0.074f;
        private const float ProgressEnd = 0.8f;

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
            // newWeaponFrame.rotation.RotateAboutUp(0.1f);
            newWeaponFrame.Strafe(0.57f);
            newWeaponFrame.Elevate(-0.1f);

            if (_progress >= ProgressStart)
            {
                var frame = newWeaponFrame.DeepClone();
                frame.Elevate(-0.06f);
                frame.rotation.RotateAboutForward(0.1f);

                frame.Advance(-0.1f);
                frame.rotation.RotateAboutUp(-0.2f);

                float lerpDuration = 0.1f;

                if (_progress <= ProgressStart + lerpDuration)
                {
                    float lerpProgress = (_progress - ProgressStart) / lerpDuration;
                    newWeaponFrame = LerpMatrixFrame(newWeaponFrame, frame, lerpProgress);
                }
                else
                {
                    newWeaponFrame = frame; // Maintain the final transformed state
                }
            }

            return newWeaponFrame;
        }

        private static MatrixFrame LerpMatrixFrame(MatrixFrame from, MatrixFrame to, float t)
        {
            Vec3 pos = Vec3.Lerp(from.origin, to.origin, t);
            Mat3 rot = Mat3.Lerp(from.rotation, to.rotation, t);
            return new MatrixFrame(rot, pos);
        }
    }
}