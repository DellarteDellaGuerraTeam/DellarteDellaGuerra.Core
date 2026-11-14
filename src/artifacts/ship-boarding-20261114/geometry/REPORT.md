# Final boarding geometry review — 2026-11-14

This report supersedes REPORT_INITIAL_SUPERSEDED.md and its rejected body-only carrack recommendations. Read-only Blender 5.1 background CLI checks only; parent owns and deploys the staged XML/navmesh. No model, live prefab, collision or navmesh was edited by this survey.

## Final stations and actual walking-surface fit

Measurements use exact staged attachment XML, recursively resolved current Native/NavalDLC prefab GUID inheritance. Physical geometry comes from current detailed custom-hull FBXs and TpacTool.Lib PhysicsDescriptionData for bo_boarding_bridge_ramp_a and bo_cog_ship_stairs. No ad-hoc binary parsing, TPAC writing or GUI was used.

|Ship|X|Y|Collision floor Z|Minimum surface/rail clearance|Nearest ramp/deck transition vertical error|
|---|---:|---:|---:|---:|---:|
|english_boat_b|-0.375|-1.95|0.20781|0.1072m|0.0125m|
|english_boat_b|0.375|-1.95|0.20575|0.1079m|0.0000m|
|english_boat_b|-0.375|2.45|0.19953|0.0978m|0.0020m|
|english_boat_b|0.375|2.45|0.19813|0.0993m|0.0094m|
|kogge|-2.5|-3.75|1.73249|0.1357m|0.0003m|
|kogge|2.5|-3.75|1.73332|0.1097m|0.0002m|
|kogge|-2.5|-2.25|1.69838|0.1279m|0.0122m|
|kogge|2.5|-2.25|1.69794|0.0694m|0.0128m|
|kogge|-2.5|2.75|1.78805|0.0921m|0.0171m|
|kogge|2.5|2.75|1.78911|0.0592m|0.0167m|
|carrack|-2.3|5.3|2.23134|0.1350m|0.0099m|
|carrack|2.3|5.3|2.23134|0.0741m|0.0099m|
|carrack|-1.1|10.3|4.49333|0.5822m|0.0071m|
|carrack|1.1|10.3|4.50877|0.5960m|0.0011m|

Clearance compares the active native ramp surface at each sampled collision-rail XY and, beyond the connection plane, the current engine equal-height catenary plank surface with 1.035 length ratio for 2m/4m/6m spans and source-frame cross-width roll. The check covers a conservative 1.6m along-rail strip at 10cm spacing and vertical collision scans at2.5cm spacing. It is not merely the height of a marker or bridge_end visual. The engine generates plank physics from connection_point (ShipAttachmentMachine.cs4104–4107,4210–4217,4344); bridge_end is visual geometry. Native catenary math is RopeSegment.cs1155–1229. Actual finite planks use sampled curve chords; these calculations use the continuous neutral catenary.

All final minimum margins exceed the2.5cm rail-height sampling uncertainty. They are neutral static evidence, not guarantees under ship pitch, unequal deck elevations, different headings or long spans. No collision cuts/new ramp assets are indicated by this static fit. Actual boarding runtime remains required.

Every native ramp has sampled points both below and above real hull deck, so its lower end still intersects the deck after lifting; this is an overlap, not a floating first step. Nearest crossing sample has≤1.8cm vertical difference at2.5cm horizontal spacing. The native has_steps and agent_only collider flags are preserved. This does not certify the engine step/slope limit at runtime.

## Final clearance and rejected positions

- English:2 stations per side. Aft x±.375,y-1.95 and fore x±.375,y2.45 retain restricted .6×.6m single-file landing approaches. Support and1.8m body/collision headroom pass. Existing aft station moved7.5cm fromy-1.875 for tagged-face containment. Old bench-intersecting nav corners moved tox±.5,y-2.1 (Lfloor.20780/R.20634; clear headroom). Fore does not support a1m-wide approach: narrowing hull and benches constrain it. Final total bridge visual endpoint +native capsule lift from original: aft+.25m, fore+.35m, preserving station/pilot origins.
- Kogge:3 stations per side atx±2.5,y-3.75/-2.25/2.75, each with1×1m supported approaches and clear sampled body/physics rays. Final endpoint/capsule lifts aft0,middle+.05,fore+.10m. The main deck here is roughly5.9–6.2m across. Kogge climbing target moves fromy0 toy-.3; x±3.0 retained, with regular nav landing lips tox±3.05, y-.6..0. Outer lip collision support atx±3.05,y-.3 isz1.66526, and physical approach rays are clear. Thin port rig surfaces intersect some1.8m visual-only headroom rays near(-2.6,-.2,2.74): possible visual rope clipping remains; the detailed physical collider is clear. Do not claim visual rig clearance or validated climb animation.
- Carrack:2 stations per side: waist x±2.3,y5.3,z2.23134 with1×1m approach; forecastle x±1.1,y10.3,z4.49333L/4.50877R with .8m inboard×1m longitudinal approach. Both pass hull-body, detailed collision and all five actual instanced native staircase physics/headroom checks to1.8m. Final waist endpoints/capsule lift+.15m; fore0. Forecastle restricted approach avoids centreline rig and stays at least1.8m ahead of the fore guns. Ordinary waist floor width~5.2m; forecastle nav width2.4m at station, so retain the restricted inboard strip.
- Rejected carrack initial x±2.3,y.5 andx±1.5,y-5 lie under the actual waist/poop stair runs; root-only stair circles missed them. y2.0/2.1 still hits stair structure on1m approaches. Upper sidedeck y-2/-1.75 clears stairs but falls inside side-gun clearance. Stern fourth station near-6.5 conflicts with aft gun footprint. Preserve existing stairs and connected nav routes.

## Staged nav spot checks

- english_boat_b: all tagged boarding vertices have detailed collision support; maximum vertical error0.11884m (<.15m target). Staged nav JSON SHA256 `af373b3e028b5f112815c92e55939d2f75172134dd905a6b55723434b9ce26d4`.
- kogge: all tagged boarding vertices have detailed collision support; maximum vertical error0.01213m (<.15m target). Staged nav JSON SHA256 `e7c97580c142bb0f75c35c81fe100e684669309ac582e39486c6981bad95fea5`.
- carrack: all tagged boarding vertices have detailed collision support; maximum vertical error0.00179m (<.15m target). Staged nav JSON SHA256 `f7ba23ee052c4a1e53aeba1f82fd68bdce3987ae36f3929267cde313a2b8783d`.

Parent separately validates unique g3 boarding face IDs, machine containment, one unshared boundary edge, connectedness, CCW winding, no3D T-junctions and landing coverage. The geometry survey consumes parent after.json files; it does not parse or mutate binary navmesh. Native rules read: submodules/bannerlord-agent/brain/war-sails-naval/ws-ship-navmesh-boarding-face-rules.md.

## Native visual stack / apparent floating crates

A crate-to-hull-only gap is not sufficient evidence of an exposed floating prop. Native crate_a/b, boarding_bridge_pile_a/b, boarding_bridge_hanged_a and bd_sack_b/c were exported and transformed using actual inherited local frames. Boat crate base footprints overlap native stack geometry in both bridge source and target states (74–80 sampled source contacts or14–20 target contacts). No isolated exposed crate-base gap was confirmed; boat-crate-proposal.json therefore proposes no transform edits. This is bounded contact evidence, not an artistic render approval, and does not prove every stack-to-hull contact. Pre-existing kogge presentation was not altered. Capsule/endpoints/visibility hierarchy stay intact.

## Evidence and reproduction

- candidates.json: corrected final coordinates, source hashes, exact staged attachment/nav hashes, final fit verdicts.
- final_probe.json: actual native ramp transforms/bounds; deck transition samples; rail/ramp/plank comparisons; all boarding lip vertex height checks; exact carrack and kogge landing probes.
- attachment_check.json: inherited source/target/end/clip/capsule/prop root frames, actual five stair instances, native visual crate bounds, current packed XML hashes.
- native-physics.json + physics-reader/: typed TpacTool.Lib extraction of native ramp/stair manifold vertices/faces; native package data was read only.
- native-*-v*/manifest.json: native FBX export package paths, GUIDs, byte counts andSHA256. First unresolved export is retained as failed evidence; resolved export successfully loaded Native dependencies.
- survey.json/probe_candidates.json: original source/collision hashes, root offsets, obstacle/station snapshots and rejected candidate exploration. These raw historical samples do not override final decisions above.
- Blender -b --python attachment_check.py; then Blender -b --python final_probe.py; Blender -b --python crate_proposal.py; Python final_report.py. Attachment checks read parent stage/NestedPrefabs and original body/collision sources; final probe reads parent *-after.json. These scripts write only geometry evidence.

No live mission, AI traffic, climb animation, enemy boarding, crew animation or vessel motion was tested by this survey. Parent deployment status is recorded separately. Source-body variants: English original live body with+.15m rootZ, kogge no-yards with-.8m, carrack no-yards with-1.4m; detailed collision is already ship-root-frame and receives no duplicate offset.
