---
name: bannerlord-crafting-offsets
description: Diagnose and fix Bannerlord crafted-item piece alignment with the BannerlordCraftingTool CLI. Use when an agent needs to render a screenshot plus 3D measurements of an assembled CraftedItem, calculate its layout from a mod's SubModule.xml, inspect the resolved Items/CraftingPieces/CraftingTemplates data, identify gaps, overlaps, or lateral drift between pieces, and decide between minimal BuildData offset edits or fixing the mesh in Blender for incorrectly aligned crafted weapons.
---

# Bannerlord Crafting Offsets

Use `D:\Bannerlord\Tools\BannerlordCraftingTool\BannerlordCraftingTool.Cli` to inspect calculated crafted-item layouts from a module's declared XML data.

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

`--fbx-folder` scans `.fbx` recursively and matches pieces to meshes by name (case-insensitive, `.lod0` tolerated). The CLI is `net8.0-windows`, so this needs the Windows desktop runtime. Prefer `render`'s `seams[].axis_gap` over `audit`'s `visual_gap` — `audit` applies an inconsistent ×0.01 factor to mesh coordinates.

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

## Audit a Weapon Type

Use the FBX-backed audit for visual seams rather than inferring them from XML pivots alone:

```powershell
dotnet run --project D:\Bannerlord\Tools\BannerlordCraftingTool\BannerlordCraftingTool.Cli\BannerlordCraftingTool.Cli.csproj --configuration Release --no-build -- audit --submodule <mod>\SubModule.xml --fbx-folder <mod>\AssetSources\weapons --all-templates --out <audit.json>
```

Use repeatable `--template <CraftingTemplate id>` instead of `--all-templates` to limit a sweep. Use `--modules-root <Modules directory>` for nonstandard layouts and `--tolerance <raw XML units>` to define the permitted visual seam (default: `0.01`).

The audit uses the visualiser's FBX node/mesh matching, transforms, scale, and engine-equivalent pivots. It runs a fixed-reference adjacent-piece sweep: each usable candidate is tested against the first usable piece of its neighbouring category. Review records with `visual_gap`; positive is a separation and negative is an overlap. `missing_meshes` and `skipped` are incomplete evidence, not alignment results. Correct only the cited `BuildData` connector, then rerun the same audit. Do not invent a manual JSON manifest or raw-file workaround.

## Offset Guidance

- Use `previous_piece_offset` or `next_piece_offset` to tune a specific adjacent join.
- A non-handle `piece_offset` moves that piece and all later pieces on its side of the assembly.
- A handle `piece_offset` changes the weapon’s grip-relative placement; do not use it to hide a bad blade/guard/handle joint.
- Preserve all unrelated attributes and report the exact XML snippets changed.
