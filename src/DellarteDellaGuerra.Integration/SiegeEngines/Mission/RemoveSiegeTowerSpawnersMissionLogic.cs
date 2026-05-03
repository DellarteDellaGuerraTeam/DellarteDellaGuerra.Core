using System.Linq;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.Integration.SiegeEngines.Mission;

public class RemoveSiegeTowerSpawnersMissionLogic : MissionLogic
{
    // SiegeTowerSpawner entities in siege scenes always spawn a SiegeTower at scene load,
    // even though DadgSiegeEventModel returns an empty list for tower siege engines so they
    // are never deployed. These invisible towers cause two problems:
    //   1. BehaviorShootFromSiegeTower caches _siegeTower from ActiveMissionObjects in its
    //      constructor. If _siegeTower != null && !IsDestroyed, it forces a stop movement
    //      order every tick, freezing the attacker AI indefinitely.
    //   2. MissionGauntletSiegeEngineMarker.OnDeploymentFinished() scans ActiveMissionObjects
    //      to build its HUD marker list — producing a persistent green icon.
    // SetDisabledSynched() in AfterStart() fires before formation behaviors are constructed,
    // removes the tower from ActiveMissionObjects (so the behavior cache misses), sets
    // IsDisabled = true (so BehaviorAssaultWalls skips the tower), and hides the entity.
    public override void AfterStart()
    {
        base.AfterStart();

        Mission.ActiveMissionObjects
            .OfType<SiegeTower>()
            .ToList()
            .ForEach(t => t.SetDisabledSynched());
    }
}