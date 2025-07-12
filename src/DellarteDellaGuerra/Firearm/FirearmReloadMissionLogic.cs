using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Firearm.Reload;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.Firearm
{
    public class FirearmReloadMissionLogic : MissionLogic
    {
        private readonly Dictionary<int, ReloadComponent> _reloadComponentByAgent = new();
        private readonly ILoggerFactory _loggerFactory;

        public FirearmReloadMissionLogic(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }

        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            base.OnAgentBuild(agent, banner);
            if (agent.IsHuman)
                _reloadComponentByAgent.Add(agent.GetHashCode(),
                    new ReloadComponent(InitialiseReloadPhases(agent), agent));
        }

        private List<IReloadPhase> InitialiseReloadPhases(Agent agent)
        {
            var reloadPhases = new List<IReloadPhase>
            {
                new ReloadStartComponent(agent),
                new InitialHandSwapReloadComponent(agent),
                new BlackPowderReloadComponent(agent),
                new RammingReloadComponent(agent, _loggerFactory),
                new ReloadStopComponent(agent)
            };
            return reloadPhases;
        }
        
        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
            _reloadComponentByAgent.Values.ToList().ForEach(component => component.OnTick(dt));
        }
    }
}