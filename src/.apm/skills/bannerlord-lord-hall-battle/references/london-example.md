# London: minimal conversion with a successful user playtest

## Evidence and scope

On 2026-08-31, after the reviewed conversion was applied, the user reported: "that keep battle worked on the first try!" This is user-reported runtime success for the converted scene, not an agent-observed test matrix. Troop counts, wall level, reinforcements, civilian visit and campaign return were not specified.

Installed Native/SandBox manifests reported 1.4.8; controller/deployment source was available for 1.4.6. The successful run supports this layout on the user's tested setup, without proving all 1.4.6 constants or untested behaviors unchanged in 1.4.8.

## What was sufficient

The settlement already mapped all wall levels to `DellarteDellaGuerraScenes/SceneObj/castle_hall_London`. Although named a hall, the asset is an outdoor courtyard. The conversion preserved that mapping and `is_indoors=false`.

Only 36 top-level metadata entities were added to `scene.xscene`:

| Helpers | London placement and purpose |
|---|---|
| One `FightAreaMarker` | Center `(47,31,0)`, radius 9, area 1 / subarea 1; a single defense stage |
| 27 `defender_infantry` points | Distinct positions facing the approach |
| Six separate `defender_archer` points | Preferred ranged positions; native fallback can use infantry points |
| One infantry attacker anchor | `(44,42,0.041)`, tags `attacker_infantry`, `battle` |
| One ranged attacker anchor | `(41,50,0.200)`, tags `attacker_archer`, `attacker_ranged`, `battle` |

Every addition explicitly selected `level_1`, `level_2`, `level_3` and `siege`, excluding `civilian`. No meshes, prefab dependencies, barricades, exterior siege machinery, campaign code or new reinforcement frames were needed. Existing architecture, peaceful helpers and all six companion scene files were unchanged.

The six ranged helpers are not a six-archer troop limit. The 33 distinct defender positions were checked against every composition of 27 defenders under the inspected allocation rules, including 19 ranged plus eight infantry. Attacker helpers are formation anchors, not individual troop slots.

## Minimal marker structure

This is the actual London structure. Adapt coordinates, radius and level names; it is not a complete scene or an automatic placement recipe.

```xml
<game_entity name="dadg_keep_battle_fight_area_01" old_prefab_name="">
  <tags><tag name="fight_area_marker" /></tags>
  <transform position="47.000, 31.000, 0.000" rotation_euler="0.000, 0.000, 0.000000" />
  <scripts>
    <script name="FightAreaMarker">
      <variables>
        <variable name="AreaRadius" value="9.000" />
        <variable name="AreaIndex" value="1" />
        <variable name="Type" value="" />
        <variable name="SubAreaIndex" value="1" />
      </variables>
    </script>
  </scripts>
  <levels>
    <level name="level_1" /><level name="level_2" />
    <level name="level_3" /><level name="siege" />
  </levels>
</game_entity>
```

Defender and attacker helpers used the same entity/transform/level structure with their respective tags and no marker script. Editor arrow meshes were unnecessary for runtime behavior.

## Placement and navigation lessons

- London's NMG9 mesh contained 104 faces across seven shared-edge components. All 35 agent helpers used the existing 89-face ground component; the other components were preserved.
- Used faces retained raw `g3=1` and `g5=-1`. The entire navmesh binary remained byte-identical. This successful conversion did not require rewriting navigation IDs to zero.
- Defender spacing was approximately 1.5m; the farthest point lay 7.576m from the marker center, inside its 9m **3D** sphere. Heights followed the selected navmesh floor, not a flattened Z plane.
- For these unparented, unpitched helpers, yaw is in radians and local +Y is forward: `forward=(-sin(yaw), cos(yaw), 0)`. For a desired horizontal direction `(dx,dy)`, use `yaw=atan2(-dx,dy)`. London attackers used about -2.730076 and defenders 0.411517.
- Static checks covered all attacker-to-defender approaches and separate 8×8m and 16×4m deployment envelopes. Those dimensions were clearance probes, not engine footprint guarantees or universal minimums.

Reuse the method: retain a usable mesh, add the native functional helpers, check allocation and spatial clearance, then verify combat. Recompute placement for each scene; narrower rooms, stairs, alternate layers or model overrides can need a different layout.
