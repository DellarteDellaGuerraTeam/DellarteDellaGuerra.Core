# Native keep-fight contract

Source behavior below was verified against the 1.4.6 decompiled source. Shipped SandBox comparison assets were inspected locally; during the London conversion, installed Native/SandBox manifests reported 1.4.8. Do not equate the source and asset versions. Recheck the installed version and model overrides before authoring; these names are useful source-search targets. The [London example](london-example.md) records the subsequent user playtest separately.

## Mission and campaign

`MenuHelper.LordsHallTroopRosterManageDone` resets the battle state and opens the besieged settlement's `lordshall` location scene for its current wall level. `SandBoxMissions.OpenSiegeLordsHallFightMission` loads that scene with `siege`, `LordsHallFightMissionController`, and `NoTeamAI`. It creates a new mission from campaign troop rosters; it does not transfer the same agents from the outer siege.

Native attacker-side outer-siege `BattleEndLogic` can enable enemy defender pullback. Preparing a hall does not implement an automatic retreat-to-keep campaign flow for a player defender. Do not change campaign code just to satisfy scene authoring.

`DefaultSiegeLordsHallFightModel` in 1.4.6:

| Property | Default |
|---|---:|
| MaxDefenderSideTroopCount | 27 |
| MaxAttackerSideTroopCount | 19 |
| AttackerDefenderTroopCountRatio | 0.7 |
| DefenderMaxArcherRatio | 0.7 |
| MaxDefenderArcherCount | Rounded defender maximum × archer ratio |
| AreaLostRatio | 3 |
| DefenderTroopNumberForSuccessfulPullBack | 20 |

SandBox caps mission totals to supplied rosters and uses the model's ranged limit. The controller dismounts troops, spawns attackers in formations, and supplies five-attacker reinforcement waves after five attacker removals. Do not equate a helper count with a troop model override.

## Defender grouping and failure cases

`FightAreaMarker` inherits `AreaMarker`. The controller gathers active marker script instances and sorts by `AreaIndex`; it groups them by `AreaIndex` then `SubAreaIndex`. Marker names alone have no effect.

`AreaMarker.IsPositionInRange` and `GetGameEntitiesWithTagInRange` compare squared global 3D distance to squared `AreaRadius`. A large sphere can collect a position on another storey or across a wall. The script does not perform a room/line-of-sight test.

`AreaData.AddAreaMarker` gathers `defender_archer` and `defender_infantry`. Archers are retained only when `Scene.GetNavMeshFaceIndex(..., checkIfDisabled:true)` returns a face. Infantry positions are not filtered this way, so static and runtime validation must catch bad ones.

Initial defenders are sorted with ranged first and round-robin across area-index groups. A subarea is selected with weight `1000 × available preferred-type slots + available other-type slots`; one free preferred slot is used, otherwise the other type. Deduplication is within one group's type list, not globally across types/areas. Sharing a physical entity across groups or types can produce overlapping assignments.

Missing groups can cause modulo-by-zero; exhausted slots can yield a null entity dereference. A summed count across the whole scene does not prove allocation safety. One well-covered group with sufficient distinct positions is the simplest reliable conversion.

When an area is lost, surviving assigned defenders move to available slots in the next area. `FindPosition` can still index a subarea of `-1` when neither type has free capacity. Model worst-case occupancy after fallback, including defenders already in that destination. A single positive stage avoids this inter-stage relocation path; it still needs usable initial slots and routes.

## Attacker deployment is different

The controller calls `Mission.SpawnTroop` with `hasFormation:true` and no explicit initial position. `DefaultMissionDeploymentPlan.ReadSiegeBattleEntitiesFromScene` selects **one** entity with the formation's side/class tag through `FindEntityWithTag`, plus an optional matching `_reinforcement` entity. Missing reinforcement frames reuse the initial frame; missing classes fall back to another formation class.

`Mission.GetTroopSpawnFrameWithIndex` derives unit positions around the deployment frame through `Formation.GetUnitSpawnFrameWithIndex`, using the troop index/count, formation width and spacing. It can clamp an off-navmesh position back toward the navigation mesh. A valid anchor therefore does not prove a safe formation footprint, and multiple same-tag anchors are not a set of individual troop slots. The controller sets square/deep formation orders after initial spawning, so do not assume those orders describe the initial footprint.

Native prefab `sp_attacker_infantry` carries `attacker_infantry` and `battle`; `sp_attacker_archer` carries `attacker_archer`, `attacker_ranged` and `battle`. Preserve the installed contract rather than inferring a tag from the prefab's display name.

## Shipped comparison

Under the active game installation, inspect:

- `Modules/SandBox/SceneObj/european_castle_keep_a_l3_interior/scene.xscene`
- `Modules/SandBox/Prefabs/sp_editor_spawnpoints.xml`
- `Modules/Native/Prefabs/editor_spawnpoints.xml`
- The active settlement XML defining `lordshall` scene names.

The verified keep has civilian/siege variants, ground and upper-floor fight markers, defender positions, and siege-only barricades. Prefabs supply defaults that scene instances may omit. Inspect effective scripts, tags, levels and global frames; do not copy the reference's coordinates, marker count or stage count into another hall.
