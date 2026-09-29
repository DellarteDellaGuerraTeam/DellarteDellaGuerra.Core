---
name: bannerlord-lord-hall-battle
description: Prepare an existing Bannerlord lord hall for native post-siege keep fighting, including battle helpers, scene levels, spawn capacity, and navmesh validation. Use when converting a peaceful lord hall into a fighting hall or enabling combat after siege defenders pull back; not for exterior siege machinery or campaign retreat logic.
---

# Bannerlord Lord Hall Battle

Convert the selected scene while preserving its peaceful visit, existing architecture, and settlement mapping. A location named `lordshall` may actually load a courtyard: inspect the asset, explain that finding, and adapt its combat layout without silently replacing it or changing `is_indoors`.

This prepares scene content for the existing keep-fight mission. It does not add a campaign transition for a retreating player defender or turn the hall into an exterior siege mission.

For a concrete minimal conversion, read the [London worked example](references/london-example.md): a courtyard with unchanged navigation and metadata-only helpers, followed by a user-reported successful keep battle. Adapt the layout to the target; its coordinates and counts are not a universal template.

## Establish the native contract

1. Resolve the active game version, loaded modules and overrides, settlement `lordshall` scene names for each wall level, actual `SceneObj` directory, local instructions, and existing edits. Do not assume the repository's supported version is the running version.
2. Inspect a shipped SandBox keep of that version and its inherited prefabs. Use available version-matched Bannerlord source tools for `LordsHallFightMissionController`, `FightAreaMarker`/`AreaMarker`, `DefaultMissionDeploymentPlan`, `SandBoxMissions.OpenSiegeLordsHallFightMission`, and the active `SiegeLordsHallFightModel` or mod override. [Native contract notes](references/native-contract.md) record a verified 1.4.6 example, not universal constants.
3. Work from a staged copy or equivalent recoverable edit, with original hashes. Keep the settlement mapping and other scenes unchanged unless the requested conversion requires them. Make the patch reviewable before applying it; existing user authorization still applies.

If the hall already satisfies the combat contract, preserve its existing stages and return a verified no-change result; the single-stage preference below applies to new layouts. If installed modules and available source versions differ, label source-derived requirements provisional, check the installed native assets, and identify the runtime compatibility checks still needed.

## Derive the combat layout from the scene

- Identify the actual entrance, floor elevations, stairs, furniture/collision, barriers, peaceful spawn helpers, and scene-level groups. Resolve prefab defaults and parent transforms before counting helpers or using coordinates.
- Read the existing navmesh and overlay it with the proposed helpers. Read [navmesh handling](references/navmesh.md) when using Studio, inspecting binary fields, or changing navigation. A floor-plan projection alone cannot establish that a point belongs to the right storey or active navigation layer.
- Choose an attacker entry with space for generated formation footprints and a reachable defender zone. Face attackers into the fight and defenders toward the approach. Retain useful cover; add barricades only when the design needs them and their collision/navigation can be validated.
- Prefer a single stage (`AreaIndex=1`, `SubAreaIndex=1`) for a minimal conversion. Multiple markers in that same group may cover irregular rooms. Use multiple stages only when the user needs staged fallback and every destination has validated capacity and routes for surviving defenders.

## Author the helpers

- Add actual `FightAreaMarker` script components, not merely entities named or tagged like markers. Set positive area/subarea indices and suitable `AreaRadius` values. Radius membership uses global **3D spherical distance**; check it against all nearby floors.
- Helpers can be metadata-only entities with explicit scripts, tags, transforms and levels; editor arrow meshes and prefab instances are not inherently required. Verify the native component contract before omitting prefab defaults. Derive facing from the target's transforms; for an unparented, unpitched helper, local +Y gives `forward=(-sin(yaw), cos(yaw), 0)`.
- Place individually usable entities tagged `defender_infantry` and, where useful, `defender_archer` inside the intended marker spheres. Provide enough **distinct, reachable, unoccupied positions** for the active model's maximum defenders, allowing its ranged-to-infantry and infantry-to-ranged fallback. Do not double-count an entity bearing both tags or shared across different groups.
- Author recognized attacker deployment frames using the version's native tags/prefabs, normally infantry and ranged. The native ranged prefab carries both `attacker_archer` and `attacker_ranged`; verify the installed prefab and source. These are **formation anchors**, not one point per soldier: duplicate tagged anchors do not provide extra capacity. Validate the generated footprint for infantry-only, ranged-only and mixed attacker rosters, plus the player and reinforcements.
- Optional matching attacker `*_reinforcement` frames fall back to the initial frames in the verified version. Add them only when initial-area reuse would be unsafe; keep the entry route open during combat.
- Give new combat helpers explicit effective visibility in `siege` and every applicable wall-level variant, excluding `civilian`. Match the actual scene level scheme; do not invent level names or rely on prefab/editor-only mesh flags as proof of runtime activation. Top-level helpers simplify transform and visibility auditing.
- Preserve original peaceful helpers, common architecture, atmosphere and lighting. Remove a civilian obstruction from siege visibility only when it blocks the intended combat route. Keep navmesh unchanged if existing active faces support the layout; do not add exterior siege gates, ladders or deployment machinery without a separate need.

## Validate and apply

Static acceptance checks:

- XML parses; IDs remain unique; scripts, tags and referenced prefabs resolve in the loaded modules. Repeated execution updates the conversion's helpers rather than adding a duplicate set.
- Each effective siege/wall-level configuration has usable attacker frames and nonempty defender groups. The civilian configuration retains its original visit and excludes the new battle helpers.
- Every defender position belongs to its intended group in global 3D coordinates, is on the correct floor, has physical clearance, and connects to the attacker combat area through traversable active navigation. Archer positions need an enabled navmesh face; infantry needs valid navigation even though this controller does not filter its points during collection.
- Capacity is checked with the controller's actual allocation and fallback rules, not just total entity count. Multiple stages need destination free slots for surviving earlier groups **and** their existing occupants. Do not permit empty or exhausted selected groups.
- Spawns, stairs and movement corridors stay usable with the siege furniture configuration. Disconnected navmesh components or unusual raw face IDs are findings to investigate, not automatic defects.
- Review the diff and original hashes before applying. Preserve concurrent edits; apply only intended scene changes and keep a recoverable original. If navigation was unchanged, verify its hash is identical. If it changed, validate its format, attributes and topology before loading it.

When a compatible editor/game and tools are available, verify both the civilian visit and native `SiegeLordsHallFightMission`: maximum supported defenders, attacker roster extremes, player spawn, reinforcement reuse, pathfinding, combat resolution, and exit back to campaign. Exercise each wall-level scene variant actually changed. For multi-stage scenes, also exercise fallback with surviving defenders. Use available Bannerlord debug/playtest skills for those operations; they are not required for a static-only deliverable.

Report the actual edited scene, helper layout/capacity, navmesh changes or preservation, and evidence. Distinguish **statically prepared**, **loaded successfully**, **combat tested**, and **campaign transition tested**. Parsing a binary or loading a peaceful scene does not establish keep-fight correctness. If runtime access is unavailable, finish the safe authoring/static work and state the unverified checks without claiming the conversion is fully playtested.

Record later user playtest results as attributed evidence and update the assessment. A reported successful battle confirms that run, not unreported roster extremes, wall-level variants, civilian visits, or campaign transitions; distinguish user reports from directly observed tests.
