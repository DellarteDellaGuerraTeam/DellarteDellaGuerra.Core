# Native editor navmesh workflow

Read after the parent skill's computer-use setup. Menu labels below were exercised in the DADG editor; rediscover their positions and current controls instead of using coordinates from an earlier run.

## Select the right scene and region

Resolve the active game installation and module directories before comparing files:

| Purpose | Path relative to the game directory |
| --- | --- |
| DADG campaign | `Modules/DellarteDellaGuerraMap/SceneObj/Main_map/` |
| Shipped campaign reference | `Modules/SandBox/SceneObj/Main_map/` |
| DADG mission scenes | `Modules/DellarteDellaGuerraScenes/SceneObj/<scene>/` |

The shipped main map is in **SandBox**, even when called the "Native main map." Pair `navmesh.bin` with that scene's `scene.xscene` and terrain. Check the editor title and Studio's loaded scene independently; similar scene names do not establish that both loaded the same files. After external replacement, follow the parent skill's safe reload procedure.

For a repeatable Skye location, search the Scene tree for `dadg_sco_dunvegan_castle`, expand its ancestor if only the regional capsule appears, and double-click the castle. In the investigated DADG scene it was under `campaign_icon_capsule_dadg_region_Stornoway`, near world X=391.35, Y=886.07. These are a local navigation example, not a fixed placement contract; use the current scene's entity transform. **View > Move Camera To** can widen or reposition the view without moving the settlement.

## Inspect faces and level filtering

1. Open **Window > Visibility**. Record the active level names, the **Require all levels** / **Require at least one level** choice, and the **Navigation Mesh** visibility toggle before changing them.
2. Enter the toolbar's Navigation Mesh selection mode. Confirm `NavigationMeshInspector` and the Navigation Mesh Select status. Set **Mesh Edit Mode > Face** explicitly; edges or vertices alone do not prove a face can be selected.
3. Select one known face and inspect its **ID** and **Scene Level Visibilities**. The face's level checkboxes edit its stored mask; the Visibility window controls the display filter. Changing the display filter is the first diagnostic step for hidden faces, not a reason to alter the face.
4. If an island appears empty, compare the same camera location with a less restrictive display filter. In **Require all levels**, each requested bit must be present. Changing to **Require at least one level** can reveal faces excluded by the original conjunction. Restore the intended filter and verify the face under it.
5. Compare filled faces, edges, and terrain-type coloring while preserving the selected display mode in the evidence. Color-only coverage does not establish selection, connectivity, or runtime traversability. If faces remain absent across filters, inspect the loaded file, polygon topology, heights relative to terrain, and winding before generating replacement geometry.

The editor's conjunction and disjunction modes are described in [TaleWorlds' Scene Level System](https://moddocs.bannerlord.com/editor/scene-editor/level_system/). A terrain-type color mode does not make a different level mask correct.

## Keep terrain IDs and visibility masks separate

In Studio's NMG face representation, **`g3` is the face ID** (used for campaign terrain classification), and **`g5` is the signed 32-bit scene-level visibility mask**. Neither is a connected-island identifier. A face index is a separate identifier again. Mission scene IDs can have different scripted meanings; do not transfer campaign terrain meanings into a village or siege scene blindly.

Read the target scene's level definitions; bit assignments are scene data. The inspected SandBox and DADG main maps both defined:

| Level | Bit value |
| --- | ---: |
| base | 1 |
| level_1 | 2 |
| level_2 | 4 |
| level_3 | 8 |
| civilian | 16 |
| siege | 32 |
| looted | 64 |

Interpret negative masks as 32-bit bit patterns. With these definitions:

- **`-109` = `0xFFFFFF93`**, containing the defined levels **base + level_1 + civilian**. This was the most common shipped campaign mask: 13,304 of 17,153 faces in the inspected SandBox binary.
- The same shipped binary also contained **`-101`**, **`-103`**, and **`-69`**. These are evidence that authored exceptions exist, not invalid values to normalize.
- **`-1` = `0xFFFFFFFF`** includes all bits. Preserve an authored all-level mask when no change is needed.
- The earlier Skye mask **`-204` = `0xFFFFFF34`** included level_2, civilian, and siege, while excluding base and level_1. Thus faces could appear in one editor view and vanish with **Require all levels + level_1 + civilian**.

For **Require all levels**, test `(mask & requiredBits) === requiredBits`; for **Require at least one level**, test `(mask & requiredBits) !== 0`. Here the base/level_1/civilian bits sum to `19`, but **`19` is not byte-equivalent to the shipped `-109`** because the latter also retains high bits. The editor's Visibility panel may not expose base as a separate checkbox.

The Native distribution above is a measured local reference, not a universal constant across game updates or scene types. For a new bulk correction, compare the installed shipped scene and identify the wrongly generated faces first. Preserve authored exceptions, terrain IDs, costs, direction data, geometry, and protected regions. Do not apply `-109` to every face merely because it is the common campaign default.

## Verify the requested outcome

- Keep hashes/backups when replacing a binary so the file loaded by the editor is identifiable and reversible.
- Reopen the correct scene, return to the same location, restore the original display filter, and select representative land and restricted-terrain faces. Record their IDs and scene levels separately.
- Treat **Generation Tools**, **Fix**, **Weld**, **Remove**, and auto-generation controls as mesh-changing operations. The Inspector's Debugging Tools group contains modifying actions as well as diagnostic selections; do not click every button as a validation pass.
- For a visibility-only change, verify that geometry and terrain classifications remained unchanged. For topology or walkability changes, add relevant structural checks and the requested campaign/mission playtest. An editor **Find Path** result is still editor evidence, not a completed game traversal test.

Use `bannerlord-navmesh-preview` when an offline comparison helps, and `bannerlord-debug` for runtime investigation. Keep those evidence categories explicit in the final report.
