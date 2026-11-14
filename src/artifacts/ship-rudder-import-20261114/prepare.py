"""Copy the six verified rudder/body sources into the native import source tree."""
from pathlib import Path
import hashlib,json,shutil,xml.etree.ElementTree as ET

OUT=Path(__file__).resolve().parent
ROOT=OUT.parents[1];MAP=ROOT.parent.parent/'DellarteDellaGuerraMap'
PACKAGE=MAP/'Tools/WarSailsShips/visual-fixes-20261114'
DEST=MAP/'AssetSources/dadg_warsails/rudder'
auth=json.loads((PACKAGE/'authoring/source-authoring.json').read_text())
hashes={r['path']:r['sha256'] for r in json.loads((PACKAGE/'files.json').read_text())}
rows=[]
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
for ship,a in auth.items():
    row={'ship':ship,'body_metamesh':a['body_metamesh'],'rudder_metamesh':a['rudder_metamesh'],'pivot_root':a['rudder_pivot_root'],'rotation_x':a['rudder_initial_rotation_x'],'live_body_z_offset_unchanged':a['body_z_offset']}
    if 'original_body_metamesh' in a:
        row['original_body_metamesh']=a['original_body_metamesh']
        body_live=MAP/f'NestedPrefabs/dadg_ws_{ship}_ship_nested/mesh_holder.xml'
        body_candidate=PACKAGE/f'authoring/prefab-stage/{ship}/mesh_holder.xml'
        body_tree=ET.parse(body_candidate)
        assert body_tree.find(f'.//meta_mesh_component[@name="{a["body_metamesh"]}"]') is not None
        row['body_prefab_candidate']=str(body_candidate)
        row['live_body_prefab_before_sha256']=sha(body_live)
        row['body_prefab_candidate_sha256']=sha(body_candidate)
    for role in ('body','rudder'):
        name=Path(a[f'{role}_fbx']).name
        source=PACKAGE/'authoring'/name;dest=DEST/name
        assert sha(source)==hashes[name]
        if dest.exists():assert sha(dest)==hashes[name],f'Different existing source: {dest}'
        else:DEST.mkdir(parents=True,exist_ok=True);shutil.copy2(source,dest)
        assert sha(dest)==hashes[name]
        row[f'{role}_fbx']=str(dest);row[f'{role}_sha256']=hashes[name]
    live=MAP/f'NestedPrefabs/dadg_ws_{ship}_ship_nested/rudders_holder.xml'
    candidate=PACKAGE/f'authoring/prefab-stage/{ship}/rudders_holder.xml'
    old=ET.parse(live);new=ET.parse(candidate)
    def stock(tree):return ET.canonicalize(ET.tostring(tree.find('.//entity[@name="rudder_stock"]'),encoding='unicode'),strip_text=True)
    assert stock(old)==stock(new)
    assert new.find(f'.//meta_mesh_component[@name="{a["rudder_metamesh"]}"]') is not None
    tags=[t.get('name') for t in new.findall('.//tag')]
    expected='rudder_rotation_entity' if ship=='english_boat_b' else 'dadg_rudder_visual'
    assert tags.count(expected)==1
    row['prefab_candidate']=str(candidate);row['live_prefab_before_sha256']=sha(live);row['prefab_candidate_sha256']=sha(candidate)
    rows.append(row)
(OUT/'import-plan.json').write_text(json.dumps(rows,indent=2))
print('Prepared',len(rows)*2,'verified FBX sources in',DEST)
print('Import the three separate bodies and three rudders as unique new meshes. No prefab cutover was performed.')
