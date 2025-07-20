using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.Firearm.Reload
{
    public class ReloadComponent : ITickable
    {
        private readonly IWeaponEntityRepository _weaponEntityRepository;
        private readonly List<IReloadPhase> _phases;
        private readonly Agent _agent;
        private readonly Dictionary<IReloadPhase, bool> _activePhaseStates = new();

        private readonly float _baseAnimationDuration = GetBaseReloadAnimationDuration();
        private readonly float _continuedAnimationDuration = GetContinuedReloadAnimationDuration();

        private readonly float _totalDuration;

        private const string ReloadAnimationName = "reload_firearm";
        private const string ReloadContinueAnimationName = "firearm_reload_continue";

        public ReloadComponent(List<IReloadPhase> phases, Agent agent,
            IWeaponEntityRepository weaponEntityRepository)
        {
            _phases = phases;
            _agent = agent;
            _weaponEntityRepository = weaponEntityRepository;
            _totalDuration = _baseAnimationDuration + _continuedAnimationDuration;

            foreach (var phase in _phases)
                _activePhaseStates[phase] = false;

            // agent.OnAgentWieldedItemChange += () =>
            // {
            //     var wieldedWeaponIndex = agent.GetWieldedItemIndex(Agent.HandIndex.MainHand);
            //
            //     if (wieldedWeaponIndex < EquipmentIndex.WeaponItemBeginSlot ||
            //         wieldedWeaponIndex > EquipmentIndex.NumAllWeaponSlots) return;
            //
            //     if (agent.Equipment[wieldedWeaponIndex]
            //             .CurrentUsageItem?.WeaponClass
            //             .Equals(WeaponClass.Musket) ??
            //         false) return;
            //
            //     
            // };
        }

        public void OnTick(float dt)
        {
            if (!_agent.IsHuman) return;

            if (!IsUsingMusket(_agent)) return;
            
            float progress = GetReloadingProgress(_agent);
            var isMusketReloadingActive = progress > 0f;

            if (!isMusketReloadingActive)
            {
                if (_activePhaseStates.ContainsValue(true))
                {
                    _phases.Where(phase => _activePhaseStates[phase]).ToList()
                        .ForEach(phase =>
                        {
                            phase.OnReloadPhaseEnd();
                            _activePhaseStates[phase] = false;
                        });
                    WieldOriginalWeapon();
                }

                return;
            }

            if (_activePhaseStates.All(phaseState => !phaseState.Value)) HideWieldedWeapon();

            // During the transition between two phases (eg. both phases share the same end/start progress value),
            // The weapon of both phases will be visible for a tick.
            // The weapon of the previous transition will be removed on the next tick.
            // This makes the transition more seamless.
            foreach (var phase in _phases)
            {
                if (isMusketReloadingActive && progress >= phase.PhaseProgressStart &&
                    progress <= phase.PhaseProgressEnd)
                {
                    if (!_activePhaseStates[phase])
                    {
                        phase.OnReloadProgress(progress);
                        phase.OnReloadPhaseStart();
                        _activePhaseStates[phase] = true;
                        return;
                    }

                    phase.OnReloadProgress(progress);
                    phase.OnTick(dt);
                }
            }

            foreach (var phase in _phases)
            {
                if (_activePhaseStates[phase] && progress > phase.PhaseProgressEnd)
                {
                    phase.OnReloadPhaseEnd();
                    _activePhaseStates[phase] = false;
                }
            }
        }

        private float GetReloadingProgress(Agent agent)
        {
            var currentActionStage = agent.GetCurrentActionStage(1);
            if (currentActionStage == Agent.ActionStage.ReloadMidPhase)
                return _agent.GetCurrentActionProgress(1) * _baseAnimationDuration / _totalDuration;
            if (currentActionStage == Agent.ActionStage.ReloadLastPhase)
                return (_baseAnimationDuration +
                        _agent.GetCurrentActionProgress(1) * _continuedAnimationDuration) / _totalDuration;
            return 0f;
        }

        private static float GetBaseReloadAnimationDuration()
        {
            return MBAnimation.GetAnimationDuration(ReloadAnimationName);
        }

        private static float GetContinuedReloadAnimationDuration()
        {
            return MBAnimation.GetAnimationDuration(ReloadContinueAnimationName);
        }

        private static bool IsUsingMusket(Agent agent)
        {
            return agent.WieldedWeapon.CurrentUsageItem?.WeaponClass == WeaponClass.Musket;
        }

        private void HideWieldedWeapon()
        {
            var equipmentIndex = _agent.GetWieldedItemIndex(Agent.HandIndex.MainHand);
            var firearmWeaponEntity =
                _agent.GetWeaponEntityFromEquipmentSlot(equipmentIndex);
            firearmWeaponEntity.GetMetaMesh(0).ClearMeshes();

            GameEntity? gameEntity = _weaponEntityRepository.GetWeaponEntity(_agent.Index.ToString());

            var missionWeapon = new MissionWeapon(_agent.WieldedWeapon.Item, null, null);
            var newWeaponEntity = Mission.Current.SpawnWeaponWithNewEntity(ref missionWeapon,
                Mission.WeaponSpawnFlags.None,
                firearmWeaponEntity.GetMetaMesh(0).Frame);

            _weaponEntityRepository.SaveWeaponEntity(newWeaponEntity, _agent.Index.ToString());

            gameEntity?.Remove(0);
        }

        private void WieldOriginalWeapon()
        {
            var firearmEquipmentIndex = _agent.GetWieldedItemIndex(Agent.HandIndex.MainHand);
            var firearmWeapon = _agent.WieldedWeapon;

            GameEntity? weaponEntity = _weaponEntityRepository.GetWeaponEntity(_agent.Index.ToString());

            var frame = MatrixFrame.Identity;
            _agent.AttachWeaponToWeapon(firearmEquipmentIndex,
                new MissionWeapon(firearmWeapon.Item, null, null),
                weaponEntity, ref frame);
        }
    }
}