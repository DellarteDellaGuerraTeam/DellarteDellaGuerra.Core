---
name: bannerlord-scotland-settlement-placement
description: Project Scottish settlement coordinates from the DADG Crown or Isles KMZ/KML datasources into calibrated DADG campaign-map coordinates. Use when adding, replacing, moving, auditing, or regenerating Scottish settlements; do not use political or clan folders to infer mainland-versus-island geography.
---

# Scotland Settlement Placement

Use the bundled calibration and projection script instead of recreating coordinate math or copying the old England-only transform.

## Place settlements

Run from this skill directory with the project Python:

```powershell
C:\Users\Joe\.pyenv\pyenv-win\versions\3.12.1\python.exe scripts\place_settlements.py --name Inchnadamph --name "Threave Castle"
```

Use `--all` to project every point in the bundled Crown and Isles datasources. Use repeated `--source` arguments for a replacement KMZ, KML, or JSON datasource. Use `--output <path>` to retain the JSON manifest. The script is read-only and never edits the scene or settlement XML.

Treat its `map_x` and `map_y` as the calibrated geographic position. Political folders describe ownership, not physical geography. In particular, an Isles or clan record may legitimately project onto the Scottish mainland.

## Validate before persistence

Keep these stages distinct and record each coordinate:

1. datasource longitude/latitude;
2. calibrated DADG `map_x`/`map_y` from the script;
3. final terrain/navmesh-valid coordinate, only if a small adjustment is necessary.

Validate against the live `DellarteDellaGuerraMap\SceneObj\Main_map` terrain and navmesh. Land requires terrain height `> 10.0` and no covering water face (`g3` in `8,10,11,18,19,22,24`). Town and castle footprints need more clearance than villages. If adjustment is required, keep it on the historically correct connected land body and report the vector from the calibrated coordinate. Never snap to the nearest arbitrary island, terrain pixel, or clan-classified body.

Do not modify `terrain.bin`, create islands, persist Navmesh Studio moves, or write settlement XML without the user's explicit approval. Preserve England navmesh faces `[0,7599)` if a later approved navmesh edit is needed.

## Resources

- Read [references/calibration.md](references/calibration.md) when changing controls, evaluating residuals, replacing the transform, or investigating a questionable result.
- `assets/scotland-calibration.json` is the machine-readable source of truth for the projection.
- `assets/crown-of-scotland-locations.kmz` and `assets/lordship-of-the-isles-locations.kmz` are the copied authoritative datasources.
- `assets/scotland-calibration-reference.png` preserves the reviewed north-up vector evidence; it is supporting evidence, not executable calibration data.
