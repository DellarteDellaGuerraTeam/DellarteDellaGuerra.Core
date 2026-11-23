# DADG War Sails seaborne village raid migration

## Scope

The campaign defines 426 villages. Seventy-three have both port coordinates in
`DellarteDellaGuerraMap/ModuleData/settlements.xml` and a tagged village sea
drop-off in `Main_map/scene.xscene`. Those 73 are currently reachable from sea.
Three use custom DADG mission scenes:

| Village | Original DADG scene | Sea-raid scene | Status |
| --- | --- | --- | --- |
| Grimsby | `dadg_village_coastal_a` | `dadg_village_coastal_a_navalraid` | Installed; offline checks passed; game mission pending |
| Southwark | `dadg_village_southwark` | `dadg_village_southwark_navalraid` | Installed; offline checks passed; game mission pending |
| Thorne | `dadg_village_d_combe` | None | Deferred by request |

The other 70 sea-accessible villages originally used 19 shared Empire or
Battania village layouts. All 70 are now assigned the Grimsby DADG generic
coastal raid layout, `dadg_village_coastal_a_navalraid`. Their original shared
scene assets remain available to villages without sea access. Eight of those
old layouts, used by 35 villages, had no water enabled, so changing only raid
markers in them would not have supported ship landings. See
`generic-seaborne-layouts.csv` for the original per-layout audit.
The 55 other villages that use `dadg_village_coastal_a` lack port coordinates
and map sea drop-offs; a coastal-looking mission scene alone does not make them
sea-accessible. See `seaborne-village-inventory.csv` for each village's state.

## Acceptance checks for each scene variant

1. Copy the original `SceneObj` and `SceneEditData` into a new scene ID, keeping
   the original scene available for villages without sea access.
2. Add `land_raid` and `naval_raid` levels. Keep the ordinary battle set on
   `land_raid`; add the NavalDLC raid battle set with defenders on land and
   three attacker ship formations in navigable water.
3. Author a connected naval navmesh with land faces using ID 0 and shallow
   water faces no deeper than 1.4 m below the water surface. Check defenders,
   landings, jumping points, defender view, tactical, archers, and flee points
   against the active naval mesh; check ship spawns in deep water and all points
   inside soft borders.
4. Add three spaced `landing_00x_0` shore points, child `jumping_00x` points,
   and approach route nodes facing the next point. Add ChokePoint with child
   Cliff, strategic archers, defender `player_spawn_frame`, and appropriate
   atmosphere and sounds.
5. Parse and validate the scene XML; compare navmesh topology with the source;
   inspect the scene offline; then run a sea raid mission in game. Reassign
   the village only when scene geometry and static checks pass, and record
   any remaining runtime uncertainty.

These checks implement TaleWorlds' [seaborne raid scene specification](https://moddocs.bannerlord.com/war-sails/what_makes_seaborne_village_raid_scene/).
The world-map drop-off requirement comes from its [War Sails world map guide](https://moddocs.bannerlord.com/war-sails/wsworldmap/).

## Scene-specific work

### `dadg_village_coastal_a_navalraid` — Grimsby

Created from the original DADG coastal scene. The west shore has room for
three landing points along roughly 100 m of coastline. The variant adds the
naval battle set, three landing and jumping routes, ChokePoint/Cliff,
strategic archers, defender view point, wider soft borders over water, and a
connected naval ID-0 navmesh. The original land navmesh remains on the
`land_raid` level. The copied editor terrain source and runtime terrain are
unchanged. Navmesh Studio verified that the three landing paths reach the
village, all ship spawns sit in water deeper than 2 m, and newly authored
walking faces follow seabed no deeper than 1.4 m. Grimsby and the 70 other
generic sea-accessible villages were reassigned. A game raid
and normal visit remain to be tested before calling this scene fully proven.
An independent Astra review found an off-mesh Cliff child and route headings
that did not face the next waypoint. Both were corrected; the child now lies on
the connected village path and the ship routes face shore.

### `dadg_village_southwark_navalraid` — Southwark

The original village terrain was retained. The variant extends the north soft
border over water, adds three landing and jumping routes along the existing
bank, and connects a new shallow-water naval ID-0 navmesh to the village's
main walking component. All three landings and the tactical point belong to
the same 1,327-face component as the village center. Eight attacker ship spawn
points are in water more than 3 m deep. XML, marker, border, and path checks
pass offline, and Southwark alone was reassigned. A game raid and normal visit
are still needed to establish runtime behavior.
An independent Astra review found an off-mesh Cliff child, off-mesh flee-line
endpoints, and closely spaced primary ship spawns. The Cliff child and both
complete flee-line segments now lie on connected naval mesh. The three primary
ships are spaced 45 m and 80 m apart, and ship 1 starts 10 m north of its first
waypoint. Hull clearance and docking still need a game mission test.

### `dadg_village_d_combe` — Thorne (deferred)

The water inside the scene is a narrow, winding channel. A 2 m terrain sample
found water strips only about 4–8 m wide at y=220–280, between the wider
water at y=200 and the village. The source navmesh covers the adjoining land
but has no naval ID-0 faces in that channel. Check whether three ships can
approach and dock in an existing wider section while still providing a short,
connected fight into the village. If that route cannot work, widening the
channel would require coordinated changes to runtime and editor terrain.
Thorne is excluded from the current migration by request and remains assigned
to its original scene.

### Generic scene distribution

The 70 villages with existing port coordinates and tagged campaign-map drop-off
points, excluding Grimsby, Southwark, and Thorne, now use the validated DADG
generic coastal raid variant. Only their `village_center` scene names changed;
their world-map positions and drop-offs were not moved, so this assignment does
not require a settlement distance-cache rebuild. The scene is shared across
these villages; regional building styles from their previous Empire/Battania
layouts are no longer used by these 70 village centers. The
`seaborne-village-inventory.csv` records all assignments.

## Current verification boundary

The Grimsby and Southwark scene assets are installed. Seventy-two of the 73
sea-accessible villages are assigned one of those two variants; Thorne remains
unchanged by request. Offline XML and Navmesh Studio checks do not establish
that ships dock or agents fight correctly in a Bannerlord mission; that
requires a runtime raid playtest.
