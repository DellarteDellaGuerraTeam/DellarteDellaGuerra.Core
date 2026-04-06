# Field Battle Cannon Placement Plan

## Goal
Enable players to place DADG cannons during the deployment phase of normal field battles (non-siege), without using siege deployment handlers or static siege deployment points.

## Scope
- In scope:
  - Player-driven cannon placement during `MissionMode.Deployment` in non-siege battles.
  - Placement constrained to mission deployment boundaries.
  - Runtime spawning of cannon prefabs (not scene-placed spawners).
  - Deployment limit enforcement.
- Out of scope (phase 1):
  - Campaign economy integration for cannon counts.
  - Multiplayer support.
  - Siege mission behavior changes.

## Architecture Direction
1. Keep native field battle deployment flow (`BattleDeploymentHandler` etc.) unchanged.
2. Add a dedicated DADG mission behavior/view for cannon placement mode.
3. Use `Mission.CreateMissionObjectFromPrefab(...)` to spawn `dadg_<cannonId>` directly.
4. Validate placement via `Mission.DeploymentPlan.IsPositionInsideDeploymentBoundaries(...)`.
5. Keep existing `CannonTeamMissionLogic` for `Deployment -> Battle` team assignment and forced use.

## Implementation Phases

### Phase 1 - Input and Placement Loop
1. Add `FieldBattleCannonPlacementMissionBehavior` under `Integration/SiegeEngines/Mission/Battle`.
2. Register behavior in DI and add in `SubModule.OnBeforeMissionBehaviorInitialize`.
3. Gate behavior to non-siege deployment missions only.
4. Implement controls:
   - Toggle placement mode.
   - Cycle cannon type.
   - Rotate preview.
   - Confirm placement.
   - Cancel placement mode.
5. Suspend `OrderTroopPlacer` while cannon placement mode is active; restore when inactive.

### Phase 2 - Validation and Spawn
1. Implement cursor raycast to terrain.
2. Add validity checks:
   - Ground hit exists.
   - Inside deployment boundary.
   - Limit not exceeded.
3. Add ghost preview entity (`dadg_<cannonId>_ghost`) and update frame each tick.
4. On confirm:
   - Spawn with `Mission.CreateMissionObjectFromPrefab("dadg_<cannonId>", frame, ...)`.
   - Resolve `GenericCannon` script from spawned entity hierarchy.
   - Set side to player side.
   - Mark all `ISpawnable` scripts in spawned tree as spawned-from-spawner.

### Phase 3 - Limits and Editing
1. Add config entries in `config/dadg.config.xml` for field-battle placement limits.
2. Track placed cannon count for player side during deployment.
3. Block placement when limit reached and show feedback.
4. Add remove/reposition action for deployment phase.

### Phase 4 - Validation and Hardening
1. Manual test matrix:
   - Can enter/exit placement mode during deployment.
   - Cannot place outside deployment boundary.
   - Can place up to limit only.
   - Placed cannons survive until battle start.
   - Existing deployment->battle cannon team assignment still works.
2. Regression checks:
   - No effect on siege deployment UI/logic.
   - No effect on missions without deployment mode.
3. Add targeted automated tests where practical for utility logic (limit/config/validation helpers).

## Risks and Mitigations
- Risk: input conflicts with formation deployment.
  - Mitigation: explicit mode toggle and temporary `OrderTroopPlacer` suspension.
- Risk: prefab-spawned cannon scripts differ from spawner path.
  - Mitigation: call `SetSpawnedFromSpawner()` for all `ISpawnable` components on spawned entity tree.
- Risk: unclear business rule for cannon limit.
  - Mitigation: configurable default limit in phase 1, integrate campaign logic later.

## Deliverable Definition (Phase 1 Done)
- In a normal field battle deployment phase, player can place at least one cannon prefab at a valid location inside deployment boundaries, and the cannon is usable by the correct side after deployment ends.
