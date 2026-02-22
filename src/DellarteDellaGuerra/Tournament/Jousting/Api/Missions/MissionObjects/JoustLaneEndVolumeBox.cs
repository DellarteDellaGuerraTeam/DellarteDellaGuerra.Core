using System.Linq;
using DellarteDellaGuerra.Tournament.Jousting.Api.Missions.MissionLogic;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.Tournament.Jousting.Api.Missions.MissionObjects
{
    public class JoustLaneEndVolumeBox : MissionObject
    {
        private JoustFightMissionController _missionController;
        private int _teamIndex = -1;

        protected override void OnInit()
        {
            _missionController = Mission.Current.GetMissionBehavior<JoustFightMissionController>();
            int.TryParse(GameEntity.Name.Split('_').Last(), out _teamIndex);
        }

        public override TickRequirement GetTickRequirement()
        {
            return TickRequirement.TickOccasionally;
        }

        protected override void OnTickOccasionally(float currentFrameDeltaTime)
        {
            if (_missionController != null && _teamIndex >= 0 && _missionController.CurrentState ==
                JoustFightMissionController.JoustFightState.MountedCombat)
            {
                MBList<Agent> agents = new();
                agents = Mission.Current.GetNearbyAgents(GameEntity.GetGlobalFrame().origin.AsVec2, 7, agents);
                if (agents.Any(x => x.Team.TeamIndex == _teamIndex)) _missionController.RestartMatch();
            }
        }
    }
}