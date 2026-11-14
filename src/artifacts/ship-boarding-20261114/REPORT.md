# DADG boarding and ship navmesh changes — 2026-11-14

Deployed and verified: the three ship layouts and their matching native NMG9 navigation meshes are changed together. `deployment.json` records the verified live file hashes; `backup/` retains each original file. The post-deployment `verify_live.py` check passed for all three native meshes, attachment pairs and the unchanged campaign-map identity. This work uses offline Blender CLI geometry checks and Bannerlord Scene Studio's native parser/serializer.

| Ship | Boarding stations per side, before → after | Final positions (ship-root metres) | Navmesh faces, before → after |
|---|---|---|---|
| English boat B | 1 → 2 | X±.375; aft Y-1.95, fore Y2.45 | 13 → 14 |
| Kogge | 1 → 3 | X±2.5; Y-3.75,-2.25,2.75 | 228 → 260 |
| Carrack | 1 → 2 | Waist X±2.3,Y5.3; forecastle X±1.1,Y10.3 | 353 → 367 |

## What changed

- The English boat's existing aft attachment origins were just outside their tagged navmesh faces. Moving them 7.5cm aft puts them inside. Two old boundary vertices crossed a bench collider; moving them to X±.5,Y-2.1 keeps those faces on the surveyed low deck. A split of the existing foredeck adds one narrow station per side. The boat remains single file; additional stations are not a promise of independent two-way traffic.
- The kogge gets three spaced boarding lanes per side, with separate boundary faces and preserved deck/stair routes. Both rope-climber landing markers were .8m outside the original navmesh. Their targets move .3m aft to a clear strip, retaining the rope roots/chains, and receive ordinary walkable landing faces.
- The carrack's old boarding positions overlapped the rope-climber landings. Boarding moves to the clear waist and forecastle. The old tagged faces become ordinary walkable faces, preserving rope-climber access. Existing stairs and their navigation links remain. The original proposed upper-aft and rear-waist positions were rejected after checking the actual instanced stair meshes and gun crew space; the final layout uses two cleared stations per side.
- Native bridge endpoint visuals and their matching `bo_boarding_bridge_ramp_a` colliders move together where rail fit requires it. Only the explicit target/source/connection child nodes and step capsule receive the height correction; pilot/station positions and the inherited visual-parent transform remain intact. The corrections are boat aft +.25m/fore +.35m, kogge middle +.05m/fore +.10m, carrack waist +.15m. These corrections use measured ramp surfaces and neutral native catenary spans, rather than connection-marker height alone. See the geometry follow-up evidence for the final physical fit checks and limits.

## Validation

`build_boarding.py` asserts all of the following before writing staging files:

- Size-prefixed NMG9 files parse and round-trip through Studio, including the original payloads.
- Every nonzero boarding ID has exactly one triangle/quad, at least one unshared edge, and its matching attachment/receiver origin strictly inside. IDs are unique and within 1..50.
- All face loops are valid, CCW triangles/quads; no repeated corners, non-manifold edges, unused vertices or 3D T junctions remain. Each ship is one component connected by shared whole edges.
- Native face fields and scene-level masks remain `(g1,g2,g4,g5,g6)=(2,0,0,-1,0)`.
- New instances have unique GUIDs and preserve the original native script/component hierarchy.

`check_landings.py` checks 114 existing spawn, helm, gun and rope-climber origins. Every origin has after-edit navmesh coverage; no previously supported origin loses its vertical fit. Actual positions and per-origin results are in `landing-coverage.json`.

The Blender specialist measured actual source-body and detailed collision geometry, native instantiated stairs and the native ramp physics, using root transforms and source hashes. The English approach is .6m single file; the carrack forecastle has a restricted .8m inboard approach, avoiding centre rigging. `geometry/REPORT.md` contains the corrected final geometry report and links to raw ray/grid evidence. `geometry/REPORT_INITIAL_SUPERSEDED.md` preserves the initial candidate list, including the rejected positions under stairs. `final-stations.json` pins the exact deployed coordinates independently of the survey manifests.

The final ramp-surface checks include the installed native equal-height catenary at 2/4/6m spans and connection-frame roll across the boarding width. Minimum sampled physical rail clearance is .098m for the boat, .059m for the kogge and .074m for the carrack; each ramp retains deck intersection, with sampled transition error under 1.8cm. Profiles use finite samples (rail raster spacing 2.5cm), so these are static geometric checks. Native stack geometry overlaps the boat crate bases in both bridge visual states; floor-gap measurements alone did not justify changing intentionally layered props. The kogge landing extension has physical clearance, while thin existing visual rigging can still intersect an agent silhouette.

`navmesh-before-after.png` is a top-down offline diagnostic projection, not an editor/game screenshot. `verification.json` records native mesh counts and topology results. `manifest.json` and `deployment.json` identify the exact staged/live bytes.

## Scope and remaining verification

Seven live files are involved: three attachment-holder XML files, three ship navmesh binaries and the kogge climber-holder XML. No source hull, detailed collision resource, TPAC, runtime cache, C# code, gun, helm, buoyancy or upgrade configuration is changed. The campaign-map navmesh is hash-checked unchanged during deployment.

No native editor or game battle was run for this change. Offline checks do not establish AI queue throughput, crew animation clearance, moving-ship bridge sag/pitch or the corrected rope-climbing transition. Those remain the runtime acceptance checks. There is no C# build/test requirement for these XML/navmesh changes.

## Reproduction and rollback

Run the builder and coverage checks with the navmesh-studio skill's pinned requirements plus `shapely==2.1.2`; the preview uses `matplotlib==3.10.8`. The scripts import the resolved Studio checkout, never patch its code or hand-edit native navmesh bytes. Build from the original live files (before deployment); backup identity checks deliberately reject a different source revision.

`deploy_boarding.py` checks every original, backup and staged hash before writing, re-reads each live file, and restores the originals if deployment fails. To roll back later, restore only the seven paths listed in `manifest.json` from `backup/`, provided their live hashes still match this deployment. Do not overwrite subsequent user changes.
