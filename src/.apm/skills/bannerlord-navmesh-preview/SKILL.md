---
name: bannerlord-navmesh-preview
description: Render an offline top-down preview of a Bannerlord scene navmesh over decoded terrain, including face edges, vertices, and Mountain/Lake classifications. Use when inspecting navmesh coverage or comparing a candidate navmesh.bin; this is not runtime or editor validation.
---

# Bannerlord Navmesh Preview

Use `scripts/render_navmesh_preview.py` to produce a read-only PNG from a scene directory containing `scene.xscene`, `terrain.bin`, and `navmesh.bin`.

```powershell
python scripts/render_navmesh_preview.py "<scene-directory>" --bounds X0 X1 Y0 Y1 --output "<preview.png>"
```

Pass `--navmesh <candidate.navmesh.bin>` to preview a candidate while retaining the scene terrain, or `--studio <bannerlord-navmesh-studio>` when the maintained Studio checkout is not at its default path. Omitting the scene directory targets the DADG `Main_map` scene.

The plot preserves Bannerlord world X/Y orientation (Y increases upward), decodes terrain elevation from `terrain.bin` using the scene parameters, reads the scene water level when present, draws actual polygon edges and red vertices, and fills `g3=7` Mountain faces purple and `g3=8` Lake faces blue.

Treat the image as an offline diagnostic plot, not a Bannerlord editor screenshot. It does not validate navmesh connectivity, topology, agent traversal, face abilities, serialization safety, or runtime behavior. Use the editor/game and appropriate structural checks for those claims.

For DADG navmesh setup or repair using these previews, read [the setup and repair guide](references/navmesh-setup-and-repair.md). It covers prerequisites, island coverage, mountain passes, candidate validation, and rollback; it is not needed for a simple preview render.
