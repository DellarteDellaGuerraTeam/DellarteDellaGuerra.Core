# War Sails: DADG sail cloth setup

## Why

A sea battle crashes as soon as a DADG ship spawns. It fails with an NRE in `SailVisual.GetDimensions`, called from `MissionSail.InitializeCenterOfSailForceLocal`.

`SailVisual` reads `SailClothComponent` from the skeleton of the child entity tagged `sail_mesh_entity`. That component only exists if the sail's cloth metamesh has saved **Cloth Editor mesh settings**. None of the DADG sail meshes have any. The playtest evidence is in `src/runtime-evidence/ws-dadg-ships-sea-battle.feature.md`.

## Meshes to set up (required: 7)

| Ship | Sail entity | Cloth metamesh | Package (Map/Assets) |
| --- | --- | --- | --- |
| Carrack | sail_center_0 | `dadg_ws_carrack_sail01_v1_cloth` | `dadg_ws_carrack_sail01_v3_geo` |
| Carrack | sail_center_1 | `dadg_ws_carrack_sail02_v1_cloth` | `dadg_ws_carrack_sail02_v3_geo` |
| Carrack | sail_center_2 | `dadg_ws_carrack_sail03_v1_cloth` | `dadg_ws_carrack_sail03_v3_geo` |
| Carrack | sail_center_3 | `dadg_ws_carrack_sail04_v1_cloth` | `dadg_ws_carrack_sail04_v3_geo` |
| Carrack | sail_center_4 (lateen) | `dadg_ws_carrack_sail05_v1_cloth` | `dadg_ws_carrack_sail05_v3_geo` |
| Kogge | sail_center_0 | `dadg_ws_kogge_sail01_v1_cloth` | `dadg_ws_kogge_sail01_v3_geo` |
| English boat B | sail_center_0 | `dadg_ws_english_boat_b_cloth_v4` | `dadg_ws_english_boat_b_sail_source_v4_geo` |

**Optional:** the matching `*_burning` meshes (`..._v1_burning`, `dadg_ws_english_boat_b_burning_v4`). The code tolerates missing cloth on these at spawn, but burning sails won't render their fire mesh without it.

## Steps for each cloth metamesh (Modding Kit, NavalDLC loaded first)

1. Open the **Cloth Editor** and select the `_cloth` metamesh from the table as the render mesh.
2. Copy the native values from `square_sail_a` (for the carrack's lateen sail05, use `lateen_sail_a`):
   - **Use cloth**: ticked
   - **Always keep state**: ticked
   - **Max distance**: 12
   - **Collision body**: `sail_pole`
   - **Material preset**: empty

   Do not copy from `square_sail_a_free`. That is the burning mesh, and it uses Max distance 30.
3. The **Skeleton** field under Preview properties is for the preview only and is not saved. The in-game skeleton comes from the prefab. The field also "sticks" when you switch render meshes, which is harmless.
4. Preview with wind. The sail should billow and should not shred or fall through.
5. Click **Save mesh settings**. This writes into the sail's own `Assets/*_geo.tpac` (and the RuntimeDataCache .rdc). No `.tcc` or `.tcm` files are produced. Commit the tpac.

**Exception, English boat B:** with `sail_pole` collision the cloth shreds because the pole passes through the sail. Use an empty collision body and **Max distance 1**.

## Top flag (required)

`MissionSail` reads the sail's top banner on every tick without a null check. Every sail entity therefore needs a child tagged `bd_banner_b` whose mesh has cloth. The flag-capture animation also calls that cloth without a guard. Each DADG sail now has a `sail_faction_flag` child with these settings:
- tags `bd_banner_b` and `banner_with_faction_color`
- mesh `ship_generic_flag_a`
- position 1.5 m above the sail origin
- scale 2, or 1 on the boat

Adjust the position in the editor if the flag doesn't sit on the mast top.

## Check before telling Claude it's done

- Open `dadg_ws_carrack_ship_nested` in a scene with wind. All 5 sails should simulate. Repeat for the kogge and the English boat.
- Tell Claude when you're done. The playtest then gets rerun, and the pass criterion is that `SailClothComponent != null` for every sail at spawn, followed by the sailing and combat checks.
