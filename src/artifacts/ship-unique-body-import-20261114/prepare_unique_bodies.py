"""Blender CLI only: losslessly rename three verified body FBXs for new imports.

Uses Blender's FBX codec instead of another geometric export so all original
vertex, polygon, UV, normal, colour and material data remain exactly unchanged.
"""
import bpy
import hashlib
import json
from pathlib import Path
from io_scene_fbx import parse_fbx, encode_bin

SOURCE = Path('D:/SteamLibrary/steamapps/common/Mount & Blade II Bannerlord/Modules/DellarteDellaGuerraMap/Tools/WarSailsShips/visual-fixes-20261114/authoring')
OUTPUT = Path('D:/AI/blender-mcp/data/dadg_ship_unique_body_import_20261114_v1')
FILES = [
 ('dadg_ws_english_boat_b_rudder_separated_v1.fbx', 'dadg_ws_english_boat_b_body_no_rudder_v1'),
 ('dadg_ws_kogge_body_without_yards_v1_rudder_separated_v1.fbx', 'dadg_ws_kogge_body_no_rudder_v1'),
 ('dadg_ws_carrack_body_without_yards_v1_rudder_separated_v1.fbx', 'dadg_ws_carrack_body_no_rudder_v1'),
]
METHODS = dict(zip(b'BCZYILFDRSilfdbc', [
 'add_bool', 'add_char', 'add_int8', 'add_int16', 'add_int32',
 'add_int64', 'add_float32', 'add_float64', 'add_bytes', 'add_string',
 'add_int32_array', 'add_int64_array', 'add_float32_array',
 'add_float64_array', 'add_bool_array', 'add_byte_array']))

def encode(node):
 result = encode_bin.FBXElem(node.id)
 for kind, value in zip(node.props_type, node.props):
  getattr(result, METHODS[kind])(value)
 result.elems = [encode(n) for n in node.elems]
 return result

def flag(name, kind, value, typ):
 return parse_fbx.FBXElem(b'P', [name, kind, b'', b'', value], bytearray(b'SSSS' + typ), [])

def serial(node):
 """Canonical byte-exact semantic fingerprint independent of compression."""
 h = hashlib.sha256()
 def walk(n):
  h.update(repr((n.id, bytes(n.props_type))).encode())
  for p in n.props:
   h.update(p.tobytes() if hasattr(p, 'tobytes') else repr(p).encode())
  for c in n.elems: walk(c)
 walk(node)
 return h.hexdigest()

assert not OUTPUT.exists(), 'Unique output package already exists; do not overwrite'
OUTPUT.mkdir(parents=True)
report = {'method': 'Blender CLI FBX codec: rename only, with explicit visible flags; no geometric re-export', 'files': []}
for source_name, target in FILES:
 source = SOURCE / source_name
 tree, version = parse_fbx.parse(str(source))
 objects = next(n for n in tree.elems if n.id == b'Objects')
 models = [n for n in objects.elems if n.id == b'Model']
 meshes = [n for n in objects.elems if n.id == b'Geometry']
 assert len(models) == len(meshes) == 1
 model, mesh = models[0], meshes[0]
 assert model.props[2] == mesh.props[2] == b'Mesh'
 original_names = {'model': model.props[1].decode(), 'geometry': mesh.props[1].decode()}
 geometry_children_before = [serial(n) for n in mesh.elems]
 old_flags = []
 props = next(n for n in model.elems if n.id == b'Properties70')
 for n in props.elems:
  if n.props[0] in (b'Visibility', b'Show'):
   old_flags.append([p.decode() if isinstance(p, bytes) else p for p in n.props])
 model.props[1] = target.encode() + b'\x00\x01Model'
 mesh.props[1] = target.encode() + b'\x00\x01Geometry'
 props.elems[:] = [p for p in props.elems if p.props[0] not in (b'Visibility', b'Show')]
 props.elems.extend([flag(b'Visibility', b'Visibility', 1.0, b'D'), flag(b'Show', b'bool', 1, b'I')])
 expected_semantics = serial(tree)
 target_fbx = OUTPUT / (target + '.fbx')
 # Keep the existing header metadata too: this is a semantic rename, not an asset rebuild.
 encode_bin._write_timedate_hack = lambda root: None
 encode_bin.write(str(target_fbx), encode(tree), version)
 reread, reread_version = parse_fbx.parse(str(target_fbx))
 assert version == reread_version and serial(reread) == expected_semantics
 new_mesh = next(n for n in next(n for n in reread.elems if n.id == b'Objects').elems if n.id == b'Geometry')
 assert geometry_children_before == [serial(n) for n in new_mesh.elems]
 bpy.ops.wm.read_factory_settings(use_empty=True)
 bpy.ops.import_scene.fbx(filepath=str(target_fbx))
 obs = [o for o in bpy.data.objects if o.type == 'MESH']
 assert len(obs) == 1
 ob = obs[0]
 assert ob.name == target and ob.data.name == target
 assert not ob.hide_render and not ob.hide_viewport and not ob.hide_get()
 target_blend = OUTPUT / (target + '.blend')
 bpy.ops.wm.save_as_mainfile(filepath=str(target_blend))
 entry = {'source': str(source), 'fbx': str(target_fbx), 'blend': str(target_blend), 'id': target,
  'source_names': original_names, 'source_explicit_visibility_show': old_flags,
  'source_hidden_override_found': False, 'output_explicit_visibility': 1.0, 'output_explicit_show': True,
  'source_sha256': hashlib.sha256(source.read_bytes()).hexdigest(),
  'fbx_sha256': hashlib.sha256(target_fbx.read_bytes()).hexdigest(),
  'geometry_children_exact': True, 'complete_expected_fbx_semantics_exact': True}
 report['files'].append(entry)
 print('PREPARED', target, flush=True)
(OUTPUT / 'preparation-audit.json').write_text(json.dumps(report, indent=2))
print('OUTPUT', OUTPUT, flush=True)
