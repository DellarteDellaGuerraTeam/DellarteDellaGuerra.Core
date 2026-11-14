"""Build visual-only XML; --deploy-fire writes only existing-native-asset fire holders."""
from pathlib import Path
import xml.etree.ElementTree as E
import copy,json,uuid,hashlib,sys,shutil
from prefab_links import ensure_definition_links
BASE=Path(__file__).resolve().parent
DATA=Path('D:/AI/blender-mcp/data/dadg_ship_visual_fixes_20261114_v1')
MAP=BASE.parents[3]/'DellarteDellaGuerraMap'
NATIVE=BASE.parent/'ship-comparison-20261114/cog_ship_nested.xml'
AUTH=json.loads((DATA/'source-authoring.json').read_text())
ZERO='{00000000-0000-0000-0000-000000000000}'
ns=uuid.UUID('b5e20c1a-8177-4d38-ad42-0a78fd08b68f')
native=E.parse(NATIVE).getroot().find("./prefab_file[@name='ship_burning_system']/entity")
assert native is not None
node=next(e for e in native.iter('entity') if e.find("./scripts/script[@name='BurningNode']") is not None)
sound=next(e for e in native.iter('entity') if e.find("./scripts/script[@name='BurningSoundNode']") is not None)
light=next(e for e in native.iter('entity') if e.find('./components/light_component') is not None)

def identity(e,key):
 for i,link in enumerate(e.iter('prefab_link')):
  instance=link.find('instance')
  if instance is not None:instance.set('guid','{'+str(uuid.uuid5(ns,key+str(i))).upper()+'}')
 # Preserve native prefab links as prototypes: they supply inherited visual defaults.

def entity(name):
 e=E.Element('entity',name=name);E.SubElement(E.SubElement(e,'misc'),'mobility',type_name='int',value='1')
 return e
def tags(e,*names):
 outer=e.find('tags')
 if outer is None:outer=E.SubElement(e,'tags')
 inner=outer.find('tags')
 if inner is None:inner=E.SubElement(outer,'tags',type_name='string')
 for name in names:
  if inner.find("tag[@name='"+name+"']") is None:E.SubElement(inner,'tag',name=name)
def position(e,p,scale=None):
 old=e.find('transform')
 if old is not None:e.remove(old)
 tr=E.SubElement(e,'transform');E.SubElement(tr,'position',type_name='vec3',value=', '.join(f'{x:.6f}' for x in p))
 if scale is not None:E.SubElement(tr,'scale',type_name='vec3',value=', '.join([f'{scale:.6f}']*3))
def write(path,e):
 ensure_definition_links(e,path.parent.name+'/'+path.stem)
 path.parent.mkdir(parents=True,exist_ok=True);wrapper=E.Element('prefab_definition');wrapper.append(e);E.indent(wrapper,space='  ');E.ElementTree(wrapper).write(path,encoding='utf-8',xml_declaration=True)

manifest={}
for ship,a in AUTH.items():
 stage=DATA/'prefab-stage'/ship;stage.mkdir(parents=True,exist_ok=True)
 root=entity('ship_burning_system');scr=E.SubElement(E.SubElement(root,'scripts'),'script',name='ShipBurningSystem');variables=E.SubElement(scr,'variables')
 for name,value in [('Spread Rate','2.000'),('Fire Start Random Count','2'),('Max Light Intensity','1200.000' if ship=='english_boat_b' else '3000.000'),('Min Fire Progress For Light','0.100'),('Max Fire Progress For Light','0.500')]:
  E.SubElement(variables,'variable',name=name,type_name='int' if name=='Fire Start Random Count' else 'float',value=value)
 children=E.SubElement(root,'children');parents={}
 for name in ['railing_parent','ship_deck_parent','deck_upgrade_parent','mast_parent','light_parent','sound_parent']:
  par=entity(name);tags(par,name);E.SubElement(par,'children');children.append(par);parents[name]=par
 points=a['fire_points_root']
 for i,p in enumerate(points):
  n=copy.deepcopy(node);n.set('name','dadg_fire_deck_'+str(i));identity(n,ship+'/deck/'+str(i));position(n,p,.4 if ship=='english_boat_b' else .75)
  n.find("./scripts/script[@name='BurningNode']/variables/variable[@name='Node Index']").set('value',str(i))
  parents['ship_deck_parent'].find('children').append(n)
 # Sound nodes every ~6 m; points are measured surface sites, not donor positions.
 selected=[]
 for p in points[::2]:
  if not selected or abs(p[1]-selected[-1][1])>=5:selected.append(p)
 if len(selected)<2:selected=[points[0],points[-1]]
 for i,p in enumerate(selected):
  s=copy.deepcopy(sound);identity(s,ship+'/sound/'+str(i));position(s,p);parents['sound_parent'].find('children').append(s)
 for i,p in enumerate([points[len(points)//4],points[3*len(points)//4]]):
  l=copy.deepcopy(light);identity(l,ship+'/light/'+str(i));position(l,[p[0],p[1],p[2]+1]);parents['light_parent'].find('children').append(l)
 assert not list(root.iter('physics')),'fire clone must not add physics'
 assert not any(s.get('name') not in ['ShipBurningSystem','BurningNode','BurningSoundNode'] for s in root.iter('script'))
 assert len({x.get('guid') for x in root.iter('instance')})==len(list(root.iter('instance')))
 write(stage/'ship_burning_system.xml',root)
 # New geometry references stay staged until the editor imports/reimports all assets.
 live=MAP/'NestedPrefabs'/('dadg_ws_'+ship+'_ship_nested')
 rudder=E.parse(live/'rudders_holder.xml').getroot().find('entity')
 pivot=entity('dadg_ws_'+ship+'_rudder_rotation');tags(pivot,'rudder_rotation_entity' if ship=='english_boat_b' else 'dadg_rudder_visual');position(pivot,a['rudder_pivot_root'])
 E.SubElement(pivot.find('transform'),'rotation_euler',type_name='vec3',value=f"{a['rudder_initial_rotation_x']:.9f}, 0.000000000, 0.000000000")
 E.SubElement(E.SubElement(pivot,'components'),'meta_mesh_component',name=a['rudder_metamesh'])
 rudder.find('children').append(pivot);write(stage/'rudders_holder.xml',rudder)
 clip=entity('ship_water_clip');clip_scr=E.SubElement(E.SubElement(clip,'scripts'),'script',name='signed_distance_field');v=E.SubElement(clip_scr,'variables')
 E.SubElement(v,'variable',name='Visualize',type_name='bool',value='0');E.SubElement(v,'variable',name='SDF Texture',type_name='string',value=a['sdf_mesh'])
 write(stage/'ship_water_clip.xml',clip)
 # Fire is complete XML over existing assets; source-only rudder/SDF are not deployed.
 if '--deploy-fire' in sys.argv:
  dst=live/'ship_burning_system.xml'
  if dst.exists():
   old=dst.read_bytes();new=(stage/'ship_burning_system.xml').read_bytes()
   if old!=new:
    backup=DATA/'backups'/ship/'ship_burning_system.xml';backup.parent.mkdir(parents=True,exist_ok=True);backup.write_bytes(old)
  shutil.copy2(stage/'ship_burning_system.xml',dst)
 firepath=live/'ship_burning_system.xml'
 manifest[ship]={'fire_nodes':len(points),'fire_sound_nodes':len(selected),'fire_lights':2,'fire_deployed':firepath.exists() and firepath.read_bytes()==(stage/'ship_burning_system.xml').read_bytes(),'fire_sha256':hashlib.sha256((stage/'ship_burning_system.xml').read_bytes()).hexdigest(),'staged_prefabs':str(stage),'live_fire_path':str(firepath),'rudder_and_sdf_deployed':False,'rudder_driver':'native controller fallback' if ship=='english_boat_b' else 'ShipRudderVisualMissionLogic; deploy freshly built DADG Core DLL together with prefab cutover','rudder_tag':'rudder_rotation_entity' if ship=='english_boat_b' else 'dadg_rudder_visual','rudder_pivot_root':a['rudder_pivot_root'],'rudder_initial_rotation_x':a['rudder_initial_rotation_x']}
print(json.dumps(manifest,indent=2));(DATA/'prefab-manifest.json').write_text(json.dumps(manifest,indent=2))
