import sys,struct,json,xml.etree.ElementTree as ET
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'submodules/bannerlord-agent/tools/BannerlordSceneStudio'))
from blnav.nmg9 import NavMesh
MAP=ROOT.parent.parent/'DellarteDellaGuerraMap'
out={}
for ship in ('english_boat_b','kogge','carrack'):
    p=MAP/'NavMeshPrefabs'/f'dadg_ws_{ship}_navmesh.bin'
    b=p.read_bytes();assert struct.unpack('<I',b[:4])[0]==len(b)-4
    m=NavMesh.parse(b[4:]);assert m.serialize()==b[4:]
    # Dump all source geometry to review without binary reasoning.
    entries=[]
    for e in ET.parse(MAP/'NestedPrefabs'/f'dadg_ws_{ship}_ship_nested'/'attachment_machine_holder.xml').findall('./entity/children/entity'):
        entries.append({'name':e.get('name'),'transform':ET.tostring(e.find('transform'),encoding='unicode'),'scripts':[(s.get('name'),[(v.get('name'),v.get('value')) for v in s.findall('./variables/variable')]) for s in e.findall('.//script')],'child_transforms':[(c.get('name'),ET.tostring(c.find('transform'),encoding='unicode') if c.find('transform') is not None else '') for c in e.findall('./children/entity')]})
    out[ship]={'stats':m.stats(),'vertices':m.vertices,'faces':[{'v':f.vertex_ids,'id':f.g3} for f in m.faces],'attachments':entries}
    print(ship,m.stats(),'attachments',json.dumps(entries))
(Path(__file__).parent/'nav-before.json').write_text(json.dumps(out,indent=2))
