using System;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using Force.DeepCloner;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.Firearms
{
    public class FirearmReloadComponent : AgentComponent
    {
        private readonly ILogger _logger;

        private const string ReloadAnimation = "act_reload_firearm";
        private const float SwitchToOffHandAnimationThreshold = 0.4f;
        private const float SwitchBackToMainHandAnimationThreshold = 0.9f;
        private const float StartedPouringPowderAnimationThreshold = 0.1f;
        private const float FinishedPouringPowderAnimationThreshold = 0.3f;

        private bool _hasStartedPouringPowder;
        private bool _hasFinishedPouringPowder;
        private bool _hasSwitchedToOffHand;
        private bool _initialSwitch;

        private MatrixFrame initialFrame;
        private float initialDeltaTime;

        private bool _isMainAgentReloadingFirearm;

        public FirearmReloadComponent(ILoggerFactory loggerFactory, Agent agent) : base(agent)
        {
            _logger = loggerFactory.CreateLogger<FirearmReloadMissionLogic>();
        }

        public override void OnTickAsAI(float dt)
        {
            try
            {
                if (Agent.IsHuman && Agent.GetCurrentAction(1)?.Name == ReloadAnimation)
                {
                    _isMainAgentReloadingFirearm = true;
                    UpdateReloadingState(Agent, dt);
                }
                else if (_isMainAgentReloadingFirearm)
                {
                    GameEntity weaponEntity =
                        Agent.GetWeaponEntityFromEquipmentSlot(Agent.GetWieldedItemIndex(Agent.HandIndex.MainHand));
                    if (weaponEntity == null) return;

                    var weaponEntityFrame = Agent.WieldedWeapon.CurrentUsageItem.Frame;

                    Agent.AttachWeaponToBone(Agent.WieldedWeapon, weaponEntity,
                        (sbyte)HumanBone.ItemR,
                        ref weaponEntityFrame);

                    ResetReloadProgressFlags();
                }
            }
            catch (InvalidOperationException e)
            {
                _logger.Error(
                    $"Updating reloading state failed for Agent {Agent.Character?.Name.Value ?? Agent.Name}",
                    e);
            }
        }

        private void UpdateReloadingState(Agent agent, float dt)
        {
            float reloadingProgress = agent.GetCurrentActionProgress(1);

            if (!_initialSwitch)
            {
                AttachWeaponToOffHandAtOriginalPosition(agent, Agent.HandIndex.OffHand);
                _initialSwitch = true;
            }

            if (reloadingProgress < StartedPouringPowderAnimationThreshold)
            {
                RevertWeaponPositionAndRotation(agent, reloadingProgress, dt);
            }

            if (reloadingProgress >= StartedPouringPowderAnimationThreshold && !_hasStartedPouringPowder)
            {
                AttachWeaponToWieldedHandWithZAxisTransformation(agent, Agent.HandIndex.MainHand, -0.4f);
                _hasStartedPouringPowder = true;
            }

            if (reloadingProgress >= FinishedPouringPowderAnimationThreshold && !_hasFinishedPouringPowder)
            {
                AttachWeaponToWieldedHandWithZAxisTransformation(agent, Agent.HandIndex.OffHand, -0.4f);
                _hasFinishedPouringPowder = true;
            }

            if (reloadingProgress > SwitchToOffHandAnimationThreshold && !_hasSwitchedToOffHand)
            {
                AttachWeaponToHand(agent, Agent.HandIndex.OffHand, GetHandWieldedWeaponFrame(agent).frame);
                _hasSwitchedToOffHand = true;
            }

            if (reloadingProgress > SwitchBackToMainHandAnimationThreshold)
                AttachWeaponToHand(agent, Agent.HandIndex.MainHand, GetHandWieldedWeaponFrame(agent).frame);
        }

        private void AttachWeaponToOffHandAtOriginalPosition(Agent agent, Agent.HandIndex handToAttachTo)
        {
            var handWieldedWeaponFrame = GetHandWieldedWeaponFrame(agent);
            GameEntity weaponEntity = agent.GetWeaponEntityFromEquipmentSlot(handWieldedWeaponFrame.equipmentIndex);
            if (weaponEntity == null) return;

            MatrixFrame weaponEntityFrame = handWieldedWeaponFrame.weapon.GetWeaponData(false).WeaponFrame;

            // Offset position to match left-hand bone
            MatrixFrame offHandBoneFrame =
                agent.AgentVisuals.GetSkeleton().GetBoneLocalRestFrame((sbyte)HumanBone.HandL);
            MatrixFrame rightHandBoneFrame =
                agent.AgentVisuals.GetSkeleton().GetBoneLocalRestFrame((sbyte)HumanBone.ItemR);
            Vec3 offset = offHandBoneFrame.origin - rightHandBoneFrame.origin;

            weaponEntityFrame.rotation.RotateAboutUp(MathF.PI);
            weaponEntityFrame.rotation.RotateAboutSide(1f);
            weaponEntityFrame.origin += offset;


            weaponEntityFrame.origin.x -= 0.14f;
            weaponEntityFrame.origin.y += 0.10f;
            weaponEntityFrame.origin.z += 0.03f;


            weaponEntityFrame.rotation.RotateAboutUp(-0.45f);
            weaponEntityFrame.rotation.RotateAboutForward(-0.38f);

            initialFrame = weaponEntityFrame.DeepClone();

            AttachWeaponToHand(agent, handToAttachTo, weaponEntityFrame);
        }

        private void RevertWeaponPositionAndRotation(Agent agent, float reloadingProgress, float dt)
        {
            float percentage = StartedPouringPowderAnimationThreshold -
                               reloadingProgress / StartedPouringPowderAnimationThreshold;

            var weaponEntityFrame = initialFrame.DeepClone();

            weaponEntityFrame.origin.x -= 0.14f * percentage;
            weaponEntityFrame.origin.y += 0.10f * percentage;
            weaponEntityFrame.origin.z += 0.03f * percentage;


            weaponEntityFrame.rotation.RotateAboutUp(-0.45f * percentage);
            weaponEntityFrame.rotation.RotateAboutForward(-0.38f * percentage);

            AttachWeaponToHand(agent, Agent.HandIndex.OffHand, weaponEntityFrame);
        }

        private static void AttachWeaponToWieldedHandWithZAxisTransformation(Agent agent, Agent.HandIndex hand,
            float zTransformation)
        {
            // var handWieldedWeaponFrame = GetHandWieldedWeaponFrame(agent);
            // MatrixFrame weaponEntityFrame = handWieldedWeaponFrame.weapon.GetWeaponData(false).WeaponFrame;

            var frame = MatrixFrame.Identity;
            // frame.rotation.RotateAboutForward(90f);
            // frame.origin.z += zTransformation;

            AttachWeaponToHand(agent, hand, frame);
        }

        private static void AttachWeaponToHandAtOriginalPosition(Agent agent, Agent.HandIndex handToAttachTo)
        {
            var handWieldedWeaponFrame = GetHandWieldedWeaponFrame(agent);
            GameEntity weaponEntity = agent.GetWeaponEntityFromEquipmentSlot(handWieldedWeaponFrame.equipmentIndex);
            if (weaponEntity == null) return;

            MatrixFrame weaponEntityFrame = handWieldedWeaponFrame.weapon.GetWeaponData(false).WeaponFrame;
            AttachWeaponToHand(agent, handToAttachTo, weaponEntityFrame);
        }

        private static void AttachWeaponToHand(Agent agent, Agent.HandIndex handToAttachTo,
            MatrixFrame weaponEntityFrame)
        {
            agent.AttachWeaponToBone(GetHandWieldedWeaponFrame(agent).weapon,
                agent.GetWeaponEntityFromEquipmentSlot(GetHandWieldedWeaponFrame(agent).equipmentIndex),
                handToAttachTo == Agent.HandIndex.OffHand
                    ? (sbyte)HumanBone.HandL
                    : (sbyte)HumanBone.HandR,
                ref weaponEntityFrame);
        }

        private static (MatrixFrame frame, MissionWeapon weapon, EquipmentIndex equipmentIndex, Agent.HandIndex hand)
            GetHandWieldedWeaponFrame(Agent agent)
        {
            var handIndex = GetWieldedWeaponHand(agent);
            EquipmentIndex equipmentIndex = agent.GetWieldedItemIndex(handIndex);
            MissionWeapon missionWeapon = agent.Equipment[equipmentIndex];

            if (missionWeapon.IsEmpty)
                throw new InvalidOperationException($"Could not find a valid weapon at {equipmentIndex}");

            MatrixFrame frame = missionWeapon.GetWeaponComponentDataForUsage(0).Frame;
            return (frame, missionWeapon, equipmentIndex, handIndex);
        }

        private static Agent.HandIndex GetWieldedWeaponHand(Agent agent)
        {
            for (var handIndex = Agent.HandIndex.MainHand; handIndex <= Agent.HandIndex.OffHand; handIndex++)
                if (!agent.GetWieldedItemIndex(handIndex).Equals(EquipmentIndex.None))
                    return handIndex;

            throw new InvalidOperationException("Could not find any hand held weapon while reloading");
        }

        private void ResetReloadProgressFlags()
        {
            _hasStartedPouringPowder = false;
            _hasFinishedPouringPowder = false;
            _hasSwitchedToOffHand = false;
            _initialSwitch = false;
            _isMainAgentReloadingFirearm = false;
        }
    }
}
