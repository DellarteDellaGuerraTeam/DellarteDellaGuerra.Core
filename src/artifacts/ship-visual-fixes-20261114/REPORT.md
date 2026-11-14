# DADG ship visual repairs — 14 November 2026

**Live changes:** native hull-fire visual/audio holders installed on all three ships. **Prepared but not activated:** separated original rudders, their correctly pivoted prefab cutovers, and hull-derived water-clip SDF source meshes. New mesh imports require the Bannerlord Resource Browser; neither a supported native desktop control surface nor a supported unattended FBX importer is available in this session.

All mesh authoring and validation used **Blender 5.1 background CLI (`-b`)**, with no GUI session left running. Original Map/Scenes source files, TPACs, RuntimeDataCache, collision, navmesh, tuning, capacity, upgrades and boarding setup were preserved.

## Installed fire presentation

| Ship | Deck fire nodes | Sound nodes | Lights | Status |
|---|---:|---:|---:|---|
| English boat B | 14 | 2 | 2 | Loose prefab XML installed; runtime untested |
| Kogge | 18 | 3 | 2 | Loose prefab XML installed; runtime untested |
| Carrack | 18 | 3 | 2 | Loose prefab XML installed; runtime untested |

The new `ship_burning_system.xml` in each live Map ship folder uses current native `ShipBurningSystem`, `BurningNode`, `BurningSoundNode`, particle and light definitions. The supported fire sites were obtained by downward rays against each actual body mesh's horizontal surfaces, with its existing root Z offset applied. The native particle prototype has no per-particle fire sound; dedicated sound nodes provide audio.

Static checks: explicit parent tags required by `FetchEntities`; contiguous unique node indices; unique instance GUIDs; no donor physics, destructible, damage or hull mesh components; all node positions supported by measured body surfaces. Empty unused fire groups are retained and are safely ignored by the native implementation. The parent agent independently reviewed these checks.

Live files:

- `DellarteDellaGuerraMap/NestedPrefabs/dadg_ws_english_boat_b_ship_nested/ship_burning_system.xml`
- `DellarteDellaGuerraMap/NestedPrefabs/dadg_ws_kogge_ship_nested/ship_burning_system.xml`
- `DellarteDellaGuerraMap/NestedPrefabs/dadg_ws_carrack_ship_nested/ship_burning_system.xml`

## Rudders: exact original geometry, separated without duplicates

Each original body contained a disconnected rudder component. Simply adding another blade to the existing compiled ship would duplicate the visible rudder. The staged body FBX removes that component, and a new rudder FBX contains the same faces, UVs and material assignments.

| Ship | Original body vertices | Body after separation | Rudder vertices | Neutral-pose maximum error |
|---|---:|---:|---:|---:|
| English boat B | 546 | 524 | 22 | 0.000000120 m |
| Kogge | 20,922 | 20,620 | 302 | 0.000000477 m |
| Carrack | 84,719 | 84,551 | 168 | 0.000000239 m |

The visual hinges follow the original raked sternpost/blade edge, rather than the inaccurate old empty stock marker. The mesh is inverse-transformed around that hinge, preserving its exact neutral assembled geometry. Physics stock markers are unchanged.

| Ship | New visual pivot in ship-root metres | Initial X rotation, radians | Driver |
|---|---|---:|---|
| English boat B | (0.000544803, -3.509893656, 0) | 0.667283704 | Native tagged-rudder fallback |
| Kogge | (-0.008291706, -11.249819756, 0) | 0.172645559 | `ShipRudderVisualMissionLogic` |
| Carrack | (0.000002518, -8.516388893, 0) | 0.268872684 | `ShipRudderVisualMissionLogic` |

Current native `ShipControllerMachine` prefers its immediate child named `controller` over a separately tagged rudder. Kogge's tiller and carrack's whipstaff already use that path, including a different steering axis. The parent agent therefore added a small visual-only adapter for blades tagged `dadg_rudder_visual`, preserving both existing controller assemblies. English boat uses `rudder_rotation_entity` and needs no custom driver. The adapter writes an absolute rotation about each blade's resting local UP axis; it does not change physics or accumulate angle drift.

Parent verification: the production-source adapter harness passed empty mission, late spawn, positive/reversed absolute rotation, preserved pivot, removed blade and no-drift checks. Release solution build passed with 86 warnings and zero errors. Built and deployed Win64 Shipping Client/editor Integration DLL SHA-256: `13c73078362c128aada391350780512b7bc11192292e9448f111ef4d9e42f513`. No new live mission was run.

## SDF water-clipping candidates

Each closed dry-interior envelope is derived from that ship's original hull vertices, clipped above the central deck, then enclosed as a convex hull. It is not a borrowed stock-ship or rectangular clipping volume. A direct timber-skin closure was deliberately rejected because it represents the solid hull shell instead of the dry interior; the carrack skin was also nonmanifold.

| Ship | SDF name | Nonmanifold edges after FBX reimport | Volume | Cap height in ship-root coordinates |
|---|---|---:|---:|---:|
| English boat B | `sdf_dadg_ws_english_boat_b_v1` | 0 | 12.490936 m³ | 0.800 m |
| Kogge | `sdf_dadg_ws_kogge_v1` | 0 | 341.953614 m³ | 2.150 m |
| Carrack | `sdf_dadg_ws_carrack_v1` | 0 | 321.162176 m³ | 2.450 m |

These are **source candidates**, not runtime SDF textures. The convex envelope can bridge hull concavities, so its native visual fit must be checked before deployment. Watertightness alone does not prove correct in-game water clipping. The staged `ship_water_clip.xml` uses the new SDF name, but remains outside live NestedPrefabs until the matching SDF resource is generated by the native importer.

## Required native import and cutover

For each ship, complete the whole body/rudder pair before launching a mission:

1. Back up the existing body geometry package and inspect that Resource Browser entry's actual Source binding. Do not assume a flat `AssetSources/*.fbx` path: those historical paths are absent in the current checkout.
2. Reimport the existing body geometry entry from its staged `*_rudder_separated_v1.fbx`, preserving its current metamesh identity/name and import settings. This removes the baked static rudder while preserving other geometry/materials. Existing body names are `dadg_ws_english_boat_b_body`, `dadg_ws_kogge_body_without_yards_v1`, and `dadg_ws_carrack_body_without_yards_v1`.
3. Import the corresponding `dadg_ws_<ship>_rudder_v1.fbx` into Map, retaining that metamesh name and source materials. Check its local pivot/bounds and primary material bindings. All FBXs use the existing authoring convention: forward Y, up Z, metre units.
4. Import `sdf_dadg_ws_<ship>_v1.fbx` through the native Resource Browser. Confirm the generated SDF resource name, then use that exact name in the staged clip script. Use the native Visualize toggle to check below-deck interior coverage and any over-clipping outside the hull.
5. Replace only that live ship's `rudders_holder.xml` with the staged version, and add its staged `ship_water_clip.xml`. The rudder and SDF cutovers are independently gated on their corresponding imports; do not deploy references to missing resources. Keep the newly built DADG Integration DLL with the kogge/carrack custom rudder tags.
6. Restart/rescan the native editor after loose prefab changes. Verify the body has only one visible rudder, neutral alignment is unchanged, rudder turns about its fitted hinge, and the existing tiller/whipstaff still moves. Confirm after a fresh editor/game launch.
7. In a sea battle, test port/starboard helm movement, waves and below-deck water clipping; inflict hull fire and verify fire spread/light/audio; then save/reload. These runtime checks have not been completed here.

## Deliverables and repeatable verification

Primary durable package: `D:/SteamLibrary/steamapps/common/Mount & Blade II Bannerlord/Modules/DellarteDellaGuerra.Core/src/artifacts/ship-visual-fixes-20261114/authoring/`.

Versioned source-art copy: `DellarteDellaGuerraMap/Tools/WarSailsShips/visual-fixes-20261114/authoring/`, tracked using that repository's existing FBX/Blender Git LFS rules. This is an inactive import handoff; copying it does not activate new resource references.

Original versioned Blender output: `D:/AI/blender-mcp/data/dadg_ship_visual_fixes_20261114_v1/`.

The package includes three `.blend` assemblies, nine FBXs, source/geometry audits, native prefab staging for all three ships, and a rudder side-profile image. `files.json` records exact copied file hashes and sizes. `source-authoring.json` records the unchanged source hashes, fitted hinges and exact neutral-pose check. `roundtrip-verification.json` records independent FBX import results.

All three independent roundtrips passed original body-plus-rudder polygon/UV/material equality (world coordinates compared to 0.1 mm; UVs to 0.00001), and all SDFs remained watertight with matching volume. These offline checks do not replace native import or runtime testing.

Reproducers alongside this report:

- `inspect_sources.py` — original mesh/component inventory.
- `author_sources.py` — source-preserving CLI separation and hull-derived SDF authoring; per-source assertions.
- `verify_roundtrip.py` — independent FBX reimport checks.
- `build_prefabs.py` — visual-only XML generation; `--deploy-fire` installs only existing-native-asset fire holders. New rudder/SDF references stay staged.

No compiled package/cache was hand-edited. Native mesh import and the final runtime checks remain the explicit completion gate for rudders and water clipping.
