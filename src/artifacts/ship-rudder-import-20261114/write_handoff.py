"""Write the exact manual native-import handoff; never edits compiled resources."""
import hashlib,json
from pathlib import Path

OUT=Path(__file__).resolve().parent;ROOT=OUT.parents[1]
MAP=ROOT.parent.parent/'DellarteDellaGuerraMap'
PACKAGE=MAP/'Tools/WarSailsShips/visual-fixes-20261114'
plan=json.loads((OUT/'import-plan.json').read_text())
inventory=json.loads((OUT/'before/manifest.json').read_text())
baseline=[]
for row in plan:
    body=next(e for e in inventory['Meshes'] if e['MeshId']==row['body_metamesh'])
    blade=next(e for e in inventory['Meshes'] if e['MeshId']==row['rudder_metamesh'])
    assert body['AssetGuid'] and body['DuplicateAssetCount']==1
    assert blade['Status']=='missing','An existing blade resource needs independent review'
    package=Path(body['SourcePackage'])
    baseline.append({'ship':row['ship'],'body_metamesh':row['body_metamesh'],'body_guid':body['AssetGuid'],'body_package':str(package),'body_package_sha256':hashlib.sha256(package.read_bytes()).hexdigest(),'new_rudder_metamesh':row['rudder_metamesh'],'new_rudder_present_before':False})
(OUT/'compiled-before.json').write_text(json.dumps(baseline,indent=2))
lines=['# Rudder import handoff','',
'Status: six verified FBXs are ready. Native imports and the final prefab activation are pending. The rudder animation adapter is already registered and built; it becomes active when the tagged separate blades are installed.','',
'## Import now','',
'Source folder: `AssetSources/dadg_warsails/rudder/` in the Map module.','',
'1. For each existing body resource below, change its source binding to the listed separated-body FBX and reimport that existing geometry resource. Preserve the listed metamesh identity and its current import settings/material bindings. The new body removes the baked static rudder.','',
'2. Import each corresponding rudder FBX as a new geometry resource. Keep its exact metamesh name below, native metre units and authored pivot. Preserve the existing source material names. Do not recenter or apply an extra ship waterline offset.','',
'| Ship | Reimport existing body resource | Separated body FBX | Import new rudder resource / FBX |','|---|---|---|---|']
for row in plan:
    lines.append(f'| {row["ship"]} | `{row["body_metamesh"]}` | `{Path(row["body_fbx"]).name}` | `{row["rudder_metamesh"]}` / `{Path(row["rudder_fbx"]).name}` |')
lines += ['',
'3. Save the packages, then report that the imports are complete. Do not change the ship body entity transforms. Existing live body offsets remain English +.15m, kogge -.8m, carrack -1.4m.','',
'## Final activation after imports','',
'The assistant will check that all three blade resources exist, that the existing body resource identities are preserved and the bodies have been reimported without their baked rudders. It will then install only `authoring/prefab-stage/<ship>/rudders_holder.xml` into the matching live ship prefab folder.','',
'Those holder files preserve each original physics stock marker. English boat uses native `rudder_rotation_entity`; kogge and carrack use `dadg_rudder_visual` with the registered adapter, preserving their existing tiller/whipstaff controllers. SDF import/activation is a separate task and is not part of this rudder cutover.','',
'Do not launch a ship mission between reimporting a body and activating its blade holder: the body and separate blade form one complete visual assembly. Native editor/game inspection after activation must verify one blade per ship, neutral alignment, both turning directions and unchanged steering controls.','',
'## Verification already completed','',
'The original body geometry equals the separated body plus rudder, including polygon UV/material assignments, after independent FBX roundtrip. Neutral-pose errors are below 0.000001m. The six copied source hashes match the committed authoring manifest. Existing stock-marker XML is semantically identical to the prepared holders. The production-source rudder adapter self-check passed.','',
'Current compiled body resource GUIDs and TPAC package hashes were recorded before import, and the three new blade names are absent. A baseline FBX export encountered unresolved external material dependencies; no claim is made that compiled-body geometry was exported successfully. This does not alter the verified authoring FBXs. Native import and rendering remain unverified.','']
(PACKAGE/'RUDDER_IMPORT.md').write_text('\n'.join(lines),encoding='utf-8')
(PACKAGE/'rudder-import-plan.json').write_text(json.dumps({'sources':plan,'compiled_before':baseline},indent=2))
print(PACKAGE/'RUDDER_IMPORT.md')
