using System.Collections.Generic;
using DellarteDellaGuerra.Integration.SiegeEngines.Mission.Siege.Spawn;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.Integration.SiegeEngines.Mission.Siege.Reposition;

/// <summary>
///     Allows cannon crews (and the player) to reposition a cannon during a siege battle
///     by occupying a scene-placed <c>dadg_cannon_reposition_target</c> entity.
///     How it works:
///     - Level designers place one or more <c>dadg_cannon_reposition_target</c> prefabs in a siege
///     scene at positions where a cannon could usefully be moved (e.g. tower corners).
///     - Each prefab carries a <c>StandingPoint</c> script tagged <c>cannon_reposition_target</c>.
///     - Targets are deactivated on battle start and are activated automatically when an idle
///     cannon is within <see cref="ActivationRange" /> metres.
///     - While a target is occupied the nearest idle cannon's root entity is translated toward
///     the target at <see cref="MoveSpeed" /> m/s (XY only — Z is preserved).
///     - When the cannon root is within <see cref="ArrivalThreshold" /> metres of the target the
///     occupant is evicted and the target is deactivated until re-entered.
/// </summary>
public class CannonPushMissionBehavior : MissionLogic
{
    private const string RepositionTargetTag = "cannon_reposition_target";

    /// <summary>Maximum distance at which a reposition target is activated for a nearby cannon.</summary>
    private const float ActivationRange = 15f;

    /// <summary>Movement speed of the cannon while being repositioned, in metres per second.</summary>
    private const float MoveSpeed = 1.5f;

    /// <summary>XY distance at which the cannon is considered to have arrived at the target.</summary>
    private const float ArrivalThreshold = 0.5f;

    private readonly List<StandingPoint> _targets = new();
    private readonly List<GenericCannon> _cannons = new();

    // ── Lifecycle ────────────────────────────────────────────────────────────

    /// <summary>
    ///     Called on the Deployment→Battle transition — the earliest reliable point at which
    ///     all scene entities (cannons and reposition targets) are fully initialised.
    /// </summary>
    public override void OnMissionModeChange(MissionMode oldMissionMode, bool atStart)
    {
        base.OnMissionModeChange(oldMissionMode, atStart);

        if (oldMissionMode != MissionMode.Deployment || Mission.Mode != MissionMode.Battle)
            return;

        foreach (var mobj in Mission.ActiveMissionObjects)
            if (mobj is StandingPoint sp && sp.GameEntity.HasTag(RepositionTargetTag))
            {
                _targets.Add(sp);
                sp.SetIsDeactivatedSynched(true);
            }
            else if (mobj is GenericCannon cannon)
            {
                _cannons.Add(cannon);
            }
    }

    // ── Per-frame update ─────────────────────────────────────────────────────

    public override void OnMissionTick(float dt)
    {
        if (_targets.Count == 0)
            return;

        foreach (var target in _targets)
        {
            GenericCannon? nearest = FindNearestIdleCannon(target);

            if (nearest == null)
            {
                if (!target.IsDeactivated)
                    EvictAndDeactivate(target);
                continue;
            }

            if (target.IsDeactivated)
                target.SetIsDeactivatedSynched(false);

            if (!target.HasUser)
                continue;

            // Move cannon toward target (XY only).
            Vec3 targetPos = target.GameEntity.GetGlobalFrame().origin;
            Vec3 cannonRootPos = GetCannonRootPos(nearest);
            float dxSq = (targetPos.x - cannonRootPos.x) * (targetPos.x - cannonRootPos.x)
                         + (targetPos.y - cannonRootPos.y) * (targetPos.y - cannonRootPos.y);

            if (dxSq <= ArrivalThreshold * ArrivalThreshold)
            {
                EvictAndDeactivate(target);
                continue;
            }

            Vec3 diff = new Vec3(targetPos.x - cannonRootPos.x, targetPos.y - cannonRootPos.y);
            Vec3 dir = diff.NormalizedCopy();
            TranslateCannonRoot(nearest, dir, dt);
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private GenericCannon? FindNearestIdleCannon(StandingPoint target)
    {
        Vec3 targetPos = target.GameEntity.GetGlobalFrame().origin;
        float bestDistSq = ActivationRange * ActivationRange;
        GenericCannon? best = null;

        foreach (var cannon in _cannons)
        {
            if (cannon.IsDestroyed || cannon.IsDeactivated)
                continue;
            if (cannon.State != RangedSiegeWeapon.WeaponState.Idle || cannon.PilotAgent != null)
                continue;

            Vec3 pos = GetCannonRootPos(cannon);
            float distSq = (pos.x - targetPos.x) * (pos.x - targetPos.x)
                           + (pos.y - targetPos.y) * (pos.y - targetPos.y);
            if (distSq < bestDistSq)
            {
                bestDistSq = distSq;
                best = cannon;
            }
        }

        return best;
    }

    private static Vec3 GetCannonRootPos(GenericCannon cannon)
    {
        return cannon.GameEntity.Parent.GetGlobalFrame().origin;
    }

    /// <summary>
    ///     Translates the cannon root (dadg_&lt;id&gt;, the machine_parent) toward <paramref name="dir" />.
    ///     All child entities move with it automatically. Z is preserved so the cannon stays on its surface.
    /// </summary>
    private static void TranslateCannonRoot(GenericCannon cannon, Vec3 dir, float dt)
    {
        GameEntity root = cannon.GameEntity.Parent;
        MatrixFrame frame = root.GetGlobalFrame();
        float delta = MoveSpeed * dt;
        frame.origin.x += dir.x * delta;
        frame.origin.y += dir.y * delta;
        root.SetGlobalFrame(in frame);
    }

    private static void EvictAndDeactivate(StandingPoint sp)
    {
        if (sp.HasUser)
            sp.UserAgent.StopUsingGameObject();
        sp.SetIsDeactivatedSynched(true);
    }
}