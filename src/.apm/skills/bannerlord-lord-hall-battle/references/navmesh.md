# Hall navigation: preserve floors, layers and raw attributes

## Read-only audit

Locate available tooling rather than requiring a machine-specific installation. In the DADG environment the maintained tool is `C:/Users/Joe/Desktop/bannerlord-navmesh-studio`. Inspect its current parser before use; a tool failure is not proof of a corrupt scene.

The verified Studio API is:

```python
from pathlib import Path
from blnav import rnm1, nmg9

payload = rnm1.read(str(Path(scene_dir) / "navmesh.bin"))
mesh = nmg9.NavMesh.parse(payload)
assert mesh.serialize() == payload  # in-memory check; does not save the scene
print(mesh.version, mesh.stats())
```

Set `scene_dir` to the resolved target and import from the actual Studio checkout. Use Python's `-B` for a read-only audit. `rnm1.read` unwraps RNM1's LZ4 block; the maintained `nmg9.NavMesh.parse` recognizes decompressed **scene** NMG8 and NMG9. NMG8 uses 20-byte edge records and NMG9 uses 24-byte records. The separate `blnav/nmg8.py` handles a size-prefixed triangle **prefab** format, not these scene files.

The legacy `bannerlord-navmesh` root's `navmesh_scene_bin.py`, `navmesh_bin.py` and early compressed-byte topology probes are superseded exploratory decoders. Do not use their offsets, compression-byte heuristics or health checks to declare scene corruption. If only an old parser exists, use a compatible maintained reader or the official editor; never force an unknown payload through the wrong format.

Inventory vertices/edges/faces, elevation ranges, shared-edge components, and distributions of **all** raw face attributes (`g1` through `g6`). Validate index ranges, face edges, floor coverage and intended routes. Adjacent-looking polygons may not share vertex/edge IDs. Conversely, disconnected components can intentionally represent different floors or visibility layouts.

## Avoid destructive assumptions

- Keep the original `navmesh.bin` byte-for-byte when helper placement can use it. A scene save should not implicitly regenerate navigation.
- On intentional navmesh edits preserve the loaded format/version, raw face attributes, floor elevations and unaffected topology. Round-trip an unchanged copy first; if the parser rejects unknown edge fields or a footer, stop binary rewriting and investigate compatibility.
- Studio labels `g3` as a dynamic face ID and `g5` as an island/group. Do not treat those labels as a complete engine specification. In a shipped keep, overlapping components' `g5` values match civilian/siege visibility bits; this is strong evidence of layer information, not a license to reinterpret every file. Preserve values unless verified editor/runtime behavior establishes the intended change.
- Do not weld all components, normalize all IDs to zero, rewrite `g5`, or strip apparent duplicates to "repair" a hall. Such operations can join incompatible navigation layers or destroy a valid alternate layout.
- `FightAreaMarker.AreaIndex` is a combat stage key, **not a navmesh face ID**. Exterior siege gate/ladder IDs and village raid/animal IDs are not lord-hall requirements.
- Studio's terrain-heightmap generator represents one height per XY position; it cannot reproduce stacked interior floors, stairs or furniture collision. Never terrain-regenerate or snap-to-terrain a multi-floor hall. An outdoor courtyard mapped as `lordshall` still needs its authored navigation and collision inspected, not an automatic terrain rebuild.

## Geometry and runtime acceptance

Check each candidate point against its intended walkable face in 3D, with clearance from walls, furniture and drop edges; ensure paths from the attacker entrance reach the defender positions on the active siege layout. Treat physical props and closed barriers as obstacles even if a polygon lies underneath. Do not reduce this to XY point-in-polygon membership or a minimum bounding-box check.

The official [navmesh guide](https://moddocs.bannerlord.com/editor/scene-editor/nav_mesh/) lists lord halls among basic ID-0 navigation scenes, requires spawn pivots to lie within navigation faces, and cautions against navigation through solid props or jump-only routes. These guidelines do not imply every existing nonzero face ID is wrong.

A graph/point audit proves only the geometry assumptions it checks. It does not prove active engine face selection, prefab collision, agent clearance or pathfinding. Validate the actual civilian and siege configurations in the editor/runtime when available, including stairs, alternate floors, generated attacker footprints, reinforcement entry, and any changed barricades. Report unverified engine behavior explicitly.
