using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.Firearm.Reload
{
    public class ReloadComponent : ITickable
    {
        private readonly List<IReloadPhase> _phases;
        private readonly Agent _agent;
        private readonly Dictionary<IReloadPhase, bool> _activePhaseStates = new();

        private float _tickAccum;

        private const string ReloadAnimationName = "reload_firearm";
        private const string ReloadContinueAnimationName = "firearm_reload_continue";

        private const string ReloadAnimationId = "act_reload_firearm";
        private const string ReloadContinueAnimationId = "act_reload_firearm_continue";

        public ReloadComponent(List<IReloadPhase> phases, Agent agent)
        {
            _phases = phases;
            _agent = agent;

            foreach (var phase in _phases)
                _activePhaseStates[phase] = false;
        }

        public void OnTick(float dt)
        {
            _tickAccum += dt;
            if (_tickAccum < 1f) return;

            if (!_agent.IsHuman || !IsUsingMusket(_agent)) return;

            float progress = GetReloadingProgress();

            foreach (var phase in _phases)
            {
                if (!IsReloadingActive() || progress > phase.ReloadingProgressEnd)
                {
                    if (_activePhaseStates[phase])
                    {
                        phase.OnReloadEnd();
                        _activePhaseStates[phase] = false;
                    }

                    continue;
                }

                if (progress >= phase.ReloadingProgressStart)
                {
                    if (!_activePhaseStates[phase])
                    {
                        phase.OnReloadStart();
                        _activePhaseStates[phase] = true;
                        return;
                    }

                    phase.OnReloadProgress(progress);
                    phase.OnTick(dt);
                }
            }
        }

        private bool IsReloadingActive()
        {
            return _agent.GetCurrentAction(1)?.Name == ReloadAnimationId ||
                   _agent.GetCurrentAction(1)?.Name == ReloadContinueAnimationId;
        }

        private float GetReloadingProgress()
        {
            float totalDuration = GetBaseReloadAnimationDuration() + GetContinuedReloadAnimationDuration();

            if (_agent.GetCurrentAction(1)?.Name == ReloadAnimationId)
                return _agent.GetCurrentActionProgress(1) * GetBaseReloadAnimationDuration() / totalDuration;
            if (_agent.GetCurrentAction(1)?.Name == ReloadContinueAnimationId)
                return (GetBaseReloadAnimationDuration() +
                        _agent.GetCurrentActionProgress(1) * GetContinuedReloadAnimationDuration()) / totalDuration;
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