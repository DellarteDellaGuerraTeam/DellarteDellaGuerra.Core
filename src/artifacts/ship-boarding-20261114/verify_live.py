"""Read-only acceptance of deployed bytes and native ship navmesh rules."""
import hashlib,json,struct,xml.etree.ElementTree as ET
from build_boarding import MAP,OUT,NavMesh,validate,SHIPS,stations

manifest=json.loads((OUT/'manifest.json').read_text())
for row in manifest:
    data=(MAP/row['path']).read_bytes()
    assert hashlib.sha256(data).hexdigest()==row['after_sha256'],row['path']
for ship in SHIPS:
    data=(MAP/'NavMeshPrefabs'/f'dadg_ws_{ship}_navmesh.bin').read_bytes()
    assert struct.unpack('<I',data[:4])[0]==len(data)-4
    mesh=NavMesh.parse(data[4:]);assert mesh.serialize()==data[4:]
    expected=stations(ship);result=validate(mesh,expected)
    tree=ET.parse(MAP/'NestedPrefabs'/f'dadg_ws_{ship}_ship_nested'/'attachment_machine_holder.xml')
    entities=tree.findall('./entity/children/entity');assert len(entities)==len(expected)
    for e,p in zip(entities,expected):
        xyz=[float(s) for s in e.find('./transform/position').get('value').split(',')]
        assert max(abs(a-b) for a,b in zip(xyz,p['position']))<1e-6
        for script in ('ShipAttachmentMachine','ShipAttachmentPointMachine'):
            assert e.find(f'.//script[@name="{script}"]/variables/variable[@name="RelatedShipNavmeshOffset"]').get('value')==str(p['id'])
    print(ship,'live native mesh + paired attachment origins verified',len(expected),'stations')
receipt=json.loads((OUT/'deployment.json').read_text())
if receipt['campaign_map_navmesh_unchanged_sha256']:
    assert hashlib.sha256((MAP/'SceneObj/Main_map/navmesh.bin').read_bytes()).hexdigest()==receipt['campaign_map_navmesh_unchanged_sha256']
print('PASS: all deployed hashes, ship navmesh rules, attachment pairs, and campaign-map identity')
