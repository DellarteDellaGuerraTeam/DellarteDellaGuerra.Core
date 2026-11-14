"""Write the six-mesh native import handoff without changing compiled resources."""
import json
from pathlib import Path

OUT=Path(__file__).resolve().parent
ROOT=OUT.parents[1]
MAP=ROOT.parent.parent/'DellarteDellaGuerraMap'
PACKAGE=MAP/'Tools/WarSailsShips/visual-fixes-20261114'
plan=json.loads((OUT/'import-plan.json').read_text())
baseline=json.loads((OUT/'compiled-before.json').read_text())
lines=['# Rudder and separated-body import handoff','',
'The result is six new mesh resources: one body without its static rudder and one separate rudder for each ship. Original body resources are retained. Ship references are switched only after the six new meshes are verified.','',
'## Why the earlier body packages were empty','',
'The earlier separated-body FBXs reused existing original body mesh names. Native imports into the new rudder folder produced empty body packages while original body packages stayed unchanged. Replacement FBXs now use unique `body_no_rudder_v1` mesh names with the same geometry, UVs and material assignments.','',
'## Import','',
'Source folder: `AssetSources/dadg_warsails/rudder/` in this Map module; exactly six FBXs.','',
'Import the three new body FBXs as new geometry resources. Keep already imported matching rudder resources: their source files have not changed. Preserve the original body resources.','',
'Settings: Convert to unit **m**; **Import meshes ON**; Convert to Z-up OFF (already Z-up); skeletons, skeletal animations, morph animations and physics shapes OFF. Keep authored scale, pivot and material names.','',
'| Ship | New body mesh / FBX | Rudder mesh / FBX |','|---|---|---|']
for row in plan:
    lines.append(f'| {row["ship"]} | `{row["body_metamesh"]}` / `{Path(row["body_fbx"]).name}` | `{row["rudder_metamesh"]}` / `{Path(row["rudder_fbx"]).name}` |')
lines += ['',
'Save the packages. Expect six mesh resources. FBX/package box icons are containers and do not count as mesh previews. Old empty separated-body packages are not replacement bodies.','',
'## Activation after verification','',
'The assistant will verify the six mesh names, geometry/materials and preservation of original resources, then install the staged `mesh_holder.xml` and `rudders_holder.xml` together. Copying source files does not activate new references.','',
'Body offsets remain English +.15m, kogge -.8m and carrack -1.4m. Physics stock markers and tiller/whipstaff controllers remain unchanged. English uses native `rudder_rotation_entity`; kogge/carrack use the registered `dadg_rudder_visual` adapter.','',
'After activation, restart/rescan the editor and verify one blade per ship, neutral alignment and both steering directions. SDF water clipping is separate.','',
'The original body-plus-rudder geometry, UVs and materials passed independent FBX roundtrip. New uniquely named bodies are compared with those verified sources in `authoring/unique-body-import-audit.json`. Native import and rendering of the new bodies remain to be checked.','']
(PACKAGE/'RUDDER_IMPORT.md').write_text('\n'.join(lines),encoding='utf-8')
(PACKAGE/'rudder-import-plan.json').write_text(json.dumps({'sources':plan,'compiled_before':baseline},indent=2))
print(PACKAGE/'RUDDER_IMPORT.md')
