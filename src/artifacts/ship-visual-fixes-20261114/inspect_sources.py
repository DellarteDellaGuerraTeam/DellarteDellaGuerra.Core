import bpy, json, os, math
from pathlib import Path
from mathutils import Vector
OUT=Path('D:/AI/blender-mcp/data/dadg_ship_visual_fixes_20261114_v1')
OUT.mkdir(parents=True,exist_ok=True)
MAP=Path('D:/SteamLibrary/steamapps/common/Mount & Blade II Bannerlord/Modules/DellarteDellaGuerraMap')
SOURCES={'english_boat_b':'dadg_ws_english_boat_b.blend','kogge':'dadg_ws_kogge_body_without_yards_v1.blend','carrack':'dadg_ws_carrack_body_without_yards_v1.blend'}
report={}
for ship,filename in SOURCES.items():
    bpy.ops.wm.open_mainfile(filepath=str(MAP/'AssetSources'/filename))
    objs=[]
    for ob in bpy.data.objects:
        if ob.type!='MESH':continue
        co=[ob.matrix_world@v.co for v in ob.data.vertices]
        adj=[set() for _ in co]
        for e in ob.data.edges:
            a,b=e.vertices;adj[a].add(b);adj[b].add(a)
        unvisited=set(range(len(co)));components=[]
        while unvisited:
            seed=unvisited.pop();stack=[seed];found=[seed]
            while stack:
                i=stack.pop();ns=adj[i]&unvisited;unvisited.difference_update(ns);stack.extend(ns);found.extend(ns)
            points=[co[i] for i in found]
            lo=[min(p[a] for p in points) for a in range(3)];hi=[max(p[a] for p in points) for a in range(3)]
            components.append({'vertices':len(found),'min':lo,'max':hi})
        objs.append({'name':ob.name,'vertices':len(co),'faces':len(ob.data.polygons),'materials':[m.name if m else None for m in ob.data.materials], 'min':[min(p[a] for p in co) for a in range(3)],'max':[max(p[a] for p in co) for a in range(3)],'components':sorted(components,key=lambda c:c['vertices'],reverse=True)})
    report[ship]={'source':filename,'objects':objs}
    print(ship,[(o['name'],o['vertices'],o['min'],o['max'],len(o['components'])) for o in objs])
(OUT/'source-inspection.json').write_text(json.dumps(report,indent=2))
print('REPORT',OUT/'source-inspection.json')
