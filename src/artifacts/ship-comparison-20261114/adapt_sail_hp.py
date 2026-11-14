"""Match normal War Sails sail durability to the existing DADG hull strength."""
from pathlib import Path
import re
import xml.etree.ElementTree as ET

ROOT=Path(__file__).resolve().parents[2]
MODULES=ROOT.parent.parent
target=MODULES/'DellarteDellaGuerraMap/ModuleData/dadg_custom_ship_hulls.xml'
native=ET.parse(MODULES/'NavalDLC/ModuleData/ship_hulls.xml')
references={'dadg_ws_english_boat_b':'fishing_ship','dadg_ws_kogge':'western_trade_ship','dadg_ws_carrack':'vlandia_heavy_ship'}
old=target.read_bytes();before=ET.fromstring(old);updated=old
for hull in before:
    if hull.tag!='ShipHull':continue
    identity=hull.get('id');ref=native.find(f'./ShipHull[@id="{references[identity]}"]')
    assert int(ref.get('max_sail_hitpoints'))*2==int(ref.get('max_hitpoints'))
    hp=int(hull.get('max_hitpoints'))//2
    assert hp in (2000,8000,12000) and hull.get('max_sail_hitpoints')=='100000'
    pattern=rb'(<ShipHull\b[^>]*\bid="'+identity.encode()+rb'"[^>]*\bmax_sail_hitpoints=")100000(")'
    updated,count=re.subn(pattern,lambda m:m[1]+str(hp).encode()+m[2],updated)
    assert count==1
after=ET.fromstring(updated)
for a,b in zip(before,after):
    assert a.tag==b.tag and a.get('id')==b.get('id')
    attrs=dict(b.attrib);attrs['max_sail_hitpoints']=a.get('max_sail_hitpoints')
    assert attrs==a.attrib
    assert ET.tostring(a.find('AvailableSlots'))==ET.tostring(b.find('AvailableSlots'))
target.write_bytes(updated)
assert target.read_bytes()==updated
print({h.get('id'):int(h.get('max_sail_hitpoints')) for h in after})
