import bpy,json
from pathlib import Path
ROOT=Path('D:/AI/blender-mcp/data/dadg_ship_visual_fixes_20261114_v1')
report={}
for ship in ['english_boat_b','kogge','carrack']:
 bpy.ops.wm.open_mainfile(filepath=str(ROOT/('dadg_ws_'+ship+'_visual_sources_v1.blend')))
 ob=bpy.data.objects['dadg_ws_'+ship+'_rudder_v1'];coords=[list(ob.matrix_world@v.co) for v in ob.data.vertices]
 body=next(o for o in bpy.data.objects if o.name.startswith('dadg_ws_'+ship+'_body') and not o.name.endswith('_reference'))
 near=[[list(body.matrix_world@body.data.vertices[i].co) for i in e.vertices] for e in body.data.edges if all(body.data.vertices[i].co.y < (-3 if ship=='english_boat_b' else -7) and body.data.vertices[i].co.z<6 for i in e.vertices)]
 report[ship]={'coords':coords,'edges':[list(e.vertices) for e in ob.data.edges],'hull_edges':near}
(ROOT/'rudder-profiles.json').write_text(json.dumps(report))
