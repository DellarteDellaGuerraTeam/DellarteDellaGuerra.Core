using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.Integration.Naval;

public sealed class NavalRaidSailMissionLogic : MissionLogic
{
    // NavalRaidMissionController lives in the optional NavalDLC assembly, so it is only reached through reflection.
    private const float MinimumLandingTailwind = 8f;

    public override void OnDeploymentFinished()
    {
        var raid = Mission.MissionBehaviors.FirstOrDefault(b => b.GetType().Name == "NavalRaidMissionController");
        if (raid == null) return;

        var raidType = raid.GetType();
        var shipsLogic = raidType.GetField("_shipsLogic").GetValue(raid);
        var landingFrames = (MatrixFrame[][])raidType.GetField("_landingFrames").GetValue(raid);
        var ships = (IEnumerable<MissionObject>)shipsLogic.GetType().GetProperty("AllShips").GetValue(shipsLogic);

        var approach = Vec2.Zero;
        foreach (var ship in ships)
        {
            var type = ship.GetType();
            var hull = (MissionShipObject)type.GetProperty("MissionShipObject").GetValue(ship);
            if (hull.OarCount != 0 || !hull.HasSails) continue;

            // The native raid furls every sail and relies on oars for the approach.
            var order = type.GetProperty("ShipOrder").GetValue(ship);
            order.GetType().GetMethod("SetEnforcedSailUsage").Invoke(order, new object[] { 1 });
            var index = (int)type.GetProperty("Index").GetValue(ship);
            var landing = landingFrames[index][0].origin.AsVec2;
            approach += (landing - ship.GameEntity.GlobalPosition.AsVec2).Normalized();
        }

        // The scripted landing lanes give square-rigged ships no room to tack.
        if (!approach.IsNonZero()) return;
        var direction = approach.Normalized();
        var wind = Mission.Scene.GetGlobalWindVelocity();
        if (Vec2.DotProduct(wind, direction) < MinimumLandingTailwind)
            Mission.Scene.SetGlobalWindVelocity(direction * MinimumLandingTailwind);
    }
}
