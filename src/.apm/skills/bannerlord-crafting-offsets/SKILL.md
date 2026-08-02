---
name: bannerlord-crafting-offsets
description: Diagnose and fix Bannerlord crafted-item reach, length, and piece alignment with the BannerlordCraftingTool CLI and FBX measurements. Use for smithing-preview gaps, overlaps, protruding shafts or tangs, lateral drift, incorrect weapon reach, inconsistent hidden-piece combinations, mixed-scale failures, and deciding between minimal BuildData offsets and a source-mesh correction.
---

# Bannerlord Crafting Offsets

Use `D:\Bannerlord\Tools\BannerlordCraftingTool\BannerlordCraftingTool.Cli` to inspect calculated crafted-item layouts from a module's declared XML data.

Prefer the existing Release executable when available:

```powershell
D:\Bannerlord\Tools\BannerlordCraftingTool\BannerlordCraftingTool.Cli\bin\Release\net8.0-windows\BannerlordCraftingTool.Cli.exe
```

Build the tool only when its Release output is absent or its source changed. This avoids unnecessary writes to the tool project's `obj` directory.

## Render (screenshot + 3D measurements) — preferred first look

Render the assembled weapon to an image plus precise 3D measurements so you can *see* a misalignment (rotation, lateral drift, axial gap) before deciding how to fix it. This is the fastest way to distinguish a bad offset from a bad mesh:

```powershell
dotnet run --project D:\Bannerlord\Tools\BannerlordCraftingTool\BannerlordCraftingTool.Cli\BannerlordCraftingTool.Cli.csproj --configuration Release --no-build -- render --submodule <mod>\SubModule.xml --weapon <CraftedItem id> --fbx-folder <mod>\AssetSources\weapons --out <dir>
```

Writes to `<dir>` (and echoes the JSON to stdout):

- `render.png` — side / front / top orthographic views in one image, pieces colour-coded by type. Read it to spot rotation, sideways drift, or gaps a single angle hides.
- `render.json` — per piece: `world_bounds`, `lateral_offset` `{x,z}` (drift off the weapon axis; ideal ~0), `axis_extent`; plus `seams[].axis_gap` (real surface-to-surface gap: `+` separation / `−` overlap), `assembled_bounds`, and `missing_meshes`.

Read the PNG and JSON together, then choose the fix:

- **Offsets** — pieces are the right shape but sit too far apart, overlap, or sit off-grip → edit `<BuildData>` (see Offset Guidance) and re-render.
- **Mesh** — a piece is rotated, mis-scaled, or authored off-origin (large `lateral_offset`, or a gap no offset closes cleanly) → hand the FBX to the `blender-3d-agent` to fix the mesh at source, then re-render.

`--fbx-folder` scans `.fbx` recursively and matches pieces to meshes by name (case-insensitive, `.lod0` tolerated). When the supplied folder is directly beneath `AssetSources` (for example, `AssetSources\weapons`), the CLI automatically expands the search to the enclosing `AssetSources` root so sibling collections such as `AssetSources\native-fbx` are included. The CLI is `net8.0-windows`, so this needs the Windows desktop runtime. Both `render` and `audit` normalize FBX `UnitScaleFactor` and `UpAxis` metadata before applying the crafting scale.

## Reach and handle placement

Bannerlord treats a centered handle with `length="L"` approximately as:

```text
distance_to_next_piece     = L / 2
distance_to_previous_piece = L / 2
```

The handle's forward reach contribution is approximately:

```text
shaft reach = (distance_to_next_piece + piece_offset) * scale
```

If the full centered handle should extend forward from the hand, use:

```text
piece_offset = distance_to_previous_piece = L / 2
```

For an off-centre handle mesh, measure its FBX bounds:

```text
distance_to_previous_piece = abs(minimum Y)
distance_to_next_piece     = maximum Y or authored socket position
piece_offset               = distance_to_previous_piece
```

Confirm the result against the reported weapon reach and visible FBX length. Do not use connector offsets to compensate for an incorrect handle pivot or reach.

## Connector model

Treat the three BuildData offsets as different tools:

- `piece_offset` places the piece in the assembly. On a handle it also changes grip-relative reach; do not use it as a seam patch.
- `previous_piece_offset` tunes this piece against the preceding/lower piece. Increasing it normally pulls the piece toward that joint and increases overlap.
- `next_piece_offset` tunes the following/upper piece from this piece. Increasing it normally pulls the following piece toward the joint and increases overlap.

Confirm signs with one render before applying a large correction. Piece order is normally `Pommel -> Handle -> Guard -> Blade`; collapsed polearm templates become `Handle -> Blade`.

Connector offsets are scaled by the piece that owns them:

```text
world correction = raw connector offset * owner scale
```

When transferring a correction between differently scaled pieces and preserving the exact world displacement:

```text
new raw offset = old raw offset * old owner scale / new owner scale
```

Then render the declared CraftedItem, because a raw one-for-one transfer is exact only when both owners use the same scale.

## Connector ownership

Put a correction on the narrowest piece that actually owns the mismatch:

- If one head gaps on every shaft, correct that head's `previous_piece_offset`.
- If one shaft gaps with every head, correct that shaft's `next_piece_offset`.
- If one guard gaps from every handle, correct the guard's `previous_piece_offset`.
- If every blade leaves the same visible shoulder gap above one guard, correct the guard's `next_piece_offset`.
- If one blade gaps above every guard, correct the blade's `previous_piece_offset`.

Do not leave a head-specific correction on a shared handle or a blade-specific correction on a shared guard. It contaminates every hidden combination. Transfer or split the correction between the matching pieces, then re-audit both the authored pair and all cross-pairs.

## Workflow

1. Build the CLI if its Release output is absent or source changed:

   ```powershell
   dotnet build D:\Bannerlord\Tools\BannerlordCraftingTool\BannerlordCraftingTool.Cli\BannerlordCraftingTool.Cli.csproj --configuration Release -v:minimal
   ```

2. Inspect one or more crafted items by XML id:

   ```powershell
   dotnet run --project D:\Bannerlord\Tools\BannerlordCraftingTool\BannerlordCraftingTool.Cli\BannerlordCraftingTool.Cli.csproj --configuration Release --no-build -- inspect --submodule <mod>\SubModule.xml --weapon <CraftedItem id> --out <inspection.json>
   ```

   Repeat `--weapon <CraftedItem id>` to return several requested items in one report, in the supplied order.

   `--submodule` is the only inspection workflow. It resolves the requested module and its dependency stack, then follows their declared XML pipeline for `Items`, `CraftingPieces`, and `CraftingTemplates` beneath `ModuleData`, including declared directory sources and later sibling XSLT/merge declarations. From each requested `CraftedItem`, it derives the `crafting_template`, its selected pieces, and their scale factors. Use `--modules-root <Modules directory>` when the target module is outside the standard sibling `Modules` directory.

   The report contains the resolved layout pieces and positions, joints and dimensions, plus `source_files` that contributed to the result.

   Scope: resolve the target module's dependency closure, not Bannerlord's complete active-module load order. The XML merge is purpose-built for crafting data, not a complete replacement for Bannerlord's schema-aware merger.

3. Read the report before editing XML:

   - `pivot`, `start`, `end`, `joints[].gap`, `weapon_length`, and `hand_to_bottom` are calculated at the selected scale.
   - A positive `gap` is a separation; a negative one is an overlap. Zero is a flush join.
   - `piece_offset`, `previous_piece_offset`, and `next_piece_offset` are raw XML values to edit. Their `scaled_*` counterparts explain the actual layout calculation.
   - Change only the relevant `<BuildData>` attributes, then rerun the same `inspect` command and compare every affected pivot and joint.

4. Classify the defect before choosing an offset:

   - one tip against all grips -> tip `previous_piece_offset`;
   - one grip against all tips -> grip `next_piece_offset`;
   - all tips above one guard -> guard `next_piece_offset`;
   - positive X/Z drift or rotation -> mesh correction, not a Y offset;
   - large negative overlap -> inspect for a sleeve, langet, tang, knuckle bow, or whole-weapon mesh before changing anything.

5. Edit the authoritative `ModuleData` XML only. Never edit `RuntimeDataCache`.

6. Re-render the declared item and rerun every affected pair at 90%, 100%, and 110%. Uniform scale sweeps do not prove mixed-scale correctness; declared CraftedItems and smithing screenshots may use different scales per piece.

7. Parse the XML and run `git diff --check`. Preserve unrelated worktree changes.

## Audit a Weapon Type

Use the FBX-backed audit for visual seams rather than inferring them from XML pivots alone:

```powershell
dotnet run --project D:\Bannerlord\Tools\BannerlordCraftingTool\BannerlordCraftingTool.Cli\BannerlordCraftingTool.Cli.csproj --configuration Release --no-build -- audit --submodule <mod>\SubModule.xml --fbx-folder <mod>\AssetSources\weapons --all-templates --out <audit.json>
```

Use repeatable `--template <CraftingTemplate id>` instead of `--all-templates` to limit a sweep. Use `--modules-root <Modules directory>` for nonstandard layouts and `--tolerance <raw XML units>` to define the permitted visual seam (default: `0.01`).

The audit uses the visualiser's FBX node/mesh matching, transforms, scale, and engine-equivalent pivots. It runs a fixed-reference physical-piece sweep: each usable candidate is tested against the first usable piece at the next physical joint. Templates containing the intentional empty `default_polearm_guard` or `default_polearm_pommel` collapse those declared slots, so polearm heads are audited directly against their handles instead of against empty placeholders. Review records with `visual_gap`; positive is a separation and negative is an overlap. `missing_meshes` and `skipped` are incomplete evidence, not alignment results. Correct only the cited `BuildData` connector, then rerun the same audit. Do not invent a manual JSON manifest or raw-file workaround.

For a complete visual compatibility audit of pieces the player can combine, add `--all-pairs --render-dir <dir>`. This replaces the fixed-reference sweep with the Cartesian product at every physical joint and writes one compact orthographic PNG per pair under `<dir>\pairs`. It also writes individual piece PNGs grouped by template and declared piece type under `<dir>\pieces`, which is useful for spotting a blade, guard, handle, or pommel assigned to the wrong category. The main audit JSON links every pair and gallery entry to its PNG, includes full 3D measurements, and reports collapsed placeholder slots.

Distinguish two meanings of "hidden":

- an XML piece explicitly marked `is_hidden="true"`;
- a selectable pair that has no declared CraftedItem but is still reachable through smithing.

For the first, inventory the flagged piece IDs and filter the all-pairs report to records involving them. Remove `is_hidden="true"` only after their affected joins pass. For the second, compare the Cartesian pairs with the pairs represented by declared CraftedItems.

## Bounds can hide visible gaps

`visual_gap` uses complete mesh bounds. A thin tang may extend into a guard while the broad blade shoulder visibly floats above it; the report then shows a negative overlap even though the screenshot has a real gap. Conversely, a long sleeve or langet can produce a very large negative value while looking correct.

Always read the PNG and JSON together:

- For a visible shoulder gap shared by all blades on one guard, move the blades with the guard's `next_piece_offset`.
- For a shaft protruding through the top of a head, compare `handle.max_y` with `head.max_y`. The protrusion is approximately `handle.max_y - head.max_y`; move the head outward by that amount and re-render.
- Do not treat negative bounds alone as a defect. Check whether the overlapping geometry is an authored socket, strap, tang, bow, or integrated segment.
- If the visible defect is lateral X/Z drift, rotation, or a component mesh spanning multiple logical slots, fix/re-author the FBX or remove the piece from the mixed template.

## Scale coverage

Run all-pairs audits at 90%, 100%, and 110%, but also render the actual CraftedItem scales. A connector on a 95% guard with raw offset `2.0` moves the blade by `1.9` world units. Smithing can retain different size settings per piece, so use the user's screenshot as evidence when a uniform audit looks correct.

## Offset Guidance

- Prefer a connector offset over `piece_offset` for a seam-only correction.
- Preserve authored deep sockets when they are visually intentional.
- When a shared connector carries a pair-specific correction, move the correction to the matching tip or split it so cross-pairs remain compatible.
- Test the maximum-gap pair and the authored matching pair after every edit.
- Keep training/whole-weapon meshes hidden or re-author them; offsets cannot make an integrated mesh interchangeable.
- Preserve unrelated attributes and report the exact XML snippets changed.

For concrete successful patterns and calculations, read [diagnostic-patterns.md](references/diagnostic-patterns.md).
