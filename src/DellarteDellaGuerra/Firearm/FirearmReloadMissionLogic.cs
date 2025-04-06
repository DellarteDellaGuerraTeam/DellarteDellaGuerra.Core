using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.Firearms
{
    public class FirearmReloadMissionLogic : MissionLogic
    {
        private readonly Dictionary<int, FirearmReloadAnimationComponent> _components = new();

        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            base.OnAgentBuild(agent, banner);
            if (agent.IsHuman)
                _components.Add(agent.GetHashCode(), new FirearmReloadAnimationComponent(agent));

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

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
            _components.Values.ToList().ForEach(component => component.OnTickAsAI(dt));
        }
    }
}