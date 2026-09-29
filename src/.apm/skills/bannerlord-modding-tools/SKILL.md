---
name: bannerlord-modding-tools
description: Operate the Bannerlord Modding Tools scene editor through Windows computer use. Use to open or inspect scenes, locate entities, position the camera, edit scene properties, or verify navmesh changes in the native editor. Requires the installed computer-use plugin; offline previews and game runtime playtests use their respective skills.
---

# Bannerlord Modding Tools

Use the existing scene editor when available. Match the requested scene and the user's existing authorization before changing it. For DADG module locations, read the relevant `dadg-project-context` reference. For navmesh visibility, terrain IDs, or external binary replacement, read [the navmesh workflow](references/navmesh.md).

For DADG War Sails ship asset imports or nested prefabs, read [the ship authoring workflow](references/war-sails-ships.md) before editing or relaunching the native editor.

## Connect through computer use

Read the installed **computer-use** skill from the current skill catalog, then its `../../docs/guidance.md`, `api.md`, and `confirmations.md`. Resolve these against that installed skill; do not assume a plugin version or installation path. Apply existing session authorization when deciding whether an action needs confirmation; this skill adds no blanket approval step.

Use `node_repl` and `@oai/sky` for all desktop observation and input. Shell tools may inspect or edit project files, but must not drive the desktop, launch a custom computer-use helper, or inject UI input. If the required plugin/tools cannot be discovered, report the missing dependency and continue only work that does not need the editor.

Initialize and discover open windows in a `node_repl` cell:

```js
if (!globalThis.sky) {
  const { sky } = await import("@oai/sky");
  globalThis.sky = sky;
}
globalThis.btWindows = await sky.list_windows();
nodeRepl.write(JSON.stringify(btWindows.filter(w =>
  /Win64_Shipping_wEditor/i.test(w.app) || /^Edit Mode:/i.test(w.title || "")
), null, 2));
```

Inspect the returned candidates. Select exactly one returned `Window` by the requested scene and application, retaining its `id` and `app`; never invent handles or reuse a previous session's IDs. Use `sky.get_window({ id, app })` to rehydrate that returned object when needed. A launcher or loading window is not the editable scene. If the editor is absent and opening it is in scope, follow the plugin's `list_apps` / `launch_app` workflow for the installed Modding Tools, then rediscover its window.

For DADG's campaign, the scene title should resolve to `Modules/DellarteDellaGuerraMap/SceneObj/Main_map/scene.xscene`. The executable may be named `TaleWorlds.MountAndBlade.Launcher.exe` even while the scene editor is running; use the `Win64_Shipping_wEditor` application path and scene title together.

## Observe, act once, refresh

1. Capture `sky.get_window_state({ window: btWindow })` and inspect the automatically displayed screenshot. Request `include_text: true` when accessibility labels or focus will help; this editor can return a null accessibility tree.
2. Confirm the screenshot actually depicts the selected editor or its expected dialog. Metadata alone is insufficient. If it instead shows the foreground app, rehydrate the returned editor window, call `sky.activate_window`, and recapture once. If the mismatch persists, stop input and report it.
3. Choose one action from that observation, perform it, then immediately refresh. Use an accessibility `element_index` from the latest tree, or window-relative coordinates with the corresponding fresh `screenshotId`. Do not reuse coordinates or indexes after a menu, dialog, docking, resize, maximization, or other layout change.

Use the plugin's action examples and current API signatures. `click` supports `click_count: 2`; `press_key` takes keys such as `Control_L+a`, `Return`, or `Escape`; `type_text` sends literal text. Before typing, focus the observed editable control and refresh to verify focus. Treat failed input as an unknown outcome: inspect again before retrying.

Floating inspectors and dialogs can appear as separate windows or screenshot regions. If a modal is missing from the editor capture, rediscover it with `list_windows`, select the returned dialog, and capture it. After closing it, rediscover or refresh the editor; it may have returned to a different window size. If an input is rejected because another window covers the target, use the plugin's activate-and-recapture recovery rather than clicking through it.

When diagnosing a hung capture, call `sky.get_window` and `sky.get_window_state` in separate `node_repl` cells so the blocked operation is identifiable. A requested timeout has been observed not to release a hung editor capture. Stop UI actions after such a hang; do not retry indefinitely or treat successful window discovery/metadata as visual validation. Report native editor verification as unavailable until a fresh capture actually succeeds.

## Work in the scene

| Task | Editor workflow |
| --- | --- |
| Open a scene | Use **File > Open Scene**, inspect the displayed browser/recent entry, and verify the module and scene in the loaded window title. Handle an unsaved-changes prompt using the reload rules below. |
| Locate an entity | Type its exact name in the **Scene** tree search. Expand matching ancestors: a filtered result can initially show only its regional parent. Double-click the intended entity to frame it; verify the selected name and Inspector before editing. |
| Position the camera | Use **View > Move Camera To** for explicit world X/Y/Z. Identify each labeled field in the live dialog. Raising Z gives a wider view; do not confuse camera coordinates with an entity's transform. |
| Edit a property | Select the intended entity and inspect its current value. Change only the requested field, commit with the control's observed behavior, and read the resulting value. On Qt numeric fields, `set_value` may fail with a CacheRequest error: reobserve, focus the field, select its contents with `Control_L+a`, then type, refreshing between actions. |
| Inspect navigation | Enter the toolbar's Navigation Mesh selection mode, then use **Mesh Edit Mode** to choose **Vertex**, **Edge**, or **Face**. Verify the mode in the Inspector/status bar before clicking the viewport. See the navmesh reference. |

For view controls or unfamiliar editing operations, inspect the current menus and tooltips; read the relevant [TaleWorlds editor documentation](https://moddocs.bannerlord.com/editor/scene-editor/) when needed. Do not guess camera chords or apply entity-edit shortcuts while in navmesh mode. Some documented shortcuts depend on viewport focus; the visible selector is more reliable when a toolbar or text box owns focus.

## Save and reload deliberately

- Before file-affecting work, establish whether the scene has unsaved user edits and retain a backup of the files the task can change. Read-only inspection does not require saving or a new backup.
- After authorized editor edits, use the observed **Save** control and verify its completion. Check the affected files when persistence matters; saving a scene can write more than `scene.xscene`.
- After an external tool replaces `navmesh.bin` or another loaded scene file, reopen the same scene to load the disk version. Do **not** save the old in-memory scene over that replacement. Choose **No / Don't Save** only when the changes being discarded are known stale state and no unrelated unsaved work needs preserving. If that is unknown, cancel the reload and resolve those edits before discarding them. Existing authorization to reload/discard that known state remains valid.
- Do not assume navmesh editing supports Undo. TaleWorlds documents that `Ctrl+Z` can undo a scene change instead of the navmesh change; use the prepared backup/reload route for rollback. [Navmesh documentation](https://moddocs.bannerlord.com/editor/scene-editor/nav_mesh/)

Report the exact scene, location/entity, relevant mode/filter, observed result, and what was saved or reloaded. Editor selection or visible coverage establishes only those editor facts. Validate campaign party routing or mission agent traversal separately when those are part of the requested outcome.
