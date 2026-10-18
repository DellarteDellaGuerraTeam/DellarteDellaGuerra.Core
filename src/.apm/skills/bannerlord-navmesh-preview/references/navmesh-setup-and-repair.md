# Navmesh setup and repair with the offline preview

This guide covers the safe workflow for diagnosing and repairing the DADG campaign-map navmesh. The preview is a read-only diagnostic. A good PNG, or even a candidate that passes offline checks, is **not** proof that Bannerlord can load or traverse the navmesh.

## Prerequisites and paths

Run commands from this repository's `src` directory. The active scene is:

```text
../../DellarteDellaGuerraMap/SceneObj/Main_map
```

It must contain `scene.xscene`, `terrain.bin`, and `navmesh.bin`. The maintained parser checkout defaults to:

```text
C:/Users/Joe/Desktop/bannerlord-navmesh-studio
```

The canonical preview script is `.apm/skills/bannerlord-navmesh-preview/scripts/render_navmesh_preview.py`; its instructions are in the adjacent `SKILL.md`. It loads `blnav.nmg9`, `blnav.rnm1`, `blnav.terrain`, and `blnav.xscene` from the Studio checkout. Preview dependencies include NumPy, Matplotlib, Pillow, and lz4. Experimental repairs additionally need SciPy, Shapely, ContourPy, and Triangle. Use the project's working Python environment; do not substitute an old global renderer or parser. The commands below use the workspace's RTK wrapper.

Confirm the interfaces before a repair session:

```powershell
rtk python .apm/skills/bannerlord-navmesh-preview/scripts/render_navmesh_preview.py --help
rtk python tools/navmesh_repair/check_candidate.py --help
rtk python tools/navmesh_repair/inventory_islands.py --help
rtk python tools/navmesh_repair/island_rebuild.py --help
rtk python tools/navmesh_repair/highland_rebuild.py --help
```

## Establish a safe baseline

1. Check the **map repository**, not just this source repository: `rtk git -C ../../DellarteDellaGuerraMap status --short --branch`. Identify existing changes before touching scene data.
2. Confirm the requested map branch is `1.4/complete-navmesh`. A branch name does not prove the active file is known-good; establish the actual baseline from its contents and checks. Do not switch a dirty checkout or fetch/create worktrees without authorization.
3. Copy `navmesh.bin` to a clearly named backup outside the active scene, for example `tools/navmesh_repair/output/navmesh.before-regional-repair.bin`. Record its hash. Never use `RuntimeDataCache` as a backup and leave that directory untouched.
4. Send every generated mesh to `tools/navmesh_repair/output` or another scratch directory. Do not point a generator at the live scene.

The native format has identity and topology constraints beyond visible geometry. In the current tooling, face records below ID 7599 are protected, and terrain type 60 is treated as unknown/protected. Never compact IDs, delete old slots, or rewrite those records merely to make the mesh look tidy.

## Render the baseline and candidate

The preview bounds order is unusual and must be written exactly as `X0 X1 Y0 Y1`:

```powershell
rtk python .apm/skills/bannerlord-navmesh-preview/scripts/render_navmesh_preview.py `
  "../../DellarteDellaGuerraMap/SceneObj/Main_map" `
  --bounds 320 730 600 1025 `
  --output "tools/navmesh_repair/output/baseline.png"

rtk python .apm/skills/bannerlord-navmesh-preview/scripts/render_navmesh_preview.py `
  "../../DellarteDellaGuerraMap/SceneObj/Main_map" `
  --navmesh "tools/navmesh_repair/output/candidate.bin" `
  --bounds 320 730 600 1025 `
  --output "tools/navmesh_repair/output/candidate.png"
```

Use `--studio "C:/path/to/bannerlord-navmesh-studio"` if the maintained checkout is elsewhere. `--samples` and `--dpi` may increase detail.

`check_candidate.py --bounds` uses the **same** `X0 X1 Y0 Y1` order; for example `--bounds 340 435 925 1025`. In contrast, the Python `rebuild(..., bounds=...)` APIs and Shapely polygons use `x0,y0,x1,y1`. Swapping these conventions can inspect or rebuild the wrong rectangle; inspect each interface rather than copying a tuple between them.

The plot uses Bannerlord world X/Y with Y upward (`worldYup`). It draws actual face edges, red vertices, `g3=7` Mountain faces in purple, and `g3=8` Lake faces in blue over decoded terrain. It is not an editor screenshot and does not prove connectivity, shared-edge correctness, traversal, face abilities, serialization safety, or runtime loading.

## Design a regional repair

Work on one bounded region at a time and compare before/after close-ups as well as a wider seam view.

### Islands and shorelines

1. Derive physical land from decoded terrain and water level, then enumerate all islands in the crop independently of settlement masks. `inventory_islands.py` exists for this coverage audit.
2. Ensure every physical island above the chosen minimum area is represented. Settlement seeds alone miss uninhabited islands.
3. Simplify each shoreline adaptively: preserve settlement anchors, narrow necks, coves that matter to connectivity, and the exact exterior seam. Reject simplification whose displacement or lost/gained area exceeds the repair tolerance.
4. Triangulate and weld coincident vertices and edges using the values that will actually be serialized. Quantize to float32 before the final weld/topology test; double-precision equality can hide microscopic overlaps or T-junctions introduced by encoding.
5. Preserve elevated authored lakes from existing `g3=8` footprints. Height at or below the scene water level is useful for sea/shore detection, but must not erase or recreate every lake as ocean.

### Mountains and passes

Classify peaks from terrain evidence rather than painting broad rectangles. Preserve meaningful ridges as `g3=7` Mountain while carving explicit connected, non-`g3=7` corridors through them for navigable passes. A colored line, a geometric gap that does not share edges, or blocking the entire mountain range is not a pass. Current offline checkers treat Mountain 7 as blocked; verify those terrain semantics again if applying this workflow to another Bannerlord version.

### Seams and connectivity

Keep every face and vertex outside the selected patch byte-for-byte or coordinate-for-coordinate as required by the checker. At the patch boundary, require shared serialized vertices and edges: no gaps, overlaps, duplicate near-coincident vertices, hanging vertices, T-junctions, or triangles that cross water.

Check every settlement remains on an intended traversable component. Preserve connectivity between settlements on the same physical island. If a baseline connection crosses sea, remove it only after proving the settlements are on different physical land components; do not use that exception to excuse a broken same-island repair.

## Experimental utilities

The scripts in `tools/navmesh_repair` are experimental regional utilities, not a guaranteed-success production generator or a tool for creating an arbitrary scene's first navmesh. They require an existing native mesh. They also import earlier audit utilities and cached evidence under `.superpowers/sdd/scottish-navmesh`; these are data/code dependencies, not a requirement to use that workflow. Check cached terrain evidence against the current scene before reuse. Inspect defaults, inputs, and JSON output before each run. Safe checks include:

```powershell
rtk python tools/navmesh_repair/terrain_regions.py --self-test
rtk python tools/navmesh_repair/island_mountains.py
rtk python tools/navmesh_repair/inventory_islands.py `
  --navmesh "tools/navmesh_repair/output/candidate.bin" `
  --output "tools/navmesh_repair/output/island-inventory-candidate.json"
rtk python tools/navmesh_repair/check_candidate.py `
  "tools/navmesh_repair/output/candidate.bin" `
  --baseline "tools/navmesh_repair/output/navmesh.before-regional-repair.bin" `
  --bounds 340 435 925 1025
```

Optional rebuild commands write scratch candidates only:

```powershell
rtk python tools/navmesh_repair/island_rebuild.py `
  --input "tools/navmesh_repair/output/navmesh.before-regional-repair.bin" `
  --output "tools/navmesh_repair/output/all-islands-candidate.bin" `
  --all-islands --with-mountains

rtk python tools/navmesh_repair/highland_rebuild.py `
  --input "tools/navmesh_repair/output/navmesh.before-regional-repair.bin" `
  --output "tools/navmesh_repair/output/highland-candidate.bin"
```

Never chain regional outputs blindly or promote a generator result merely because its own JSON says `ready`. Treat every generated file as an unverified scratch candidate until the current checker output, coverage inventory, previews, and editor/runtime validation establish its status. Do not rely on historical pass/fail wording in this guide.

## Acceptance and promotion

A candidate may be considered for explicit promotion only after all of these are reviewed against the saved baseline:

- it round-trips through the native parser/serializer;
- protected faces/IDs, unknown terrain types, and untouched exterior geometry are preserved;
- structural checks find no invalid indices, missing shared edges, duplicate near vertices, T-junctions, degenerate faces, or overlaps, including after float32 serialization;
- sampled land faces match terrain height within the chosen tolerance and water/lake faces retain the intended level and classification;
- shoreline/island coverage and displacement reports are acceptable;
- Mountain regions retain peaks and every intended pass is an actually connected traversable corridor;
- settlements are covered and same-physical-island connectivity is retained; any removed cross-sea bridge is individually verified as false;
- baseline and candidate previews pass both close-up and exterior-seam visual review;
- a disposable copy loads in the Bannerlord editor, then passes an in-game traversal/runtime test.

Promotion must be an explicit copy into the active scene with the backup retained. Close the editor/game first; after replacement, force an editor scene reload because cached scene state can conceal which file is loaded. If parsing, editor load, topology, or traversal fails, restore the saved baseline immediately and reload again. Do not modify or regenerate `RuntimeDataCache` as part of this workflow.
