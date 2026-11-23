# War Sails: kogge and carrack decks and swivel guns

Date: 2026-10-23. Scope: the DADG War Sails kogge (cog, medium hull) and carrack (heavy hull).

## Goal

1. Make each ship's upper decks AI-navigable, adding stairs where there were none.
2. Mount a historically plausible gun for 1471, reusing DADG's siege cannon assets.

## 1. Deck navigability

Before this work, both ships had coarse navmeshes that covered only the main deck. The carrack had no stairs up to its castles at all.

### What changed

**Carrack**
- Stairs added up to the forecastle and the aftercastle/poop.
- Collision for the stairs lives in `DellarteDellaGuerraMap/NestedPrefabs/dadg_ws_carrack_ship_nested/`.
- `NavMeshPrefabs/dadg_ws_carrack_navmesh.bin` was rebuilt with **353 faces**.

**Kogge**
- 30 physics-only stair treads (`bo_editor_cube`) were added in `dadg_ws_kogge_ship_nested/mesh_holder.xml`.
- The navmesh was replaced with a conforming **228-face** mesh.
- Spawn points in `spawn_point_holder.xml` were moved onto navmesh faces.

**Backups** are in the session scratchpad: `backup_carrack_stairs_20261023/` and `backup_kogge_20261023/`.

### Rules applied

These are in the bannerlord-agent brain: `ws-ship-deck-stairs-need-collision` and `ws-ship-navmesh-boarding-face-rules`.

- A navmesh ramp needs physics collision under it.
  - `bo_editor_cube` is a unit box, x/y ±0.5 and z 0..1.
  - Each tread's top is kept flush with the ramp.
- The navmesh is conforming, with CCW tris and quads only and g1=2, g5=−1.
- Each boarding id (g3) is on exactly one face, and that face has an unshared edge.
- Each attachment machine's origin lies on its g3 face, and each spawn point lies on a face.

### Known caveats

- The kogge stairs are about 62°, so they behave like ladders. The castle notches they lead into are narrow.
- The carrack's B stairs clip the poop-deck rail. This is visual only, but noticeable.
- The carrack navmesh has 2 boarding ids. Re-check them if boarding misbehaves.

## 2. Guns

### Historical basis (1471)

Ships of the 1470s carried **small guns**:
- swivel pieces (serpentines, versos, "murderers") on rails and in the castles;
- light breech-loaders on the decks and in the fore- and aftercastles.

Lidded hull **gunports** for heavy guns between decks are credited to around **1501**. A heavy hull battery would therefore be anachronistic. The decision is **small guns on deck and in the castles only**, and no hull guns.

### Asset choice

No swivel-gun asset exists in DADG, Native or NavalDLC. These DADG cannon meshes are not yet used:
- the bombard set (`dadg_siege_bombard*`);
- the mortar (`dadg_siege_mortar*`);
- `dadg_siege_falconet_mantlet`.

All of them are siege pieces, too heavy or the wrong form for a ship's deck.

The swivel is therefore built from the **falconet barrel** (`dadg_siege_falconet_barrel`) mounted on the vanilla War Sails ship-ballista **pedestal and yoke** (`ballista_e_stand`, `ballista_e_navel`):
- It is driven by the vanilla `ShipBallista` script, so it needs no new C#.
- The navel yaws, which gives the swivel motion. The body pitches.
- The barrel is placed so the pivot sits on the bore, 0.85 m ahead of the breech and 1.02 m behind the muzzle (trunnions just breech-side of the middle, like a real gun). It was 0.35 m from the breech until 2026-10-24, which left the barrel overhanging the stand.
- Ammo is a new item, `dadg_ship_falconet_ball`. It is modelled on vanilla `ballista_c_projectile_iron` (BallistaBoulder, ship damage 2400) and uses DADG's small cannonball mesh, cannonball physics material and cannonball trail.

### Several guns, one slot

`MissionShip` holds a single `ShipSiegeWeapon`, the last RangedSiegeWeapon it finds. Campaign ammo and `ShipOrder` AI crewing only reach that one. See the brain note `ws-one-siege-weapon-per-ship`.

The extra guns still work on their own:
- the player can use them;
- they aim and fire by themselves once crewed;
- they take their ammo from the prefab's `StartingAmmoCount` (30).

All the guns of a ship therefore hang under the **one** existing `dadg_gun_slot`. The slot loader keeps every child tagged `dadg_ship_falconet`, so the one `dadg_bow_falconet` piece arms them all. Saves stay compatible, because the slot and piece ids did not change.

`ShipSwivelGunCrewMissionLogic` (`DellarteDellaGuerra.Integration/Naval`) crews the extra guns. Once a second, for each tagged gun that is not the ship's `ShipSiegeWeapon` and has no pilot, it does what `ShipOrder` does for the main gun:
1. It joins the ship formation to the gun (`StartUsingMachine`).
2. It seats the nearest free AI agent within 15 m on the pilot point.
3. Before seating, it calls `RangedSiegeWeaponAi.InitializeThreatSeeker()` on the gun.

Two vanilla traps shaped this (both found live on 2026-10-24):
- **No agent is ever free.** Once the ship is set up, `ShipOrder` parks every idle sailor in the ship's `ShipPlacementDetachment`. Those agents are `IsDetachedFromFormation`, so `CanBeAssignedForScriptedMovement()` is false for all of them. The helper does what `ShipOrder` does when it needs a sailor (`PickLastAgent`): `RemoveAgent` from the placement detachment, `formation.AttachUnit`, then `AddAgentAtSlotIndex` on the gun.
- **The target list starts empty.** `ThreatSeeker._potentialTargetObjects` is only filled by `InitializeTargetableObjects()`, and vanilla calls that only for `ShipSiegeWeapon`. Without it, an AI pilot on an extra gun throws an NRE in `GetAllThreats` on its first target search.

It skips the gun when the ship is undeployed, abandoned or sinking, and when the gun is out of ammo. It detaches the gun again when the ship changes formation. `MissionShip` is read through reflection, because NavalDLC is optional.

### Placement

The guns are swivels on the castle rails, which is where 1471 ships mounted their murderers and serpentines. Each one faces outboard (yaw ±π/2, or π for the kogge bow chaser) with an arc of ±74° (`HorizontalDirectionRestriction` 1.3).

Placements were checked with a navmesh fit script (`fit2.py`):
- the pedestal footprint (r 0.5, 8 probes) is on the navmesh;
- the pilot point, 1.15 m inboard, is on the navmesh;
- the rail is 0.6–1.0 m outboard;
- the stair heads stay clear.

| Ship | Gun | Position (x, y, z) | Yaw |
|---|---|---|---|
| Kogge | aftercastle starboard | (1.5, −8.0, 5.465) | +π/2 |
| Kogge | aftercastle port | (−1.5, −8.0, 5.465) | −π/2 |
| Kogge | forecastle bow chaser | (0, 8.6, 5.486) | π |
| Carrack | forecastle starboard | (1.5, 8.0, 4.195) | +π/2 |
| Carrack | forecastle port | (−1.5, 8.0, 4.168) | −π/2 |
| Carrack | aftercastle starboard | (1.6, −2.7, 3.794) | +π/2 |
| Carrack | aftercastle port | (−1.6, −2.7, 3.794) | −π/2 |
| Carrack | poop, starboard quarter | (0.5, −8.2, 5.651) | 0.9 (arc ±60°, 1.04) |

On the carrack aftercastle, x 1.7 left one pedestal probe off the deck (7/8), so 1.6 was used.

The aftercastle pair moved from y −2.3 to −2.7 on 2026-10-24. That pulls them away from the side-stair landings at about (±2.15, −0.8): `lanes.py` (agent radius 0.3, flood fill from the stair tops) puts 49% of the aftercastle deck within reach, against 34% before. The poop gun's arc is narrowed to 1.04 rad so it can't swing into the mizzen at about (−0.08, −8.9).

Layouts the path check rejected, because they block a stair or a gunner:
- 2 guns per side on the carrack aftercastle;
- 2 staggered guns on the kogge forecastle;
- a kogge stern chaser.

So the kogge stays at 3 guns. Waist guns on the carrack would need gunports: the bulwark is about 1.95 m above the deck, while the bore is at 1.31 m.

### Crew size

`MissionShip.CrewSizeOnMainDeck` is `min(deck spawn frames, MainDeckCrewCapacity)`, where the frames are the inner and outer deck points plus the captain. Raising the hull number alone adds no agents.

The editor check also wants `frames >= main_deck * 1.05`, to allow for the Popular Captain perk.

| Ship | Hull (skeletal / main / total) | Deck frames |
|---|---|---|
| Kogge | 8 / 20 / 30 (unchanged) | 28. Seven points sitting on the new guns were moved. |
| Carrack | 24 / 56 / 100 (was 10 / 30 / 60) | 60 (was 38). Eight points sitting on the guns were dropped, and 30 were added. |

The points come from `spawngen.py`. It places them only on flat navmesh faces, at least 0.3 m from any edge and 0.5 m from any stair face, clear of the guns, pilots, helm, mast and boarding hooks, and at least 0.6 m apart. Vanilla ships use 0.5–0.8 m. The other 44 carrack men come aboard as reinforcements from `sp_troop_crew_spawn`.

### Data wiring

See the brain note `ws-ship-upgrade-slot-mechanism`.
- A gun slot entity tagged `upgrade_slot` plus the slot key, holding a gun child tagged with the piece's `slot_prefab_child_id`, in each ship's nested prefab.
- A ShipUpgradePiece with `TargetSlots`, `siege_engine`, port level 1 and no culture requirement, so any port sells it and AI lords can fit it.
- A new SiegeEngineType for the campaign simulation.
- `AvailableSlots` entries on the kogge and carrack hulls in `dadg_custom_ship_hulls.xml`.
- New XmlNodes in `DellarteDellaGuerraMap/SubModule.xml` for the new files.

Implemented (2026-10-23). All paths are in `DellarteDellaGuerraMap`. The new files validate against the game XSDs.

| What | Where |
|---|---|
| Ammo item `dadg_ship_falconet_ball` (BallistaBoulder, 2400, modelled on `ballista_c_projectile_iron`, cannonball mesh, trail) | `ModuleData/dadg_naval_items.xml` |
| SiegeEngineType `dadg_bow_falconet` | `ModuleData/dadg_naval_siegeengines.xml` |
| ShipUpgradePiece `dadg_bow_falconet`, `slot_prefab_child_id="dadg_ship_falconet"` | `ModuleData/dadg_ship_upgrade_pieces.xml` |
| ShipSlot `dadg_bow_gun` (`type_id="fore"`, so the UI shows the vanilla "fore" slot name) | `ModuleData/dadg_ship_slots.xml` |
| `<ShipSlot id="dadg_bow_gun" tag_id="dadg_gun" />` on `dadg_ws_kogge` and `dadg_ws_carrack` | `ModuleData/dadg_custom_ship_hulls.xml` |
| Entity `dadg_gun_slot` (tags `upgrade_slot`, `dadg_gun`) at the ship origin, holding `dadg_ship_falconet_1..n` (each tagged `dadg_ship_falconet`, with its own transform) | `NestedPrefabs/dadg_ws_{kogge,carrack}_ship_nested/control_point_holder.xml` |

The gun entity is generated from the vanilla `ballista_c` tree:
- It keeps the pedestal (`ballista_e_stand`) and the yoke (`ballista_e_navel`).
- The ballista body has no mesh or collision. Instead the body holds `barrel_visual` (`dadg_siege_falconet_barrel`, rotated π, offset (0, -0.60, -0.82)), so the bore sits on the pitch pivot.
- The projectile is `small_cannonball` at body y -0.80, and `projectile_leaving_position` is at y -1.06 (muzzle at -1.02). The camera is at body (0, 1.50, 0.35) and the pilot `use_pos` at (0, 1.15, 0), clear of the breech at body y +0.85.
- The skeleton is an empty `ballista_skeleton`. The script needs Skeletons[0].
- The pile is `vegulaire_cannonball_pile`.
- When destroyed, it shows the pedestal debris plus a fallen barrel.

ShipBallista settings: `BallistaShootingSpeed` 110, `HorizontalDirectionRestriction` 1.3 (±74°), pitch -0.35/+0.5.

The generator script (`gen_gun.py <folder> <key> x,y,z,yaw ...`) is reproducible. It is idempotent: it replaces an existing `dadg_gun_slot`. Pre-change backups are in the session scratchpad (`backup_gun_20261023`, `backup_rails_20261024`).

## Status / still to verify

- [x] Deck stairs and navmeshes built for both ships (offline checks only).
- [x] Swivel gun prefab, items and XML written and XSD-validated (offline).
- [x] Game launches with the single bow gun; the player placed and used it (2026-10-23).
- [x] Rail layout (3 kogge / 4 carrack guns) loads, all guns are crewed and fire, carrack shows the larger crew (2026-10-24):
  - all 10 falconets were piloted at about 280 s; an extra kogge gun went from 29 to 26 ammo;
  - this was with the two crew fixes above applied by debugger eval; the code version is built (11:41) and needs a fresh battle;
  - the carrack fielded 56 sailors and each kogge 20.
- [x] Decks used (2026-10-24 snapshot, ship-local z). Carrack: about 30 at the main deck (~2.3) and 15 at the castles (3.7-4.3); 6 were caught between levels (2.6-3.3), which is the stair run. Kogge: 9-10 at the castle (5.5) and the rest on the main deck (~1.8).
- [x] Code build (11:41) in a fresh battle, no debugger help (2026-10-24, t=75-177 s):
  - all 4 carrack guns and all 3 guns on two kogges piloted; extra guns fired on their own (falconet_3 26→19 and 27→21 ammo);
  - the third kogge's guns stayed empty because its formation had no men left, so there was nobody to seat;
  - `Scene.GetPathDistanceBetweenPositions` connects the carrack main deck to the aft castle, the castle top and the fore castle, with paths longer than the straight line (they run via the stairs).
- [x] AI walks the stairs (2026-10-24). Carrack sailor 0 was taken out of `ShipPlacementDetachment` (`RemoveAgent` + `AttachUnit`) and given `SetScriptedPosition` to a fore-castle point. Ship-local (x/y/z): t=208.2 at 0.3/−3.7/2.3 (main deck), t=223.4 at 0.5/8.6/4.4 (fore castle). That is 12.3 m forward and 2.1 m up in 15 s.
  - While a sailor is still in the placement detachment, `SetScriptedPosition` does nothing; the detachment owns its position. Release the sailor first.
- [ ] Live naval battle: the gun aims across ±74° and damages ships.
- [ ] Boarding still works on both ships, including the carrack's 2 boarding ids.
- [ ] Known cosmetic gaps:
  - the gun still plays the ballista crank (reload) animations;
  - it still uses the hardcoded `event:/mission/ballista_naval/*` sounds.

  An optional Harmony patch could add cannon sounds and a muzzle flash (`psys_cannon_shot_1`). It must be verified in an offline harness first, because a patch failure kills the game silently.

## 3. Helm: whipstaff (carrack) and tiller (kogge)

### Why it moved

Both ships kept the vanilla `ShipControl_nested` near the ship origin.
- On the carrack that put the pilot inside the main mast, at (0, 0, 2.34).
- On the kogge, at (0, -4, 1.9), the pilot stood at the foot of the only aft stair.

Both helms now sit aft on the castle deck, with a custom moving lever. No vanilla tiller mesh is reused. `english_boat_b` is unchanged.

Historical basis:
- A 15th-century carrack was steered from below the poop with a **whipstaff**: a vertical staff through a pivot (rowle) in the deck. Its lower end drove the tiller, so the helmsman moved the top of the staff athwartships.
- A cog was steered with a **tiller** slotted straight into the rudder head over the stern.

### How the engine animates it

`ShipControllerMachine.OnInit` (NavalDLC) scans the machine's **direct** children:
- `controller` becomes the rotating entity, and the engine caches its initial local frame.
- `controller/hand_position` becomes the right-hand IK target.
- A direct `hand_position` child would override that.
- The tagged `rudder_rotation_entity` is used only when there is no `controller`. Neither DADG ship has that tag, so nothing on these ships rotated before this change.

Every tick the engine sets `frame = initial; frame.rotation.RotateAboutUp(VisualRudderRotation)`. In other words, the controller turns about **its own local up axis** by the rudder angle (±17° = ±0.297 rad on both hulls). The pilot plays `act_rudder_backward_push_idle` or `act_rudder_backward_pull_idle`, and `_isRightHandOnly` = 1 pins the right hand to `hand_position`.

The vanilla prefab cannot hold a controller, because it has its own direct `hand_position`. So `ShipControl_nested` is now **unlinked** (`prefab guid` = 0) in both ships. All the vanilla variables and flags are written out explicitly, and the old instance guids are kept. Every descendant carries `does_not_affect_parents_local_bb`, which `CheckControllerMachineFlags` requires.

### Swing-axis maths

Mat3 rows are (s, f, u). `RotateAboutUp(a)` gives s' = s·cos a + f·sin a and f' = f·cos a − s·sin a.

- **Carrack whipstaff.** The controller has Euler (−π/2, 0, 0), so s = (1,0,0), f = (0,0,−1), u = (0,1,0).
  - The pivot axis is the ship's fore-aft axis, so the staff swings athwartships.
  - The staff mesh runs along local −y, which is world up. Its direction is −f' = (sin a, 0, cos a).
  - For a > 0 the top goes +x, toward the pilot, so the engine plays *pull*.
  - The hand is 0.90 m up the staff, so the swing is ±0.27 m (vanilla tolerates about ±0.7 m).
- **Kogge tiller.** The controller has identity rotation, and the pivot is the rudder stock (vertical).
  - The tiller runs along local +y, so f' = (−sin a, cos a, 0).
  - For a > 0 the end goes −x, toward the pilot, so the engine plays *pull*.
  - The hand is 1.30 m out, so the swing is ±0.38 m.

### New positions (ship-local)

| | Carrack | Kogge |
|---|---|---|
| `ShipControl_nested` | (0, −3.700, 3.869), aft castle deck, ahead of the hatch | (0.034, −11.428, 5.465), coaxial with the rudder stock |
| `controller` (rotates) | (0, 0, 0.14), rot (−π/2, 0, 0), mesh `dadg_ws_whipstaff` | (0, 0, 0), identity, mesh `dadg_ws_kogge_tiller` |
| `controller/hand_position` | (0, −0.90, 0), world height 1.04 m above the deck | (0, 1.30, 1.00), 1.0 m above the deck |
| static frame | `whipstaff_frame`, mesh `dadg_ws_whipstaff_frame` (deck pad, cheeks, axle pin) | — (the rudder head is part of the tiller mesh and turns with it) |
| `pilot` | (0.45, −0.19, 0), yaw +π/2 (faces port) | (−0.45, 1.49, 0), yaw −π/2 (faces starboard) |
| `shoulder_camera_target` | pilot + 1.15 m up | pilot + 1.15 m up |
| `camera_target` | (0, 5.1, 12.0) | (−0.034, 10.0, 10.5) |
| `front_camera_target` | (0, 15.9, 1.0) | (−0.034, 20.5, 0) |

The camera targets put both ships' aim point near the ship centre. Without that, the camera would orbit the stern.

In both ships the hand sits 0.45 m in front of the pilot and 0.19 m to his right, close to vanilla's 0.34 / 0.19.

Clearances:
- Carrack pilot: clear of the stair ramps (x ≥ 1.05) and about 1.6 m from the falconet seats at (±0.75, −2.30).
- Kogge pilot: on the walkable castle deck (x ±1.3 at y −11), clear of the stern transom (y ≤ −12.3) and about 1.6 m from the falconets at (±0.65, −8).

Cameras: the vanilla machine defaults (`_cameraOffset` 0/−20/5, no camera targets) are sized for small ships. Both DADG ships now use the vanilla cog's camera recipe, scaled out:
- `_cameraOffset` (0, −56, 16)
- `_frontCameraOffset` (0, 3.2, 0)
- `_shoulderCameraOffset` (−0.85, 0.15, −1.35)

Spawn points that overlapped the new helms were moved:
- carrack (0.6, −3.9, 3.87) → (0.9, −8.1, 5.633)
- carrack (0, −3.6, 3.87) → (−1.0, −6.3, 5.302)
- kogge (0, −11, 5.465) → (0.7, −10.9, 5.465)

All three new spots are on the navmesh (8 of 8 ring probes hit).

### Meshes

Source: `DellarteDellaGuerraMap/Tools/WarSailsShips/external-authoring/data/dadg_ws_helm_v1.py` (Blender 5.1, run headless).

Output:
- `AssetSources/dadg_warsails/helm/dadg_ws_helm_v1.fbx` and `.blend`
- the preview `Tools/WarSailsShips/previews/dadg_ws_helm_v1.png`, which shows the meshes at ±17° and a 1.8 m pilot

| Meta mesh | Size (m) | Tris | Pivot |
|---|---|---|---|
| `dadg_ws_whipstaff` | 0.16 × 1.47 × 0.24 | 564 | rowle axle (local −y = up the staff) |
| `dadg_ws_whipstaff_frame` | 0.63 × 0.49 × 0.26 | 224 | deck surface under the axle |
| `dadg_ws_kogge_tiller` | 0.27 × 1.65 × 1.45 | 256 | rudder-stock axis at deck level |

Materials: `dadg_ws_dawa_rich_wood` and `crude_iron`. Export uses `axis_forward='Y', axis_up='Z'` and `apply_scale_options='FBX_SCALE_ALL'`. The script's byte gate checks that no model node carries a non-1 `Lcl Scaling`.

Note: the older DADG ship FBXs carry `UnitScaleFactor` 1 with node `Lcl Scaling` 100. That is the signature the generic brain calls corrupt, yet those ships work. This FBX has the healthy signature (`UnitScaleFactor` 100, no node scaling). Check the size once after import: the whipstaff top should stand about 1.25 m above the deck.

### Mesh gating and editor steps

The XML does **not** reference the new meta meshes yet. They do not exist until the FBX is imported, and the ships must load either way. Until the import, the controller and frame are empty transforms; the pilot, hand IK and cameras already work.

1. Close the ship prefabs in the editor if they are open; saving them from a stale editor copy would overwrite this XML.
2. In the Modding Kit editor's Resource Browser, import `DellarteDellaGuerraMap/AssetSources/dadg_warsails/helm/dadg_ws_helm_v1.fbx` (via the Discovered Files list). It yields three meta meshes in `Assets/dadg_warsails/helm/dadg_ws_helm_v1_geo.tpac`. The same FBX at the `AssetSources` root failed with "Package path must contain /Assets/ element" (2026-10-24); the subfolder copy imported fine.
3. Open each meta mesh. Check that the material is `dadg_ws_dawa_rich_wood` / `crude_iron` and not `default`, and that the whipstaff is about 1.5 m long. Save.
4. Run `python Tools/WarSailsShips/external-authoring/data/dadg_ws_helm_enable_meshes_v1.py`. It aborts if the tpac is missing and is idempotent. It adds the `meta_mesh_component`s to `controller` (both ships) and `whipstaff_frame` (carrack).
5. Relaunch the game.

Backups of the four touched XML files: session scratchpad `backup_helm_20261024/`.

### Test checklist

- [ ] Both ships load in a naval battle with no crash, both before and after step 4.
- [ ] Carrack: the pilot stands aft of the hatch on the castle deck, facing port, not inside the main mast. Kogge: the pilot stands aft by the rudder head, facing starboard, and the aft stair foot is free.
- [ ] The right hand rests on the staff or tiller end and stays on it while steering.
- [ ] The staff or tiller swings with the rudder, in the same sense as the hand: the hand pulls toward the body on one turn and pushes away on the other.
- [ ] AI-crewed ships still steer, and their pilots are seated.
- [ ] The player can take the helm, and AI sailors still capture it.
- [ ] Cameras: the back camera frames the whole ship and does not clip into the sterncastle. The front camera looks over the bow. The shoulder camera sits behind the pilot's head.
- [ ] Troops spawn without stacking on the helm.
