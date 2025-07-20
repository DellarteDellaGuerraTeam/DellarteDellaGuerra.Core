using System.Collections.Generic;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Firearm.Reload;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.Firearm
{
    public class FirearmReloadMissionLogic : MissionLogic
    {
        private readonly Dictionary<int, ReloadComponent> _reloadComponentByAgent = new();
        private readonly Dictionary<int, int> _agentSkipTickCounter = new();
        private readonly List<Agent> _activeHumanAgents = new();

        private readonly ILoggerFactory _loggerFactory;
        private readonly IWeaponEntityRepository _weaponEntityRepository;

        private readonly float _viewAngle = 110f;
        private readonly float _cosViewAngleThreshold;

        public FirearmReloadMissionLogic(ILoggerFactory loggerFactory, IWeaponEntityRepository weaponEntityRepository)
        {
            _loggerFactory = loggerFactory;
            _weaponEntityRepository = weaponEntityRepository;
            _cosViewAngleThreshold = MathF.Cos(_viewAngle * 0.5f * (MathF.PI / 180f));
        }

        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            base.OnAgentBuild(agent, banner);
            if (agent.IsHuman)
            {
                ItemObject? fireItemObject = FindFirstFirearm(agent);
                if (fireItemObject is null) return;

                int id = agent.Index;
                _reloadComponentByAgent[id] = new ReloadComponent(InitialiseReloadPhases(agent, fireItemObject), agent,
                    _weaponEntityRepository);
                _reloadComponentByAgent[id].OnAgentBuild();
                _agentSkipTickCounter[id] = 0;
                _activeHumanAgents.Add(agent);
            }
        }

        private List<IReloadPhase> InitialiseReloadPhases(Agent agent, ItemObject firearmItem)
        {
            return new List<IReloadPhase>
            {
                new InitialHandSwapReloadComponent(agent, firearmItem),
                new BlackPowderReloadComponent(agent, firearmItem),
                new RammingReloadComponent(agent, firearmItem, _loggerFactory)
            };
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
            if (Mission.Current == null)
                return;

            var cameraFrame = Mission.Current.GetCameraFrame();
            Vec3 camPos = cameraFrame.origin;
            Vec3 camForward = -cameraFrame.rotation.u.NormalizedCopy();

            foreach (var agent in _activeHumanAgents)
            {
                if (ShouldSkipAgent(agent))
                    continue;

                int id = agent.Index;

                if (!IsAgentVisible(camPos, camForward, agent))
                {
                    ResetSkipCounter(id);
                    continue;
                }

                float distance = (agent.Position - camPos).LengthSquared;
                int skipTicks = GetSkipTicksForDistanceSquared(distance);

                if (ShouldSkipTick(id, skipTicks))
                    continue;

                ResetSkipCounter(id);
                ApplyReloading(agent, dt);
            }
        }

        private bool ShouldSkipAgent(Agent agent)
        {
            return !agent.IsHuman || !agent.IsActive() || agent.Health <= 0;
        }

        private void ResetSkipCounter(int agentId)
        {
            _agentSkipTickCounter[agentId] = 0;
        }

        private bool ShouldSkipTick(int agentId, int skipTicks)
        {
            if (_agentSkipTickCounter[agentId] < skipTicks)
            {
                _agentSkipTickCounter[agentId]++;
                return true;
            }

            return false;
        }

        private int GetSkipTicksForDistanceSquared(float distanceSq)
        {
            if (distanceSq < 100f) return 0; // <10m
            if (distanceSq < 900f) return 1; // <30m
            if (distanceSq < 2500f) return 2; // <50m
            if (distanceSq < 4900f) return 3; // <70m
            if (distanceSq < 8100f) return 4; // <90m
            return 5;
        }

        private bool IsAgentVisible(Vec3 cameraPos, Vec3 cameraForward, Agent agent)
        {
            Vec3 toAgent = agent.Position - cameraPos;
            float dot = Vec3.DotProduct(toAgent.NormalizedCopy(), cameraForward);
            dot = MathF.Clamp(dot, -1f, 1f);
            return dot >= _cosViewAngleThreshold;
        }

        private void ApplyReloading(Agent agent, float dt)
        {
            _reloadComponentByAgent[agent.Index].OnTick(dt);
        }

        private ItemObject? FindFirstFirearm(Agent agent)
        {
            ItemObject? firearmItem = null;
            for (EquipmentIndex index = EquipmentIndex.WeaponItemBeginSlot;
                 index < EquipmentIndex.NumAllWeaponSlots;
                 index++)
                if (agent.Equipment[index].CurrentUsageItem?.WeaponClass.Equals(WeaponClass.Musket) ?? false)
                    firearmItem = agent.Equipment[index].Item;

            return firearmItem;
        }
    }
}
