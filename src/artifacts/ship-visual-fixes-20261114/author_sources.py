"""Background Blender authoring; preserves originals and writes versioned /data sources."""
import bpy, bmesh, json, hashlib, math
from pathlib import Path
from mathutils import Vector, Matrix
from mathutils.bvhtree import BVHTree

OUT=Path('D:/AI/blender-mcp/data/dadg_ship_visual_fixes_20261114_v1')
MAP=Path('D:/SteamLibrary/steamapps/common/Mount & Blade II Bannerlord/Modules/DellarteDellaGuerraMap')
CONFIG={
 'english_boat_b':dict(source='dadg_ws_english_boat_b.blend',body='dadg_ws_english_boat_b_body',rudder_vertices=22,z=.15,hinge_targets=[(-3.283,-.438),(-4.456,1.04)],hull_components=[224],cap=.65,x=.65,ys=[-3,-2,-1,0,1,2,3],ceiling=1.3),
 'kogge':dict(source='dadg_ws_kogge_body_without_yards_v1.blend',body='dadg_ws_kogge_body_without_yards_v1',rudder_vertices=302,z=-.8,hinge_targets=[(-10.74,-1.287),(-12.0,5.1)],hull_components=[946,946],cap=2.95,x=1.8,ys=[-10,-8,-6,-4,-2,0,2,4,6,8],ceiling=7.8),
 'carrack':dict(source='dadg_ws_carrack_body_without_yards_v1.blend',body='dadg_ws_carrack_body_without_yards_v1',rudder_vertices=168,z=-1.4,hinge_targets=[(-8.133,0),(-9.35,4.387)],hull_components=[4209],cap=3.85,x=1.7,ys=[-8,-6,-4,-2,0,2,4,6,8],ceiling=8.2)}

def components(mesh):
 adj=[set() for v in mesh.vertices]
 for e in mesh.edges:
  a,b=e.vertices;adj[a].add(b);adj[b].add(a)
 unvisited=set(range(len(adj)));result=[]
 while unvisited:
  seed=unvisited.pop();stack=[seed];found={seed}
  while stack:
   ns=adj[stack.pop()]&unvisited;unvisited.difference_update(ns);stack.extend(ns);found.update(ns)
  result.append(found)
 return result

def signature(ob):
 uv=ob.data.uv_layers.active
 mw=ob.matrix_world.copy()
 coords=[tuple(round(x,6) for x in mw@v.co) for v in ob.data.vertices]
 loopverts=[l.vertex_index for l in ob.data.loops]
 uvs=[tuple(round(x,6) for x in u.uv) for u in uv.data] if uv else []
 materials=[m.name if m else '' for m in ob.data.materials]
 return sorted((tuple(coords[loopverts[i]] for i in p.loop_indices),tuple(uvs[i] for i in p.loop_indices) if uv else (),materials[p.material_index]) for p in ob.data.polygons)

def subset(source,keep,name):
 ob=source.copy();ob.data=source.data.copy();ob.name=name;bpy.context.collection.objects.link(ob)
 bm=bmesh.new();bm.from_mesh(ob.data);bm.verts.ensure_lookup_table()
 bmesh.ops.delete(bm,geom=[v for v in bm.verts if v.index not in keep],context='VERTS')
 bm.to_mesh(ob.data);bm.free();ob.data.update()
 return ob

def export(ob,path):
 ob.hide_set(False);ob.hide_viewport=False;ob.hide_render=False
 bpy.ops.object.select_all(action='DESELECT');ob.select_set(True);bpy.context.view_layer.objects.active=ob
 bpy.ops.export_scene.fbx(filepath=str(path),use_selection=True,object_types={'MESH'},apply_unit_scale=True,axis_forward='Y',axis_up='Z',use_mesh_modifiers=False,mesh_smooth_type='FACE',add_leaf_bones=False,bake_anim=False)

report={}
for ship,c in CONFIG.items():
 bpy.ops.wm.open_mainfile(filepath=str(MAP/'AssetSources'/c['source']))
 print('START',ship,flush=True)
 original=bpy.data.objects[c['body']];before=signature(original);parts=components(original.data)
 rudder_set=next(p for p in parts if len(p)==c['rudder_vertices'] and max((original.matrix_world@original.data.vertices[i].co).y for i in p)<(-3 if ship=='english_boat_b' else -8))
 # Surface-supported fire sites: horizontal body faces below rigging height.
 verts=[original.matrix_world@v.co for v in original.data.vertices]
 faces=[list(p.vertices) for p in original.data.polygons if abs(p.normal.z)>.45 and max(verts[i].z for i in p.vertices)<c['ceiling']]
 tree=BVHTree.FromPolygons(verts,faces,all_triangles=False)
 fire=[]
 for y in c['ys']:
  for x in [-c['x'],c['x']]:
   loc,n,idx,dist=tree.ray_cast(Vector((x,y,c['ceiling']+.1)),Vector((0,0,-1)),c['ceiling']+3)
   if loc is not None:fire.append([round(loc.x,6),round(loc.y,6),round(loc.z+c['z']+.025,6)])
 assert len(fire)>=8,(ship,'too few supported fire positions')
 body=subset(original,set(range(len(original.data.vertices)))-rudder_set,c['body']+'_separated')
 rudder=subset(original,rudder_set,'dadg_ws_'+ship+'_rudder_v1')
 assert sorted(signature(body)+signature(rudder))==before,(ship,'body/rudder union differs')
 print('UNION PASSED',ship,flush=True)
 union_ok=True
 original_name=original.name;original.name=original_name+'_reference';body.name='dadg_ws_'+ship+'_body_no_rudder_v1';body.data.name=body.name
 # Keep the original resource intact; import the separated body under its own name.
 body_fbx=OUT/(body.name+'.fbx');export(body,body_fbx)
 # Bake rudder to its local pivot. Prefab neutral frame restores original geometry exactly.
 mw=rudder.matrix_world.copy()
 original_rudder=[mw@v.co for v in rudder.data.vertices]
 hinge=[min(original_rudder,key=lambda p:(p.y-t[0])**2+(p.z-t[1])**2) for t in c['hinge_targets']]
 slope=(hinge[1].y-hinge[0].y)/(hinge[1].z-hinge[0].z)
 angle=math.atan(-slope);rotation=Matrix.Rotation(angle,4,'X');inverse=rotation.inverted()
 pivot=Vector(((min(p.x for p in original_rudder)+max(p.x for p in original_rudder))/2,hinge[0].y+(-c['z']-hinge[0].z)*slope,0))
 source_pivot=pivot-Vector((0,0,c['z']))
 for v in rudder.data.vertices:v.co=inverse@(mw@v.co-source_pivot)
 neutral_error=max((rotation@v.co+source_pivot-before_co).length for v,before_co in zip(rudder.data.vertices,original_rudder))
 assert neutral_error<1e-5,(ship,'neutral rudder pose changed',neutral_error)
 rudder.matrix_world=Matrix.Identity(4)
 export(rudder,OUT/(rudder.name+'.fbx'))
 rudder.location=source_pivot
 rudder.rotation_euler.x=angle
 # Build a convex dry-interior envelope from the original hull's clipped outer
 # points. Keeping the solid timber shell would exclude the dry interior itself.
 wanted=list(c['hull_components']);hull_parts=[]
 for p in parts:
  if len(p) in wanted:
   hull_parts.append(p);wanted.remove(len(p))
 assert not wanted,(ship,'hull components not found')
 sdf=subset(original,set().union(*hull_parts),'sdf_dadg_ws_'+ship+'_v1')
 bm=bmesh.new();bm.from_mesh(sdf.data)
 bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.0001)
 bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),dist=.00001,plane_co=(0,0,c['cap']),plane_no=(0,0,1),clear_outer=True,clear_inner=False)
 cloud=[v.co.copy() for v in bm.verts];bm.free();bm=bmesh.new()
 for co in cloud:bm.verts.new(co)
 result=bmesh.ops.convex_hull(bm,input=list(bm.verts),use_existing_faces=False)
 interior=[g for g in result['geom_interior']+result['geom_unused'] if isinstance(g,bmesh.types.BMVert) and g.is_valid]
 if interior:bmesh.ops.delete(bm,geom=list(set(interior)),context='VERTS')
 bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
 nonmanifold=sum(not e.is_manifold for e in bm.edges)
 volume=abs(bm.calc_volume(signed=True))
 assert nonmanifold==0 and volume>0,(ship,'SDF not watertight',nonmanifold,volume)
 bm.to_mesh(sdf.data);bm.free();sdf.data.materials.clear()
 sdf.matrix_world=Matrix.Identity(4)
 for v in sdf.data.vertices:v.co.z+=c['z']
 export(sdf,OUT/(sdf.name+'.fbx'))
 sdf.location.z=-c['z']
 original.hide_render=True;original.hide_set(True)
 for ob in bpy.data.objects:
  if ob not in [body,rudder,sdf,original]:ob.hide_render=True;ob.hide_set(True)
 sdf.hide_render=True;sdf.hide_set(True)
 bpy.ops.wm.save_as_mainfile(filepath=str(OUT/('dadg_ws_'+ship+'_visual_sources_v1.blend')))
 report[ship]={'source':str(MAP/'AssetSources'/c['source']),'source_sha256':hashlib.sha256((MAP/'AssetSources'/c['source']).read_bytes()).hexdigest(),'original_body_metamesh':original_name,'body_metamesh':body.name,'body_fbx':str(body_fbx),'rudder_metamesh':rudder.name,'rudder_fbx':str(OUT/(rudder.name+'.fbx')),'rudder_pivot_root':list(pivot),'rudder_initial_rotation_x':angle,'rudder_hinge_source_points':[list(p) for p in hinge],'rudder_neutral_pose_max_error_m':neutral_error,'body_z_offset':c['z'],'body_vertices_before':len(original.data.vertices),'body_vertices_after':len(body.data.vertices),'rudder_vertices':len(rudder.data.vertices),'geometry_uv_material_union_exact':union_ok,'sdf_mesh':sdf.name,'sdf_fbx':str(OUT/(sdf.name+'.fbx')),'sdf_nonmanifold_edges':nonmanifold,'sdf_volume_m3':volume,'sdf_method':'convex envelope of original hull points clipped above central deck; native visual fit validation required','sdf_cap_root_z':c['cap']+c['z'],'fire_points_root':fire,'status':'sources_only_native_import_required'}
 print('AUTHORED',ship,'firepoints',len(fire),'SDF volume',volume,'union verified')
 (OUT/'source-authoring.json').write_text(json.dumps(report,indent=2))
print('ALL SHIPS AUTHORED')
