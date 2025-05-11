using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.Firearm.Reload
{
    public class InitialHandSwapReloadComponent : IReloadPhase
    {
        private const float InitialHandSwitchProgressStart = 0f;
        private const float InitialHandSwitchProgressEnd = 0.18f;

        private readonly WeaponReloadPhaseComponent _reloadPhase;

        private float _progress;

        public InitialHandSwapReloadComponent(Agent agent)
        {
            _reloadPhase = new WeaponReloadPhaseComponent(() =>
                new BoneAttachedWeapon(agent, Agent.HandIndex.MainHand, HumanBone.HandL, TransformWeaponFrame));
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
            _progress = progress;
            _reloadPhase.OnReloadProgress(progress);
        }

        public void OnReloadEnd()
        {
            _reloadPhase.OnReloadEnd();
        }

        public float ReloadingProgressStart => InitialHandSwitchProgressStart;
        public float ReloadingProgressEnd => InitialHandSwitchProgressEnd;

        private static MatrixFrame GetWeaponFrameForIdleStance(MatrixFrame frame)
        {
            frame.rotation.RotateAboutUp(MathF.PI);
            frame.rotation.RotateAboutSide(1f);

            frame.Elevate(0.02f);

            frame.rotation.RotateAboutUp(-0.46f);
            frame.rotation.RotateAboutForward(-0.26f);

            frame.Strafe(0.19f);

            return frame;
        }

        private static MatrixFrame GetWeaponFrameForPowderPouring(MatrixFrame frame)
        {
            MatrixFrame weaponFrameForPowderPouring = GetWeaponFrameForIdleStance(frame);

            weaponFrameForPowderPouring.rotation.RotateAboutUp(0.2f);
            weaponFrameForPowderPouring.rotation.RotateAboutForward(0.1f);
            weaponFrameForPowderPouring.Advance(0.05f);
            weaponFrameForPowderPouring.Elevate(-0.03f);

            return weaponFrameForPowderPouring;
        }

        private static MatrixFrame LerpMatrixFrame(MatrixFrame from, MatrixFrame to, float t)
        {
            Vec3 pos = Vec3.Lerp(from.origin, to.origin, t);
            Mat3 rot = Mat3.Lerp(from.rotation, to.rotation, t);
            return new MatrixFrame(rot, pos);
        }

        private static MatrixFrame GetWeaponFrameTransitionFromIdleToPowderPouringStart(float reloadingProgress,
            MatrixFrame frame)
        {
            float percentage = reloadingProgress / InitialHandSwitchProgressEnd + 0.05f;
            percentage = MathF.Clamp(percentage, 0f, 1f);

            return LerpMatrixFrame(GetWeaponFrameForIdleStance(frame),
                GetWeaponFrameForPowderPouring(frame), percentage);
        }

        private MatrixFrame TransformWeaponFrame(MatrixFrame weaponFrame)
        {
            return GetWeaponFrameTransitionFromIdleToPowderPouringStart(_progress, weaponFrame);
        }
    }
}