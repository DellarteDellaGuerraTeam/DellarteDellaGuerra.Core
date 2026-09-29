# DADG War Sails ships — handoff

Updated: 2026-09-29, after the carrack material repair and native preview verification.

## Goal and completion criteria

Make **all three copied DADG ships** playable War Sails ships in **DellarteDellaGuerraMap**:

| Ship | Hull / mission ID | Ship prefab |
| --- | --- | --- |
| English boat B | `dadg_ws_english_boat_b` | `dadg_ws_english_boat_b_ship_nested` |
| Cog / kogge | `dadg_ws_kogge` | `dadg_ws_kogge_ship_nested` |
| Carrack | `dadg_ws_carrack` | `dadg_ws_carrack_ship_nested` |

The user explicitly requested all three, preservation of original assets, material-name fixes directly in copied FBXs, NavalDLC before DADG in editor load order, and documentation of the process. English boat A is a reference/export, not a fourth requested ship.

Completion means each custom ship loads by its own registered IDs, renders correctly after a fresh launch, floats at a calibrated waterline, sails and turns, furls/unfurls correctly, supports crew navigation, boarding and water-to-deck climbing, is obtainable from an intended shipyard, and survives campaign save/reload. **None is currently verified playable.** No game mission or campaign test has been performed for these ships.

Suggested execution order: finish shared asset dependencies, then complete English boat B end to end, followed by kogge and carrack. All three remain in scope.

## Workspace and references

- Core source / this handoff: `D:/SteamLibrary/steamapps/common/Mount & Blade II Bannerlord/Modules/DellarteDellaGuerra.Core/src`.
- Live Map module: `D:/SteamLibrary/steamapps/common/Mount & Blade II Bannerlord/Modules/DellarteDellaGuerraMap` (`../../DellarteDellaGuerraMap` from `src`).
- Original models/materials: sibling `DellarteDellaGuerraScenes`; preserve them.
- Authoring work package: `.codex-work/war-sails-ships/` under `src`; paths below are relative to this package unless stated otherwise.
- Reusable workflow: `.apm/skills/bannerlord-modding-tools/references/war-sails-ships.md`, mirrored under `.agents/skills/`.
- Official requirements: https://moddocs.bannerlord.com/war-sails/ship_creation/
- Existing local research and native examples: `reference/ModuleData/`, `reference/NestedPrefabs/`, `reference/prefabs/`, and `reference/native-meshes-resolved/`.
- Detailed follow-up: `runtime-validation.md`, `playable-root-blocker-review.md`, `gameplay-root-drafts/manifest.json`, `native-authoring-fixture/promotion.json`.

**Status precedence:** this handoff and the latest specific audit JSONs supersede older narrative checkpoints. In particular, `runtime-validation.md` still says only three of six cog/carrack sail packages use the Map sail material; that is stale. All six passed the later `sail-material-native-reimport-comparison-v1.json` audit. Older README text also predates the completed shields and carrack material copies. Historical v1/v2/v3 English experimental imports are not the intended fresh v4 import.

## Verified progress

| Area | Verified | Still missing |
| --- | --- | --- |
| Main visual imports | Three visual packages, 17 metameshes; names, bounds and primary material bindings audited at their import checkpoint. Canonical alias corrections were made in copied FBX/Blender sources. | Remaining aliases must point to Map-owned copies; native reimport and startup checks of complete assemblies. |
| Prefab discovery | Three visual ship assemblies and six cog/carrack sail candidates are visible in the native Prefabs browser after the XML wrapper fix/restart. | Live ship roots are visual assemblies; gameplay draft assembly is not deployed. |
| Collision | Six hull-specific convex/detailed bodies inspected natively with `wood_ship` and no invalid-face line. Cog/carrack bodies without static yards also imported. | Attach correct bodies to roots; validate movement, walkability, batching and buoyancy. English aisle v2 replacements still need import/reference verification. |
| Cog/carrack sails | Six v3 imports have clean identities, native skeletons and 0–199 timing; 184 optimized stored morph keys are expected. All six now use `dadg_ws_sails_plain`. | Cloth settings, furl playback, force/yard controls and assembled-ship tests. |
| English sail | Fresh v4 source with unique internal names and Map sail material alias is staged. | Native import, identity/rig/timing audit, prefab integration and cloth. Old experimental package retains obsolete skeleton/animation entries. |
| Textures | 22/22 copied Map textures passed metadata checks. | Appearance and runtime-data checks are distinct from metadata; verify final assemblies after restart. |
| Materials | `dadg_ws_sails_plain` completed earlier; 4/9 subsequent copies saved and audited: rich wood, rope, shields, carrack window. | Five subsequent material copies and mesh rebindings below. |
| Authoring scenes | Three blank Map scene folders with candidate local navmesh files promoted and hash-verified. | Native scene load, ship placement, generated floaters, cloth saves and named runtime navmesh assets. |
| Registrations | Inactive hull/mission XML drafts and integration instructions exist. | Calibration, activation, XML load, culture availability and all runtime tests. |

## Immediate next work: finish materials and source rebindings

### 1. Check the saved carrack repair across a fresh editor launch

The last verified editor was PID 47056, running the installed `Win64_Shipping_wEditor/TaleWorlds.MountAndBlade.Launcher.exe`. It was left in the foreground on **Material Editor dadg_ws_carrack_window**, rendering a textured sphere. Rediscover current windows/processes; do not reuse that PID or old Computer Use handles without checking.

The earlier assignment crash was a native access violation at `TaleWorlds.Native.dll + 0x907ca8`. Read-only disassembly identified an unchecked dereference after a case-sensitive typed-name lookup, before the material slot write. Material parameters matched the working originals. The tested repair was a native temporary-name rename followed by lowercase final names, then restart/cache regeneration:

| Slot | Saved Map texture | GUID |
| --- | --- | --- |
| 0 / DiffuseMap | `dadg_ws_window_low_carrack_window_basecolor` | `a9da8120-6843-4581-8571-cf8811ab0945` |
| 2 / NormalMap | `dadg_ws_window_low_carrack_window_normalopengl` | `51d804af-50a3-4bc5-b9cd-85028c28218b` |
| 4 / SpecularMap | `dadg_ws_window_low_carrack_s` | `4872a4da-40a7-46a6-8ea9-9421cc9ed70e` |

- Native assignment, Save and full Material Editor preview succeeded. Final save: 2026-09-29 17:24:51 local.
- Saved `Map/Assets/dadg_ws_carrack_window_mtl.tpac` SHA-256: `c071850d768d7ea11365fea993a3cad734a30198a281a5796bce81d0d7219f49`.
- Shader, flags, layout, blend, alpha and scalar/vector values match the Scenes original. The original file still matches its captured baseline.
- Both renamed texture GUIDs/settings/PNG contents were retained. Their regenerated RDC hashes matched their pre-rename contents.
- **Fresh whole-editor restart after the final normal/specular bindings is still unverified.** Diffuse alone was already verified across restart.
- This supports a texture name/registration repair. Casing alone versus the registration refresh caused by rename/restart was not isolated; do not claim a proven bad numeric material parameter.

Evidence: `material-dependency-audit/carrack-window-map-material-audit.json`, `carrack-material-final-snapshot.json`, `carrack-window-editor-crash-2026-09-29.md`, and `carrack-native-fault-diagnosis-2026-09-29.md` in the same directory.

Native Rename removed the associated runtime texture cache. Reimport/Save alone did not recreate it; restarting the editor did. Do not open a renamed texture with missing pixel data and mistake that separate cache failure for the original lookup crash. A separate shutdown assertion (`rglIntrusive_ptr.h:151`, `px != nullptr`) and later Safe Mode prompt occurred during an earlier editor exit; their cause is not established. Do not repeatedly launch duplicate editors or conflate these with the repaired assignment failure.

### 2. Finish the five pending Map materials

Use `material-dependency-audit/remaining-map-material-plan.json` for exact source paths, GUIDs and slots. The status file is `material-dependency-audit/map-material-import-status.json`: **4/9 verified**, excluding the earlier completed `sails_plain` copy.

| Pending material | Required exception |
| --- | --- |
| `dadg_ws_dawa_light_wooden_planks` | Specular uses `dadg_ws_dawa_rich_wood_s`. |
| `dadg_ws_dawa_wooden_planks` | Specular uses `dadg_ws_dawa_rich_wood_s`. |
| `dadg_ws_plank_fine_a` | Diffuse `dadg_ws_dadg_fine_plank_a`; preserve Native `plank_fine_a_n` / `plank_fine_a_s`. |
| `dadg_ws_plank_fine_b` | Diffuse `dadg_ws_dadg_fine_plank_b`; preserve Native `plank_fine_b_n` / `plank_fine_a_s`. |
| `dadg_ws_sails_heraldry` | Diffuse/normal use singular `dadg_ws_sail_heraldry_d/n`; preserve Native `plain_blue` specular. |

Inspect whether a pending duplicate already exists before duplicating again. Through the native Resource Browser, duplicate the Scenes material, rename the copy, Cut/Paste the copy into Map, assign the already imported textures, and Save. Audit distinct Map identity, exact texture slots and unchanged original shader/settings; record each audit in the status file. Never infer all slots from a filename stem. Preserve the original Scenes materials and the intended Native texture dependencies.

### 3. Promote staged FBX aliases, then perform native imports

Thirteen copied FBX/Blender source pairs are already prepared at:

`D:/AI/blender-mcp/data/dadg_ws_remaining_material_aliases_v1/manifest.json`

They cover the three original body sources, cog/carrack bodies without static yards, six cog/carrack sails, English aisle v2 body and English v4 sail. The staged edits were audited to preserve other structured FBX properties and Blender semantics. They have **not** been promoted by this remaining-material batch.

1. Inspect/run `prepare_remaining_material_alias_promotion.py` in plan mode. Its `--promote` path requires all nine material audits; do not bypass the gate.
2. After 9/9 passes, promote to each exact mapped Map `AssetSources` path with the script's backups and manifest. Obtain filesystem escalation where required for sibling Map writes.
3. Reimport **existing geometry entries** against their recorded source paths, retaining identity and import settings. Flat `Map/AssetSources/*.fbx` paths are used by many entries; the `dadg_warsails/` authoring copies are not automatically the native import sources.
4. Use a fresh additive import for the uniquely named English v4 sail and aisle v2 body when no current matching package exists. Verify/import the corresponding English aisle v2 detailed collision separately; it is not one of those 13 visual/sail alias files.
5. Audit primary material GUIDs, names, bounds/counts, rig and morph timing, and physics surfaces as applicable. `SecondMaterialGuid` overrides do not satisfy primary-binding checks.
6. Confirm every expected package changed; the editor's changed-file popup previously processed only part of a batch.
7. Restart when safe, then inspect complete hull/sail previews and startup warnings. Map loads before Scenes: previously 33/33 audited references to Scenes materials failed during startup. The permanent fix needs Map-owned material dependencies throughout these copied assets, not just an in-session reimport after Scenes loads.

Relevant tools/evidence: `verify_import.py --require-primary`, `verify_sail_import.py`, `tpac-audit/`, `source-material-import-verification.json`, `sail-material-native-reimport-comparison-v1.json`, `sail-native-rig-timing-v3.json`. Refresh input audits and expected material mappings for the intended new Map aliases before treating historical verifier results as current.

## Build and verify each actual gameplay ship

### 4. Assemble roots and resolve every imported reference

- Start from `gameplay-root-drafts/manifest.json` and its child XMLs. Live `Map/NestedPrefabs` roots still contain visual assemblies, not these gameplay roots.
- Resolve the English aisle v2 body/collision and v4 sail by their actual imported identities. Keep old experimental entries separate. Integrate cog/carrack bodies without static yards and all seven intended sail candidates without overlapping old canvas/yard geometry.
- Inspect root `MissionShip` / `NavalPhysics` and other draft scripts, convex body flags, detailed movable collision, `kinematic_batch_entity`, `batched_physics_entity`, `simulated` / `ray_cast` children and required tags against native examples and the installed game code.
- Confirm control/rudder, sail forces, spawn/rally points, boarding/receiving machines, climbers, edge barriers and water effects are fitted to each DADG hull. Native coordinates and stock statistics are references, not calibrated replacements.
- External loose prefab edits require the `<prefab_definition>` wrapper and an editor restart/rescan. Do not create a guessed packed-prefab index.

### 5. Finish native physics, navigation and cloth authoring

Use the existing Map scenes `SceneObj/dadg_ws_<ship>_authoring`; they contain local candidate navmeshes and water at root Z=0 but no placed ship root. Their files have not been verified in the native authoring workflow.

- [ ] Place the assembled root at identity and inspect detailed collision against actual deck/stairs.
- [ ] Generate floaters using root `Volume_generator` Mock Generation, volume count/width and Generate Volumes. The draft floater holders are empty.
- [ ] Calibrate mass, sit depth/waterline, drag and stability with `EditorShipPhysics`. Draft masses (English 3,071 kg; kogge 151,651 kg; carrack 105,912 kg) are estimates only.
- [ ] Inspect deck connectivity, clearance and boarding face IDs. Left/right pilot faces use matching ability IDs 1/2 and ordinary faces 0 in the current candidates.
- [ ] Set `MissionShip.NavMeshPrefabName = dadg_ws_<ship>_navmesh`; select the local faces and invoke **SaveNavMeshFromLocal**. Verify the resulting named runtime navmesh asset loads. A scene `navmesh.bin` is not that saved prefab asset.
- [ ] Use native Cloth Editor to set constraints/pins, inspect control channels, preview furl endpoints and wind, and **Save mesh settings**. Preserve generated `.tcc`/`.tcm` data. `ShipClothFixer` is not a bake button.
- [ ] Verify rig/yard control, folded/burning variants and force centers for every sail. Reconcile all five carrack canvas panels with actual mission sail entries; older drafts do not establish that mapping.
- [ ] Author/verify water-clipping geometry/SDF and visual effects; no water-clipping SDF or separate barriers were staged at the blocker review.
- [ ] Save/reopen each assembled prefab and repeat after editor restart before runtime handoff.

Known geometry risks from `playable-root-blocker-review.md`:

- English aisle collision already includes its +0.15 m shift; do not apply it twice on the entity.
- English boarding pilots lie on connected ID 1/2 faces, but straight floor-level travel toward the rails crosses the hull wall. A raised centerline is only an offline clearance result; native bridge trajectory and agent capsule still need testing.
- English nearest pilot-to-steering spacing is about 0.53 m. Five fitted spawn roles plus a rally point do not prove combat crew capacity or simultaneous occupancy.
- English climber landings are collision-supported candidates; rope reach, animations and water-to-deck movement remain unverified.
- Cog collision has eight measured height deviations of 0.275–0.641 m at ramps/structures. Check walking and fix where needed.
- Cog/carrack high platforms are separate navmesh islands. Keep them unoccupied until real access links work.

### 6. Complete and activate registrations per verified ship

Read `map-package/AssetSources/dadg_warsails/registration-drafts/REGISTRATION-STATUS.md` and the corresponding live Map authoring drafts. Current `.xml.in` files are incomplete identity/link drafts, not ready-to-load gameplay data.

- Calibrate mission mass/physics, rudder, sail force/counts, deployment area and landing depth to the actual prefab. Add oar fields only if working oars exist.
- Set tested crew capacities, health/sail health, ship type, cargo, campaign speed, seaworthiness, shallow-water capability, value/production and map scale. Register upgrade slots/pieces only where implemented.
- Copy completed mission/hull XMLs into Map `ModuleData`; merge their `XmlNode` entries into existing `SubModule.xml` (`MissionShips` before `ShipHulls`). NavalDLC dependency already exists.
- Add the new hull IDs to intended culture `available_ship_hulls` in `ModuleData/culture/dadg_cultures.xml`. Existing instructions suggest `vlandia`, whereas the runtime route mentions London / `empire`; reconcile the intended culture and actual eligible port before testing availability.
- Validate drafts with `rtk proxy python .codex-work/war-sails-ships/validate_registration_drafts.py`; then verify actual game XML load and object lookup. Static draft validation is not runtime acceptance.
- Enable each ship for controlled runtime testing only after its native authoring gate; finish mission/shipyard tests before calling it complete. Keep the other unfinished entries inactive.

### 7. Runtime acceptance for all three ships

Follow `runtime-validation.md` for the test procedure, using this handoff's newer asset status. Discover current GABS tools and actual console command signatures; no reliable ship-by-ID spawn command has been established. Do not invent one. Use the current installed version/path and a disposable campaign fixture, preserving its source save.

For **each** ship, record:

1. Exact custom hull, mission and prefab IDs loaded; correct visible ship and crew in a naval mission.
2. Stable flotation/waterline, forward motion, port/starboard turns and sail furl/unfurl; measure position/heading/state as well as screenshots.
3. Player and AI deck traversal, including controls and all intended occupied areas.
4. Boarding alongside another ship, bridge/navmesh connection and agents actually crossing.
5. Water-to-deck climbing with the intended climbers.
6. Availability/acquisition at the intended culture's shipyard and confirmed fleet ownership.
7. New save/reload preserving ownership, appearance, implemented upgrades and entry into another naval mission.

Write per-ship evidence under `runtime-evidence/ws-english-boat-b.feature.md`, `ws-kogge.feature.md`, and `ws-carrack.feature.md`: game version/commit, saves, ordered commands, screenshot paths, debugger facts, reproduction steps and explicit Passed/Failed/Partial/Inconclusive outcomes. No runtime test evidence exists yet.

## Operational rules and handoff cautions

- Read `AGENTS.md`, `dadg-project-context`, `bannerlord-modding-tools`, Computer Use and (for crash/runtime work) `bannerlord-debug` skills. Shell commands use `rtk`; no broad home-directory recursive scans or broad/integration `dotnet test` runs.
- User authorized agents and prefers cheaper models for routine work, Astra for difficult diagnosis. Give exactly one agent UI ownership; other agents can audit files or prepare scoped artifacts. Use the Blender specialist for further 3D authoring.
- Editor profile: `DellarteDellaGuerra.Integration: Standalone.Editor.WarSails` in `DellarteDellaGuerra.Integration/Properties/launchSettings.json`. Ensure **NavalDLC precedes every DADG module**. Verify the running process/load log, not just the selected profile. Rider is unnecessary for ordinary editor interaction when the editor is already running.
- Bring the editor foreground when needed. Use fresh Computer Use observations and the popup's own screenshot coordinates for picker selections. Stop input if Computer Use says the user stopped it; rediscover windows after restart.
- Never hand-edit TPACs or RuntimeDataCache. The native editor owns compiled writes. Preserve original Scenes assets, import identities, source backups and manifests.
- Do not rerun broad generation/conversion scripts over manually improved outputs without inspecting their overwrite behavior. Staged source, compiled import, native preview and game verification establish different facts.
- Last material success log: `C:/ProgramData/Mount and Blade II Bannerlord/logs/rgl_log_47056.txt` (save signal 17:24:51; preview load 17:25:10). For disappearance/crash, inspect Windows Application Event 1000 as well as RGL logs; an empty RGL error log did not exclude earlier native crashes.
- Keep status/audit files and reusable skill references updated as gates pass. Do not carry forward stale process/window handles or mark the overall goal complete after only an import or material preview.
