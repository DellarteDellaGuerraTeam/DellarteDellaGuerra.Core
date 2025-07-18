using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.Firearm.Reload
{
    public class ReloadComponent : ITickable
    {
        private readonly List<IReloadPhase> _phases;
        private readonly Agent _agent;
        private readonly Dictionary<IReloadPhase, bool> _activePhaseStates = new();

        private readonly float _baseAnimationDuration = GetBaseReloadAnimationDuration();
        private readonly float _continuedAnimationDuration = GetContinuedReloadAnimationDuration();

        private readonly float _totalDuration;

        private float _tickAccum;

        private const string ReloadAnimationName = "reload_firearm";
        private const string ReloadContinueAnimationName = "firearm_reload_continue";

        public ReloadComponent(List<IReloadPhase> phases, Agent agent)
        {
            _phases = phases;
            _agent = agent;
            _totalDuration = _baseAnimationDuration + _continuedAnimationDuration;

            foreach (var phase in _phases)
                _activePhaseStates[phase] = false;

            agent.OnAgentWieldedItemChange += () =>
            {
                var wieldedWeaponIndex = agent.GetWieldedItemIndex(Agent.HandIndex.MainHand);

                if (wieldedWeaponIndex < EquipmentIndex.WeaponItemBeginSlot ||
                    wieldedWeaponIndex > EquipmentIndex.NumAllWeaponSlots) return;

                if (agent.Equipment[wieldedWeaponIndex]
                        .CurrentUsageItem?.WeaponClass
                        .Equals(WeaponClass.Musket) ??
                    false) return;

                for (EquipmentIndex index = EquipmentIndex.WeaponItemBeginSlot;
                     index < EquipmentIndex.NumAllWeaponSlots;
                     index++)
                    if (agent.Equipment[index].CurrentUsageItem?.WeaponClass.Equals(WeaponClass.Musket) ?? false)
                    {
                        _phases.Where(phase => _activePhaseStates[phase]).ToList()
                            .ForEach(activePhase => activePhase.OnReloadPhaseEnd());

                        var weapon = agent.Equipment[index];
                        agent.RemoveEquippedWeapon(index);
                        agent.EquipWeaponWithNewEntity(index, ref weapon);
                    }
            };
        }

        public void OnTick(float dt)
        {
            _tickAccum += dt;
            if (_tickAccum < 0.02f) return;
            _tickAccum = 0;

            if (!_agent.IsHuman || !IsUsingMusket(_agent)) return;

            float progress = GetReloadingProgress(_agent);
            var isReloadingActive = IsReloadingActive(_agent);

            // During the transition between two phases (eg. both phases share the same end/start progress value),
            // The weapon of both phases will be visible for a tick.
            // The weapon of the previous transition will be removed on the next tick.
            // This makes the transition more seamless.
            foreach (var phase in _phases)
            {
                if (isReloadingActive && progress >= phase.PhaseProgressStart && progress <= phase.PhaseProgressEnd)
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
                if (_activePhaseStates[phase] && (!isReloadingActive || progress > phase.PhaseProgressEnd))
                {
                    phase.OnReloadPhaseEnd();
                    _activePhaseStates[phase] = false;
                }
            }
        }

        private bool IsReloadingActive(Agent agent)
        {
            return agent.GetCurrentActionType(1) == Agent.ActionCodeType.Reload;
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
            var index = agent.GetWieldedItemIndex(Agent.HandIndex.MainHand);
            return index is >= EquipmentIndex.WeaponItemBeginSlot and < EquipmentIndex.NumAllWeaponSlots &&
                   agent.Equipment[index].CurrentUsageItem?.WeaponClass == WeaponClass.Musket;
        }
    }
}