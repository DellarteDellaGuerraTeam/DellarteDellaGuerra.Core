using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.Firearm.Reload
{
    public class ReloadComponent : ITickable
    {
        private readonly List<IReloadPhase> _phases;
        private readonly Agent _agent;
        private readonly Dictionary<IReloadPhase, bool> _phaseStates = new();

        private float _tickAccum;

        private const string ReloadAnim = "act_reload_firearm";

        public ReloadComponent(List<IReloadPhase> phases, Agent agent)
        {
            _phases = phases;
            _agent = agent;

            foreach (var phase in _phases)
                _phaseStates[phase] = false;
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
                    if (_phaseStates[phase])
                    {
                        phase.OnReloadEnd();
                        _phaseStates[phase] = false;
                    }

                    continue;
                }

                if (progress >= phase.ReloadingProgressStart)
                {
                    if (!_phaseStates[phase])
                    {
                        phase.OnReloadStart();
                        _phaseStates[phase] = true;
                        return;
                    }

                    phase.OnReloadProgress(progress);
                    phase.OnTick(dt);
                }
            }
        }

        private bool IsReloadingActive()
        {
            return _agent.GetCurrentAction(1)?.Name == ReloadAnim;
        }

        private float GetReloadingProgress()
        {
            return IsReloadingActive() ? _agent.GetCurrentActionProgress(1) : 0f;
        }

        private static bool IsUsingMusket(Agent agent)
        {
            var index = agent.GetWieldedItemIndex(Agent.HandIndex.MainHand);
            return index is >= EquipmentIndex.WeaponItemBeginSlot and < EquipmentIndex.NumAllWeaponSlots &&
                   agent.Equipment[index].CurrentUsageItem?.WeaponClass == WeaponClass.Musket;
        }
    }
}