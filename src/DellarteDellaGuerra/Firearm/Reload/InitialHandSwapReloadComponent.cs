using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.Firearm.Reload
{
    public class InitialHandSwapReloadComponent : IReloadPhase
    {
        private const float InitialHandSwitchProgressStart = 0f;
        private const float InitialHandSwitchProgressEnd = 0.074f;

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
            // frame.rotation.RotateAboutUp(MathF.PI);
            // frame.rotation.RotateAboutSide(0.9f);
            //
            // frame.Elevate(0.02f);
            //
            // frame.rotation.RotateAboutUp(-0.47f);
            // frame.rotation.RotateAboutForward(-0.25f);
            //
            // frame.Strafe(0.2f);

            frame.rotation.RotateAboutUp(MathF.PI);
            frame.rotation.RotateAboutSide(0.9f);
            //
            frame.Elevate(0.02f);
            frame.Advance(0.005f);
            //
            frame.rotation.RotateAboutUp(-0.53f);
            frame.rotation.RotateAboutForward(-0.15f);
            //
            frame.Strafe(0.192f);

            return frame;
        }

        private MatrixFrame GetWeaponFrameForPowderPouring(MatrixFrame frame)
        {
            MatrixFrame weaponFrameForPowderPouring = GetWeaponFrameForIdleStance(frame);

            // weaponFrameForPowderPouring.rotation.RotateAboutUp(0.4f);
            // weaponFrameForPowderPouring.rotation.RotateAboutForward(0.1f);
            // weaponFrameForPowderPouring.Advance(0.05f);
            // weaponFrameForPowderPouring.Elevate(-0.03f);

            weaponFrameForPowderPouring.rotation.RotateAboutSide(1f);
            weaponFrameForPowderPouring.rotation.RotateAboutForward(0.1f);
            weaponFrameForPowderPouring.Advance(0.01f);
            // weaponFrameForPowderPouring.Advance(0.06f);
            weaponFrameForPowderPouring.Elevate(-0.04f);
            weaponFrameForPowderPouring.Strafe(0.1f);
            // weaponFrameForPowderPouring.rotation.RotateAboutUp(0.2f);

            return weaponFrameForPowderPouring;
        }

        private static MatrixFrame LerpMatrixFrame(MatrixFrame from, MatrixFrame to, float t)
        {
            Vec3 pos = Vec3.Lerp(from.origin, to.origin, t);
            Mat3 rot = Mat3.Lerp(from.rotation, to.rotation, t);
            return new MatrixFrame(rot, pos);
        }

        private MatrixFrame GetWeaponFrameTransitionFromIdleToPowderPouringStart(float reloadingProgress,
            MatrixFrame frame)
        {
            float firstProgressEndStep = 0.05f;
            float percentage = reloadingProgress / firstProgressEndStep;
            percentage = MathF.Clamp(percentage, 0f, 1f);

            if (_progress <= firstProgressEndStep)
                return LerpMatrixFrame(GetWeaponFrameForIdleStance(frame),
                GetWeaponFrameForPowderPouring(frame), percentage);

            var newFrame = GetWeaponFrameForPowderPouring(frame);
            // newFrame.rotation.RotateAboutUp(0.25f);
            // newFrame.rotation.RotateAboutForward(.14f);
            // newFrame.rotation.RotateAboutSide(-.15f);
            // newFrame.Advance(0.12f);
            // newFrame.Elevate(-0.03f);
            // newFrame.Strafe(0.03f);

            // newFrame.rotation.RotateAboutUp(0.15f);
            // newFrame.rotation.RotateAboutForward(0.1f);
            // newFrame.Elevate(-0.08f);
            // newFrame.Strafe(0.05f);
            // newFrame.rotation.RotateAboutSide(-.15f);
            // newFrame.Advance(0.1f);
            // newFrame.Strafe(0.1f);
            // newFrame.Strafe(0.1f);

            // newFrame.rotation.RotateAboutForward(0.1f);
            newFrame.rotation.RotateAboutUp(0.2f);
            newFrame.Advance(0.07f);

            newFrame.rotation.RotateAboutForward(0.1f);
            newFrame.Elevate(-0.03f);

            // newFrame.Strafe(0.01f);

            return LerpMatrixFrame(GetWeaponFrameForPowderPouring(frame),
                newFrame, (reloadingProgress - firstProgressEndStep) / .01f);
        }

        private MatrixFrame TransformWeaponFrame(MatrixFrame weaponFrame)
        {
            return GetWeaponFrameTransitionFromIdleToPowderPouringStart(_progress, weaponFrame);
        }
    }
}