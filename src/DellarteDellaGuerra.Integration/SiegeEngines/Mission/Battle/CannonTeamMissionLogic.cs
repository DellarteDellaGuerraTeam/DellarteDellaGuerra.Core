using System.Linq;
using DellarteDellaGuerra.Integration.SiegeEngines.Mission.Siege.Spawn;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.Integration.SiegeEngines.Mission.Battle;

public class CannonTeamMissionLogic : MissionLogic
{
    public override void OnAddTeam(TaleWorlds.MountAndBlade.Team team)
    {
        base.OnAddTeam(team);

        TaleWorlds.MountAndBlade.Mission.Current.ActiveMissionObjects
            .OfType<GenericCannon>()
            .Where(script => script.Side.Equals(BattleSideEnum.Attacker) && team.IsAttacker)
            .ToList()
            .ForEach(script =>
            {
                script.Team = team;
                script.SetForcedUse(true);
            });

        TaleWorlds.MountAndBlade.Mission.Current.ActiveMissionObjects
            .OfType<GenericCannon>()
            .Where(script => script.Side.Equals(BattleSideEnum.Defender) && team.IsDefender)
            .ToList()
            .ForEach(script =>
            {
                script.Team = team;
                script.SetForcedUse(true);
            });
    }
}