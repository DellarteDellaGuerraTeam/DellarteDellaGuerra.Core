using System;
using System.Runtime.CompilerServices;
using DellarteDellaGuerra.Domain.Naval;
using HarmonyLib;
using NavalDLC.Missions;
using NavalDLC.Missions.Objects;
using NavalDLC.Missions.ShipActuators;
using NavalDLC.Missions.ShipControl;
using NavalDLC.Missions.ShipInput;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using Vector2 = System.Numerics.Vector2;

namespace DellarteDellaGuerra.Integration.Naval;

internal static class OarlessShipAI
{
    private sealed class Helm
    {
        public readonly SailingCourse Course = new();
        public bool MakeSail;
        public int OrbitSide;
        public MissionShip? OrbitTarget;
        public float NextTrimCheck;
        public bool HasDrive;
        public Vec2 LastClearWaypoint;
        public Vec2 LastCheckPosition;
        public float RecheckClearanceAt;
    }

    private static readonly ConditionalWeakTable<MissionShip, Helm> Helms = new();
    private static readonly Func<AIShipController, float> Acceleration =
        AccessTools.MethodDelegate<Func<AIShipController, float>>(
            AccessTools.PropertyGetter(typeof(AIShipController), "DesiredLinearAcceleration"));
    private static readonly HarmonyLib.Harmony Patches = new("com.dadg.oarless-ships");

    private static bool _installed;

    internal static void Install()
    {
        if (_installed) return;
        Patches.Patch(AccessTools.Method(typeof(AIShipController), nameof(AIShipController.Update)),
            postfix: Hook(nameof(ChooseSail)));
        Patches.Patch(AccessTools.Method(typeof(AIShipController), nameof(AIShipController.GetNextTarget)),
            postfix: Hook(nameof(ChooseTack)));
        Patches.Patch(AccessTools.Method(typeof(ShipOrder), "UpdateSkirmishOrder"),
            postfix: Hook(nameof(SailPastTarget)));
        _installed = true;
    }

    private static HarmonyMethod Hook(string name) => new(typeof(OarlessShipAI), name);

    internal static bool IsOarless(MissionShip ship) =>
        ship.IsInitialized && ship.IsDeployed && !ship.IsRemoved && !ship.IsSinking &&
        ship.IsAIControlled && ship.MissionShipObject.OarCount == 0 && ship.Sails.Count > 0;

    internal static bool IsMakingSail(MissionShip ship) => Helms.GetOrCreateValue(ship).MakeSail;
    internal static void SetMakeSail(MissionShip ship, bool enabled) => Helms.GetOrCreateValue(ship).MakeSail = enabled;

    private static bool IsUnderway(MissionShip ship)
    {
        if (ship.ShipOrder.MovementOrderEnum == ShipOrder.ShipMovementOrderEnum.Stop ||
            ship.Physics.IsAnchored || ship.GetIsConnected()) return false;
        var target = ship.ShipOrder.GetBoardingTargetShip();
        if (target == null) return true;
        // Allow for both hulls when measuring the final approach between ship origins.
        var reach = 12f + 0.5f * (HullLength(ship) + HullLength(target));
        return (target.GlobalFrame.origin - ship.GlobalFrame.origin).AsVec2.LengthSquared > reach * reach;
    }

    private static void ChooseSail(AIShipController __instance, MissionShip ____ownerShip, ref ShipInputRecord __result)
    {
        var ship = ____ownerShip;
        if (!IsOarless(ship) || ship.HasCustomSailSetting || ship.ShipOrder.EnforceSailUsage != 0) return;
        if (!__instance.HasTarget || !IsUnderway(ship) || __instance.HasArrivedAtTarget(out _, out _))
        {
            __result.SetSail(SailInput.Raised);
            return;
        }

        var helm = Helms.GetOrCreateValue(ship);
        if (helm.MakeSail)
        {
            __result.SetSail(SailInput.Full);
            return;
        }
        var acceleration = Acceleration(__instance);
        var turningFromRest = __result.RudderLateral != 0f && ship.Physics.LinearVelocity.AsVec2.LengthSquared < 4f;
        if (acceleration > 0.01f || (acceleration >= 0f && turningFromRest))
        {
            var time = Mission.Current.CurrentTime;
            if (time >= helm.NextTrimCheck)
            {
                helm.HasDrive = HasForwardThrust(ship);
                helm.NextTrimCheck = time + 0.4f;
            }
            // When caught bow-to-wind, back the sail for steerage. Native rudder physics handles sternway.
            if (helm.HasDrive || turningFromRest) __result.SetSail(SailInput.Full);
        }
    }

    private static bool HasForwardThrust(MissionShip ship)
    {
        var wind = ship.Scene.GetGlobalWindVelocity() - ship.Physics.LinearVelocity.AsVec2;
        if (wind.Normalize() < 0.5f) return false;
        var rotation = ship.GlobalFrame.rotation;
        var localWind = new Vec2(Vec2.DotProduct(wind, rotation.s.AsVec2), Vec2.DotProduct(wind, rotation.f.AsVec2));
        localWind.Normalize();
        float drive = 0f, weight = 0f;
        foreach (var sail in ship.Sails)
        {
            var rig = sail.SailObject;
            var area = sail.Area * rig.ForceMultiplier;
            drive += area * BestForwardCoefficient(rig, localWind);
            weight += area;
        }
        return drive > 0.02f * weight;
    }

    private static float BestForwardCoefficient(ShipSail rig, Vec2 localWind)
    {
        var best = float.MinValue;
        for (int i = 0; i <= 12; i++)
        {
            var angle = -rig.RightRotationLimit + (rig.RightRotationLimit + rig.LeftRotationLimit) * i / 12f;
            var normal = Vec2.Forward;
            normal.RotateCCW(angle);
            best = Math.Max(best, SailWindProfile.Instance.ComputeSailThrustValue(rig.Type, normal, Vec2.Forward, localWind));
        }
        return best;
    }

    // Feed sailing legs to the native trajectory planner, which still controls rudder and collision avoidance.
    private static void ChooseTack(MissionShip ____ownerShip, bool __result,
        ref Vec2 targetPosition, ref Vec2 targetDirection, ref float targetSpeed)
    {
        var ship = ____ownerShip;
        if (!__result || !IsOarless(ship)) return;
        var helm = Helms.GetOrCreateValue(ship);
        if (!IsUnderway(ship) || ship.ShipOrder.EnforceSailUsage < 0 || ship.HasCustomSailSetting)
        {
            helm.Course.Reset();
            return;
        }
        var position = ship.GlobalFrame.origin.AsVec2;
        if (!helm.Course.TryTack(ToVector(position), ToVector(targetPosition), ToVector(ship.GlobalFrame.rotation.f.AsVec2),
            ToVector(ship.Scene.GetGlobalWindVelocity()), HullLength(ship),
            candidate => ClearLeg(ship, ToVec2(candidate)), out var waypoint)) return;
        targetPosition = ToVec2(waypoint);
        targetDirection = (targetPosition - position).Normalized();
        targetSpeed = Math.Max(targetSpeed, ship.MissionShipObject.MaxLinearSpeed * 0.5f);
    }

    private static void SailPastTarget(MissionShip ____ownerShip, MissionShip shipToSkirmish)
    {
        var ship = ____ownerShip;
        if (!IsOarless(ship) || !IsUnderway(ship) || shipToSkirmish.IsSinking || shipToSkirmish.IsSunk) return;
        var offset = (ship.GlobalFrame.origin - shipToSkirmish.GlobalFrame.origin).AsVec2;
        var range = offset.Normalize();
        var radius = Math.Max(60f, HullLength(ship) + HullLength(shipToSkirmish));
        if (range < 1f || range > 2f * radius) return;
        var helm = Helms.GetOrCreateValue(ship);
        if (helm.OrbitTarget != shipToSkirmish)
        {
            helm.OrbitTarget = shipToSkirmish;
            helm.OrbitSide = Vec2.DotProduct(offset.LeftVec(), ship.GlobalFrame.rotation.f.AsVec2) >= 0f ? 1 : -1;
        }
        for (int attempt = 0; attempt < 2; attempt++)
        {
            var turn = attempt == 0 ? helm.OrbitSide : -helm.OrbitSide;
            var waypoint = ToVec2(SailingCourse.SkirmishWaypoint(ToVector(shipToSkirmish.GlobalFrame.origin.AsVec2),
                ToVector(offset), turn, radius));
            if (!ClearLeg(ship, waypoint)) continue;
            helm.OrbitSide = turn;
            var direction = (waypoint - ship.GlobalFrame.origin.AsVec2).Normalized();
            ship.AIController.SetTargetState(new NavalState(waypoint, direction, ship.MissionShipObject.MaxLinearSpeed * 0.5f));
            return;
        }
    }

    private static bool ClearLeg(MissionShip ship, Vec2 destination)
    {
        if (!Mission.Current.IsPositionInsideBoundaries(destination)) return false;
        var helm = Helms.GetOrCreateValue(ship);
        var time = Mission.Current.CurrentTime;
        var position = ship.GlobalFrame.origin;
        if (time < helm.RecheckClearanceAt && (destination - helm.LastClearWaypoint).LengthSquared < 1f &&
            (position.AsVec2 - helm.LastCheckPosition).LengthSquared < 16f) return true;
        bool clear;
        if (ship.ShipsLogic.SeaPathfindingEnabled)
            clear = ClearSeaPath(ship, position, destination);
        else
            clear = ClearWater(ship, position.AsVec2, destination);
        if (!clear) return false;
        helm.LastClearWaypoint = destination;
        helm.LastCheckPosition = position.AsVec2;
        helm.RecheckClearanceAt = time + 0.5f;
        return true;
    }

    private static bool ClearWater(MissionShip ship, Vec2 start, Vec2 destination)
    {
        if (!ship.Scene.HasTerrainHeightmap) return true;
        var delta = destination - start;
        var beam = Math.Max(2f, ship.Physics.PhysicsBoundingBoxSizeWithoutChildren.x);
        var steps = Math.Max(1, (int)Math.Ceiling(delta.Length / (beam * 0.5f)));
        var edge = delta.Normalized().LeftVec() * (beam * 0.5f);
        for (var i = 1; i <= steps; i++)
        {
            var point = start + delta * ((float)i / steps);
            var bottom = Math.Max(ship.Scene.GetTerrainHeight(point),
                Math.Max(ship.Scene.GetTerrainHeight(point - edge), ship.Scene.GetTerrainHeight(point + edge)));
            if (ship.Scene.GetWaterLevelAtPosition(point, true, false) - bottom <= ship.MissionShipObject.LandingDepth)
                return false;
        }
        return true;
    }

    private static bool ClearSeaPath(MissionShip ship, Vec3 position, Vec2 destination)
    {
        var scene = ship.Scene;
        // War Sails enables the sea faces only for the duration of its navigation queries.
        scene.SetAbilityOfFacesWithId(1, true);
        try
        {
            var face = scene.GetNearestNavigationMeshForPosition(position, 1000000f, true);
            return face != UIntPtr.Zero && scene.IsLineToPointClear(face, position.AsVec2, destination,
                0.5f * ship.Physics.PhysicsBoundingBoxSizeWithoutChildren.x);
        }
        finally { scene.SetAbilityOfFacesWithId(1, false); }
    }

    private static float HullLength(MissionShip ship) => Math.Max(5f, ship.Physics.PhysicsBoundingBoxSizeWithoutChildren.y);
    private static Vector2 ToVector(Vec2 v) => new(v.x, v.y);
    private static Vec2 ToVec2(Vector2 v) => new(v.X, v.Y);
}
