using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.Firearm.Reload
{
    public class ReloadComponent : ITickable, IOnAgentBuild, IOnFirearmDropped, IOnAgentRemoved
    {
        private readonly IWeaponEntityRepository _weaponEntityRepository;
        private readonly List<IReloadPhase> _phases;
        private readonly Agent _agent;
        private readonly Dictionary<IReloadPhase, bool> _activePhaseStates = new();

        private GameEntity _emptyGameEntity;

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
        }

        public void OnTick(float dt)
        {
            if (!_agent.IsHuman) return;

            if (!IsUsingMusket(_agent) && _activePhaseStates.All(activePhaseState => !activePhaseState.Value)) return;
            
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

        public void OnAgentBuild()
        {
            _phases.ForEach(phase => phase.OnAgentBuild());
        }

        public void OnAgentRemoved()
        {
            _phases.ForEach(phase => phase.OnAgentRemoved());
        }

        public void OnFirearmDropped(SpawnedItemEntity spawnedItemEntity)
        {
            WeaponEntity? weaponEntity = _weaponEntityRepository.GetWeaponEntity(_agent.Index.ToString());
            if (weaponEntity?.GameEntity == spawnedItemEntity.GameEntity)
            {
                ResetFirearmMetaMeshToWeaponEntity();
                _weaponEntityRepository.Remove(_agent.Index.ToString());
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
            var equipmentIndex = GetFirearmEquipmentIndex();
            var originalWeaponEntity = _agent.GetWeaponEntityFromEquipmentSlot(equipmentIndex);
            WeaponEntity? weaponEntity = _weaponEntityRepository.GetWeaponEntity(_agent.Index.ToString());

            if (weaponEntity is null)
                _weaponEntityRepository.SaveWeaponEntity(
                    new WeaponEntity(originalWeaponEntity,
                        originalWeaponEntity.GetMetaMesh(0)), _agent.Index.ToString());
            originalWeaponEntity.RemoveMultiMesh(originalWeaponEntity.GetMetaMesh(0));
            _emptyGameEntity = GameEntity.CreateEmpty(Mission.Current.Scene);
            _agent.AgentVisuals.AddChildEntity(_emptyGameEntity);
        }

        private EquipmentIndex GetFirearmEquipmentIndex()
        {
            EquipmentIndex equipmentIndex = EquipmentIndex.None;
            for (EquipmentIndex index = EquipmentIndex.WeaponItemBeginSlot;
                 index < EquipmentIndex.NumAllWeaponSlots;
                 index++)
                if (_agent.Equipment[index].CurrentUsageItem?.WeaponClass.Equals(WeaponClass.Musket) ?? false)
                    equipmentIndex = index;

            return equipmentIndex;
        }

        private void WieldOriginalWeapon()
        {
            ResetFirearmMetaMeshToWeaponEntity();
            _agent.AgentVisuals.RemoveChildEntity(_emptyGameEntity, 14);
        }

        private void ResetFirearmMetaMeshToWeaponEntity()
        {
            WeaponEntity? weaponEntity = _weaponEntityRepository.GetWeaponEntity(_agent.Index.ToString());

            if (weaponEntity is null) return;

            weaponEntity.GameEntity.AddMultiMesh(weaponEntity.MetaMesh);
        }
    }
}