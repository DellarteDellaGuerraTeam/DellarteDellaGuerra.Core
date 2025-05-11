using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DellarteDellaGuerra.Firearm.Reload;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.Firearms
{
    public class FirearmReloadMissionLogic : MissionLogic
    {
        private readonly Dictionary<int, ReloadComponent> _reloadComponentByAgent = new();

        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            base.OnAgentBuild(agent, banner);
            if (agent.IsHuman)
                _reloadComponentByAgent.Add(agent.GetHashCode(),
                    new ReloadComponent(InitialiseReloadPhases(agent), agent));

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
                        var weapon = agent.Equipment[index];
                        agent.RemoveEquippedWeapon(index);
                        agent.EquipWeaponWithNewEntity(index, ref weapon);
                    }
            };
        }

        private List<IReloadPhase> InitialiseReloadPhases(Agent agent)
        {
            var reloadPhaseTypes = Assembly.GetExecutingAssembly()
                .ExportedTypes
                .Where(type => typeof(IReloadPhase).IsAssignableFrom(type) &&
                               type.GetConstructor(new[] { typeof(Agent) }) != null);

            return reloadPhaseTypes
                .Select(type => (IReloadPhase)Activator.CreateInstance(type, agent))
                .ToList();
        }
        
        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
            _reloadComponentByAgent.Values.ToList().ForEach(component => component.OnTick(dt));
        }
    }
}