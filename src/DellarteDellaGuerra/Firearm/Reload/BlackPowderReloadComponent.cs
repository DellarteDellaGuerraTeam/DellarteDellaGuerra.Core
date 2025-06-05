using Force.DeepCloner;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.Firearm.Reload
{
    public class BlackPowderReloadComponent : IReloadPhase
    {
        private const float ProgressStart = 0.07f;
        private const float ProgressEnd = 0.8f;

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
            newWeaponFrame.rotation.RotateAboutForward(-0.43f);
            newWeaponFrame.Strafe(0.6f);
            newWeaponFrame.Elevate(-0.1f);
            return newWeaponFrame;
        }
    }
}