using DellarteDellaGuerra.Firearm.Reload.DellarteDellaGuerra.Firearm.Reload;
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

        public InitialHandSwapReloadComponent(Agent agent, ItemObject firearmItem)
        {
            _reloadPhase = new WeaponReloadPhaseComponent(() => new BoneAttachedItem(agent,
                HumanBone.HandL,
                TransformWeaponFrame, firearmItem, InitialHandSwitchProgressStart));
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
            _progress = progress;
            _reloadPhase.OnReloadProgress(progress);
        }

        public void OnReloadPhaseEnd()
        {
            _reloadPhase.OnReloadPhaseEnd();
        }

        public void OnAgentBuild()
        {
            _reloadPhase.OnAgentBuild();
        }
        
        public float PhaseProgressStart => InitialHandSwitchProgressStart;
        public float PhaseProgressEnd => InitialHandSwitchProgressEnd;
        
        private static MatrixFrame GetWeaponFrameForIdleStance(MatrixFrame frame)
        {
            frame.rotation.RotateAboutUp(MathF.PI);
            frame.rotation.RotateAboutSide(0.9f);
            frame.Elevate(0.02f);
            frame.Advance(0.005f);
            frame.rotation.RotateAboutUp(-0.53f);
            frame.rotation.RotateAboutForward(-0.15f);
            frame.Strafe(0.192f);

            return frame;
        }

        private MatrixFrame GetWeaponFrameForPowderPouring(MatrixFrame frame)
        {
            MatrixFrame weaponFrameForPowderPouring = GetWeaponFrameForIdleStance(frame);
            weaponFrameForPowderPouring.rotation.RotateAboutSide(1f);
            weaponFrameForPowderPouring.rotation.RotateAboutForward(0.1f);
            weaponFrameForPowderPouring.Advance(0.01f);
            weaponFrameForPowderPouring.Elevate(-0.04f);
            weaponFrameForPowderPouring.Strafe(0.1f);

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
            newFrame.rotation.RotateAboutUp(0.2f);
            newFrame.Advance(0.07f);

            newFrame.rotation.RotateAboutForward(0.1f);
            newFrame.Elevate(-0.03f);

            return LerpMatrixFrame(GetWeaponFrameForPowderPouring(frame),
                newFrame, (reloadingProgress - firstProgressEndStep) / .01f);
        }

        private MatrixFrame TransformWeaponFrame(MatrixFrame weaponFrame)
        {
            return GetWeaponFrameTransitionFromIdleToPowderPouringStart(_progress, weaponFrame);
        }

        public void Dispose()
        {
            _reloadPhase.Dispose();
        }
    }
}