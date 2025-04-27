using DellarteDellaGuerra.Firearm;
using Force.DeepCloner;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.Firearms
{
    public class FirearmReloadAnimationComponent : AgentComponent
    {
        private const string ReloadAnimation = "act_reload_firearm";

        private FirearmReloadStatus _firearmReloadStatus = FirearmReloadStatus.None; 
        
        private bool _isReloading;
        private GameEntity _emptyVisualFirearm;
        private GameEntity _visualFirearm;
        private readonly float _initialHandSwitchProgressEnd = 0.18f;

        private float _deltaTimeTick;

        public FirearmReloadAnimationComponent(Agent agent) : base(agent)
        {
        }

        public override void OnTickAsAI(float dt)
        {
            _deltaTimeTick += dt;

            if (_deltaTimeTick < 1f) return;
            
            if (!Agent.IsHuman) return;

            if (!IsUsingMusket(Agent)) return;
            
            if (!IsReloadingAnimationActive())
            {
                if (!_isReloading) return;
                _isReloading = false;
                RemoveTemporaryWeaponVisuals();
                ResetOriginalWeaponVisualForIdleStance();
                return;
            }

            if (!_isReloading)
            {
                _isReloading = true;
                HideOriginalWeaponVisual();
                InitialiseTemporaryWeaponVisuals();

                // need to wait for the next tick so that the engine initialises the metamesh frame of the empty bone-attached entity
                return;
            }

            if (GetReloadingAnimationProgress() < _initialHandSwitchProgressEnd)
            {
                _firearmReloadStatus = FirearmReloadStatus.OffhandToMainHandSwitch;
            }

            if (GetReloadingAnimationProgress() >= _initialHandSwitchProgressEnd)
            {
                _firearmReloadStatus = FirearmReloadStatus.BlackPowderChargeLoading;
                AttachToBone(Agent, Agent.HandIndex.MainHand,
                    _emptyVisualFirearm,
                    MatrixFrame.Identity);
            }

            UpdateTemporaryWeaponVisuals(dt);
        }

        private void ResetOriginalWeaponVisualForIdleStance()
        {
            var firearmEquipmentIndex = Agent.GetWieldedItemIndex(Agent.HandIndex.MainHand);
            var firearmWeapon = Agent.Equipment[firearmEquipmentIndex];

            Agent.EquipWeaponWithNewEntity(firearmEquipmentIndex, ref firearmWeapon);
            Agent.TryToWieldWeaponInSlot(firearmEquipmentIndex, Agent.WeaponWieldActionType.Instant, false);
        }

        private bool IsReloadingAnimationActive()
        {
            return Agent.GetCurrentAction(1)?.Name.Equals(ReloadAnimation) ?? false;
        }

        private float GetReloadingAnimationProgress()
        {
            return IsReloadingAnimationActive() ? Agent.GetCurrentActionProgress(1) : 0f;
        }

        private void RemoveTemporaryWeaponVisuals()
        {
            _emptyVisualFirearm?.Remove(0);
            _visualFirearm?.Remove(0);
            _emptyVisualFirearm = null;
            _visualFirearm = null;
        }

        private void InitialiseTemporaryWeaponVisuals()
        {
            var weapon = Agent.WieldedWeapon;

            var initialFrame = GetOriginalWeaponFrame(weapon);
            _emptyVisualFirearm = CreateWeapon(weapon, initialFrame);
            HideWeaponVisual(_emptyVisualFirearm);
            AttachToBone(Agent, Agent.HandIndex.OffHand, _emptyVisualFirearm, initialFrame);

            _visualFirearm = CreateWeapon(weapon, initialFrame);
        }

        private static MatrixFrame GetOriginalWeaponFrame(MissionWeapon weapon)
        {
            return weapon.GetWeaponData(false).WeaponFrame;
        }

        private static MatrixFrame GetWeaponFrameForIdleStance(Agent agent, MatrixFrame frame)
        {
            frame.rotation.RotateAboutUp(MathF.PI);
            frame.rotation.RotateAboutSide(1f);

            frame.Elevate(0.02f);

            frame.rotation.RotateAboutUp(-0.46f);
            frame.rotation.RotateAboutForward(-0.26f);

            frame.Strafe(0.19f);

            return frame;
        }

        private MatrixFrame GetWeaponFrameForPowderPouring(Agent agent, MatrixFrame frame)
        {
            MatrixFrame weaponFrameForPowderPouring = GetWeaponFrameForIdleStance(agent, frame);

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

        private MatrixFrame GetWeaponFrameTransitionFromIdleToPowderPouringStart(Agent agent, float reloadingProgress,
            MatrixFrame frame, float dt)
        {
            float percentage = reloadingProgress / _initialHandSwitchProgressEnd + 0.05f;
            percentage = MathF.Clamp(percentage, 0f, 1f);

            return LerpMatrixFrame(GetWeaponFrameForIdleStance(agent, frame),
                GetWeaponFrameForPowderPouring(agent, frame), percentage);
        }

        private static GameEntity CreateWeapon(MissionWeapon weapon, MatrixFrame frame)
        {
            return Mission.Current.SpawnWeaponWithNewEntity(ref weapon, Mission.WeaponSpawnFlags.None,
                frame);
        }

        private void HideOriginalWeaponVisual()
        {
            var weaponEntity =
                Agent.GetWeaponEntityFromEquipmentSlot(Agent.GetWieldedItemIndex(Agent.HandIndex.MainHand));
            HideWeaponVisual(weaponEntity);
        }

        private static void HideWeaponVisual(GameEntity weaponEntity)
        {
            weaponEntity.GetMetaMesh(0)?.ClearMeshes();
        }

        private void UpdateTemporaryWeaponVisuals(float dt)
        {
            _visualFirearm.SetGlobalFrame(_emptyVisualFirearm.GetGlobalFrame());

            var frame = _emptyVisualFirearm.GetMetaMesh(0).Frame.DeepClone();
            frame =
                GetWeaponFrameTransitionFromIdleToPowderPouringStart(Agent, GetReloadingAnimationProgress(), frame, dt);
            _visualFirearm.GetMetaMesh(0).Frame = frame;
        }

        private static void AttachToBone(Agent agent, Agent.HandIndex handToAttachTo, GameEntity weaponEntity,
            MatrixFrame frame)
        {
            agent.ClearAttachedWeapons();
            agent.AttachWeaponToBone(agent.WieldedWeapon,
                weaponEntity,
                handToAttachTo == Agent.HandIndex.OffHand
                    ? (sbyte)HumanBone.HandL
                    : (sbyte)HumanBone.HandR,
                ref frame);
        }


        /// <summary>
        ///     Checks if the agent is currently using a musket weapon.
        /// </summary>
        /// <param name="agent">The agent to check.</param>
        /// <returns>True if the agent is using a musket weapon; otherwise, false.</returns>
        public bool IsUsingMusket(Agent agent)
        {
            var wieldedWeaponIndex = agent.GetWieldedItemIndex(Agent.HandIndex.MainHand);
            if (wieldedWeaponIndex < EquipmentIndex.WeaponItemBeginSlot ||
                wieldedWeaponIndex > EquipmentIndex.NumAllWeaponSlots)
                return false;

            return agent.Equipment[wieldedWeaponIndex]
                .CurrentUsageItem?.WeaponClass == WeaponClass.Musket;
        }
    }
}