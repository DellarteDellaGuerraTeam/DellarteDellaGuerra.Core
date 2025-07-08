using Force.DeepCloner;
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

        private readonly WeaponReloadPhaseComponent _reloadPhase;

        public RammingReloadComponent(Agent agent)
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
            newWeaponFrame.rotation.RotateAboutSide(MathF.PI / 2 + 0.2f);
            newWeaponFrame.rotation.RotateAboutForward(MathF.PI);

            newWeaponFrame.rotation.RotateAboutUp(-0.2f);
            newWeaponFrame.rotation.RotateAboutForward(-0.2f);

            newWeaponFrame.Advance(0.03f);
            newWeaponFrame.Elevate(-0.01f);

            newWeaponFrame.Strafe(0.5f);

            return newWeaponFrame;
        }
    }
}