"""Run in a fresh Blender CLI process after preparation for independent checks."""
import bpy
import hashlib
import json
from pathlib import Path
from io_scene_fbx import parse_fbx

OUTPUT = Path('D:/AI/blender-mcp/data/dadg_ship_unique_body_import_20261114_v1')
audit = json.loads((OUTPUT / 'preparation-audit.json').read_text())

def digest(value):
 return hashlib.sha256(json.dumps(value, separators=(',', ':'), sort_keys=True).encode()).hexdigest()

def snapshot(path):
 bpy.ops.wm.read_factory_settings(use_empty=True)
 bpy.ops.import_scene.fbx(filepath=str(path))
 obs = list(bpy.data.objects)
 assert len(obs) == 1 and obs[0].type == 'MESH', 'Only one mesh and no skeleton/physics objects expected'
 ob = obs[0]
 me = ob.data
 world = [tuple(ob.matrix_world @ v.co) for v in me.vertices]
 uvs = [(layer.name, [tuple(v.uv) for v in layer.data]) for layer in me.uv_layers]
 colors = [(layer.name, layer.domain, layer.data_type,
            [tuple(v.color) for v in layer.data]) for layer in me.color_attributes]
 materials = [m.name if m else None for m in me.materials]
 polygons = [(tuple(p.vertices), p.material_index, p.use_smooth) for p in me.polygons]
 return {
  'object_name': ob.name, 'mesh_name': me.name,
  'vertices': len(me.vertices), 'edges': len(me.edges), 'polygons': len(me.polygons), 'loops': len(me.loops),
  'bounds_m': [[min(v[i] for v in world) for i in range(3)], [max(v[i] for v in world) for i in range(3)]],
  'world_vertices_sha256': digest(world), 'polygons_and_material_assignments_sha256': digest(polygons),
  'uv_sha256': digest(uvs), 'uv_layers': [u[0] for u in uvs],
  'color_sha256': digest(colors), 'color_attributes': [(c[0], c[1], c[2]) for c in colors],
  'materials': materials, 'matrix_world': [tuple(row) for row in ob.matrix_world],
  'hide_render': ob.hide_render, 'hide_viewport': ob.hide_viewport, 'hide_get': ob.hide_get(),
  'animation_present': bool(ob.animation_data), 'modifiers': [m.type for m in ob.modifiers],
 }

results = []
for a in audit['files']:
 before, after = snapshot(a['source']), snapshot(a['fbx'])
 assert after['object_name'] == after['mesh_name'] == a['id']
 checks = {key: before[key] == after[key] for key in before if key not in ('object_name', 'mesh_name')}
 assert all(checks.values()), (a['id'], checks)
 assert not any(after[k] for k in ('hide_render', 'hide_viewport', 'hide_get', 'animation_present'))
 tree, version = parse_fbx.parse(a['fbx'])
 settings = next(n for n in tree.elems if n.id == b'GlobalSettings')
 global_props = next(n for n in settings.elems if n.id == b'Properties70')
 orientation = {p.props[0].decode(): p.props[-1] for p in global_props.elems if p.props[0] in (
   b'UpAxis', b'UpAxisSign', b'FrontAxis', b'FrontAxisSign', b'CoordAxis', b'CoordAxisSign', b'UnitScaleFactor')}
 assert orientation['UpAxis'] == 2 and orientation['UpAxisSign'] == 1
 # Blender's +Y export is encoded as FBX parity FrontAxis Y with FrontAxisSign -1.
 assert orientation['FrontAxis'] == 1 and orientation['FrontAxisSign'] == -1
 results.append({'id': a['id'], 'source': before, 'output': after, 'checks': checks,
   'fbx_axis_units': orientation, 'sha256': hashlib.sha256(Path(a['fbx']).read_bytes()).hexdigest(),
   'all_checks_passed': True})
 print('VERIFIED', a['id'], after['vertices'], after['polygons'], flush=True)
report = {'status': 'all_three_independent_Blender_reimport_checks_passed_native_import_unverified',
 'source_visibility_finding': 'All three source bodies are visible in Blender; no Model Visibility/Show overrides were present. Visibility was not established as the native empty-package cause.',
 'output_visibility': 'Each new Model explicitly sets Visibility=1 and Show=true.',
 'results': results}
(OUTPUT / 'independent-verification.json').write_text(json.dumps(report, indent=2))
print('ALL VERIFIED', OUTPUT, flush=True)
