import bpy,bmesh,json,hashlib
from pathlib import Path
from mathutils import Vector,Matrix
DATA=Path('D:/AI/blender-mcp/data/dadg_ship_visual_fixes_20261114_v1')
auth=json.loads((DATA/'source-authoring.json').read_text())

def fingerprint(ob,extra=Matrix.Identity(4)):
 mw=extra@ob.matrix_world;me=ob.data
 coords=[tuple(round(x,4) for x in mw@v.co) for v in me.vertices]
 uv=me.uv_layers.active;uvs=[tuple(round(x,5) for x in u.uv) for u in uv.data] if uv else []
 loops=[l.vertex_index for l in me.loops];mats=[m.name if m else '' for m in me.materials]
 faces=[]
 for p in me.polygons:
  ring=[(coords[loops[i]],uvs[i] if uv else ()) for i in p.loop_indices]
  canonical=min(tuple(ring[n:]+ring[:n]) for n in range(len(ring)))
  faces.append((canonical,mats[p.material_index]))
 return faces

report={}
for ship,a in auth.items():
 bpy.ops.wm.open_mainfile(filepath=a['source']);original_name=a.get('original_body_metamesh',a['body_metamesh']);baseline=fingerprint(bpy.data.objects[original_name]);body_count=len(bpy.data.objects[original_name].data.polygons)
 bpy.ops.wm.read_factory_settings(use_empty=True)
 bpy.ops.import_scene.fbx(filepath=a['body_fbx']);ob=next(o for o in bpy.data.objects if o.type=='MESH');body=fingerprint(ob)
 body_import_name=ob.name
 bpy.ops.wm.read_factory_settings(use_empty=True)
 bpy.ops.import_scene.fbx(filepath=a['rudder_fbx']);ob=next(o for o in bpy.data.objects if o.type=='MESH')
 p=Vector(a['rudder_pivot_root'])-Vector((0,0,a['body_z_offset']))
 extra=Matrix.Translation(p)@Matrix.Rotation(a['rudder_initial_rotation_x'],4,'X')
 rudder=fingerprint(ob,extra)
 assert sorted(body+rudder)==sorted(baseline),(ship,'FBX union/UV/material roundtrip differs',len(baseline),len(body+rudder))
 bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=a['sdf_fbx']);ob=next(o for o in bpy.data.objects if o.type=='MESH')
 bm=bmesh.new();bm.from_mesh(ob.data);nonmanifold=sum(not e.is_manifold for e in bm.edges);volume=abs(bm.calc_volume());bm.free()
 assert nonmanifold==0 and abs(volume-a['sdf_volume_m3'])/volume<.00001
 report[ship]={'fbx_body_rudder_polygon_uv_material_union_passed':True,'original_polygon_count':body_count,'split_body_polygon_count':len(body),'rudder_polygon_count':len(rudder),'body_import_name':body_import_name,'sdf_import_nonmanifold_edges':nonmanifold,'sdf_import_volume_m3':volume,'status':'offline_roundtrip_passed_native_import_unverified'}
 print('ROUNDTRIP PASSED',ship,flush=True)
(DATA/'roundtrip-verification.json').write_text(json.dumps(report,indent=2))
print('ALL ROUNDTRIPS PASSED')
