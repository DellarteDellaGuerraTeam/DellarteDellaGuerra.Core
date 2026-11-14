"""Surgical ship boarding edits; source files are read-only until deploy_boarding.py.

Uses Studio's native model/serializer, GEOS constrained triangulation for small
boundary patches, and preserves every face outside those patches/stations.
"""
import copy, hashlib, json, math, struct, sys, uuid
from collections import Counter, defaultdict
from pathlib import Path
import xml.etree.ElementTree as ET
from shapely import constrained_delaunay_triangles
from shapely.geometry import Polygon, Point, box
from shapely.ops import unary_union

OUT=Path(__file__).resolve().parent
ROOT=OUT.parents[1]
MAP=ROOT.parent.parent/'DellarteDellaGuerraMap'
sys.path.insert(0,str(ROOT/'submodules/bannerlord-agent/tools/BannerlordSceneStudio'))
from blnav.nmg9 import NavMesh, Face
sys.path.insert(0,str(ROOT/'submodules/bannerlord-agent/tools/BannerlordSceneStudio/server'))
from navmesh_diagnostics import validate_topology

SHIPS=('english_boat_b','kogge','carrack')

def poly(m,f): return Polygon([m.vertices[k][:2] for k in f.vertex_ids])
def key(p): return tuple(round(v,6) for v in p)
def f32(p): return struct.unpack('<3f',struct.pack('<3f',*p))
def orient(points):
    a=sum(p[0]*q[1]-q[0]*p[1] for p,q in zip(points,points[1:]+points[:1]))
    return points if a>0 else points[::-1]

def source_height(m,x,y):
    # Barycentric interpolation on the original face, with a small planar
    # extrapolation for kogge's new outboard lips (at most .6m).
    candidates=sorted(m.faces,key=lambda f:poly(m,f).distance(Point(x,y)))
    f=candidates[0]
    vs=[m.vertices[k] for k in f.vertex_ids]
    best=None
    for k in range(1,len(vs)-1):
        a,b,c=vs[0],vs[k],vs[k+1]
        den=(b[1]-c[1])*(a[0]-c[0])+(c[0]-b[0])*(a[1]-c[1])
        if abs(den)<1e-10:continue
        u=((b[1]-c[1])*(x-c[0])+(c[0]-b[0])*(y-c[1]))/den
        v=((c[1]-a[1])*(x-c[0])+(a[0]-c[0])*(y-c[1]))/den
        w=1-u-v; score=min(u,v,w)
        if best is None or score>best[0]:best=(score,u*a[2]+v*b[2]+w*c[2])
    assert best is not None
    return best[1]

def add_face(m,points,g3=0):
    lookup={key(v):i for i,v in enumerate(m.vertices)}
    ids=[]
    for p in orient(points):
        p=f32(p);k=key(p)
        if k not in lookup:lookup[k]=len(m.vertices);m.vertices.append(p)
        ids.append(lookup[k])
    m.faces.append(Face(ids,[],g3=g3))

def clean_ring(coords,keep,insert):
    coords=list(coords)[:-1]
    # Keep retained neighbour corners; drop only redundant collinear corners
    # owned by this patch, then insert the exact new boarding-face corners.
    changed=True
    while changed:
        changed=False
        for i,b in enumerate(coords):
            a,c=coords[i-1],coords[(i+1)%len(coords)]
            if key(b) in keep:continue
            cross=(b[0]-a[0])*(c[1]-b[1])-(b[1]-a[1])*(c[0]-b[0])
            if abs(cross)<1e-8:
                coords.pop(i);changed=True;break
    for p in insert:
        if any(math.dist(p,q)<1e-7 for q in coords):continue
        for i,a in enumerate(coords):
            b=coords[(i+1)%len(coords)]
            if abs(math.dist(a,p)+math.dist(p,b)-math.dist(a,b))<1e-7:
                coords.insert(i+1,p);break
    return coords

def boundary_patch(m,original,side,y,xstation,z,offset,inner,outer,patch_inner,face_inner=None,halfwidth=.4):
    low,high=y-halfwidth,y+halfwidth
    removed=[]
    for f in m.faces:
        vs=[m.vertices[k] for k in f.vertex_ids]
        xs=[side*v[0] for v in vs];ys=[v[1] for v in vs]
        if min(xs)>=patch_inner-1e-6 and max(xs)<=inner+1e-6 and min(ys)<high-1e-6 and max(ys)>low+1e-6:
            # Never include a stair/elevated deck in an unrelated patch.
            if max(abs(v[2]-z) for v in vs)<.4:removed.append(f)
    assert removed,(side,y,'empty patch')
    region=unary_union([poly(m,f) for f in removed])
    assert region.geom_type=='Polygon' and not region.interiors
    remaining=[f for f in m.faces if not any(f is r for r in removed)]
    keep={key(m.vertices[k][:2]) for f in remaining for k in f.vertex_ids}
    face_inner=inner if face_inner is None else face_inner
    newquad=[(side*face_inner,low),(side*outer,low),(side*outer,high),(side*face_inner,high)]
    # On a narrowing deck, keep the whole boarding edge open instead of
    # leaving a partially shared edge across an old stepped boundary.
    if outer<inner-1e-6:
        clip=box(outer,low,inner+1,high) if side>0 else box(-inner-1,low,-outer,high)
        region=region.difference(clip)
    region=region.difference(Polygon(orient(newquad)))
    assert region.geom_type=='Polygon' and not region.interiors
    ring=clean_ring(region.exterior.coords,keep,newquad)
    region=Polygon(ring);assert region.is_valid
    triangles=constrained_delaunay_triangles(region)
    assert abs(sum(t.area for t in triangles.geoms)-region.area)<1e-7
    m.faces=remaining
    for t in triangles.geoms:
        add_face(m,[(x,yy,source_height(original,x,yy)) for x,yy in list(t.exterior.coords)[:-1]])
    add_face(m,[(x,yy,source_height(original,x,yy)) for x,yy in newquad],offset)

def stations(ship):
    final=OUT/'final-stations.json'
    if final.exists() and ship in json.loads(final.read_text()):
        return json.loads(final.read_text())[ship]
    data=json.loads((OUT/'geometry/candidates.json').read_text())[ship]['candidates']
    result=[]
    for i,c in enumerate(data):
        x,y,z=c['position']
        if ship=='english_boat_b' and y<0:y=-1.95
        result.append({'id':i+1,'position':[x,y,z]})
    return result

def prefab(ship,points):
    src=MAP/'NestedPrefabs'/f'dadg_ws_{ship}_ship_nested'/'attachment_machine_holder.xml'
    tree=ET.parse(src);children=tree.find('./entity/children');templates=list(children)
    assert len(templates)==2
    children[:]=[]
    for p in points:
        side=0 if p['position'][0]<0 else 1
        e=copy.deepcopy(templates[side]);old_id=side+1
        if p['id']!=old_id:
            for inst in e.findall('.//prefab_link/instance'):
                inst.set('guid','{'+str(uuid.uuid5(uuid.NAMESPACE_URL,f'dadg/boarding/20261114/{ship}/{p["id"]}/{inst.get("guid")}')).upper()+'}')
        e.find('./transform/position').set('value',', '.join(f'{v:.6f}' for v in p['position']))
        for v in e.findall('.//variable[@name="RelatedShipNavmeshOffset"]'):v.set('value',str(p['id']))
        lift=0
        if ship=='english_boat_b':lift=.25 if p['id']<=2 else .35
        elif ship=='kogge' and p['id']>=5:lift=.10
        elif ship=='kogge' and p['id']>=3:lift=.05
        elif ship=='carrack' and p['id']<=2:lift=.15
        if lift:
            point=e.find('./children/entity[@name="attachment_point_machine"]')
            for path in ('./children/entity[@name="visual"]/children/entity[@name="bridge_target_point"]','./children/entity[@name="visual"]/children/entity[@name="bridge_source_point"]','./children/entity[@name="visual"]/children/entity[@name="bridge_connection_point"]','./children/entity[@name="capsule_physics_a"]'):
                pos=point.find(path+'/transform/position');assert pos is not None,(ship,path)
                xyz=[float(v) for v in pos.get('value').split(',')];xyz[2]+=lift
                pos.set('value',', '.join(f'{v:.6f}' for v in xyz))
        children.append(e)
    ET.indent(tree,space='  ')
    guids=[g.get('guid') for g in tree.findall('.//prefab_link/instance')]
    assert len(guids)==len(set(guids)),(ship,'duplicate instance GUID')
    for e,p in zip(children,points):
        for script_name in ('ShipAttachmentMachine','ShipAttachmentPointMachine'):
            scripts=e.findall(f'.//script[@name="{script_name}"]')
            assert len(scripts)==1
            assert scripts[0].find('./variables/variable[@name="RelatedShipNavmeshOffset"]').get('value')==str(p['id'])
    return src,ET.tostring(tree.getroot(),encoding='utf-8',xml_declaration=True)+b'\n'

def kogge_climbers():
    src=MAP/'NestedPrefabs/dadg_ws_kogge_ship_nested/climbers_holder.xml'
    tree=ET.parse(src)
    for root in tree.findall('./entity/children/entity'):
        x=float(root.find('./transform/position').get('value').split(',')[0])
        target=root.find('./children/entity[@name="climb_point"]/transform/position')
        values=[float(s) for s in target.get('value').split(',')]
        # Roots face outboard (port pi, starboard zero); shift only the landing
        # marker .3m aft in ship space, preserving the existing rope chain.
        values[1]=.3 if x<0 else -.3
        target.set('value',', '.join(f'{v:.6f}' for v in values))
    ET.indent(tree,space='  ')
    return src,ET.tostring(tree.getroot(),encoding='utf-8',xml_declaration=True)+b'\n'

def compact(m):
    used=sorted({k for f in m.faces for k in f.vertex_ids});lookup={old:i for i,old in enumerate(used)}
    m.vertices=[m.vertices[k] for k in used]
    for f in m.faces:f.vertex_ids=[lookup[k] for k in f.vertex_ids]
    m.rebuild_edges()

def validate(m,points):
    assert validate_topology(m)['passed']
    uses=defaultdict(list)
    for i,f in enumerate(m.faces):
        assert len(f.vertex_ids) in(3,4) and len(set(f.vertex_ids))==len(f.vertex_ids)
        vs=[m.vertices[k] for k in f.vertex_ids]
        assert poly(m,f).is_valid and poly(m,f).area>1e-8
        assert sum(a[0]*b[1]-b[0]*a[1] for a,b in zip(vs,vs[1:]+vs[:1]))>0
        assert(f.g1,f.g2,f.g4,f.g5,f.g6)==(2,0,0,-1,0)
        for j,e in enumerate(f.edge_ids):
            uses[e].append(i)
            assert {m.edges[e].va,m.edges[e].vb}=={f.vertex_ids[j],f.vertex_ids[(j+1)%len(f.vertex_ids)]}
    assert all(len(v)in(1,2) for v in uses.values())
    # Connected via whole shared edges, not corner-touching patches.
    unseen=set(range(len(m.faces)));components=[]
    while unseen:
        pending=[unseen.pop()];size=0
        while pending:
            i=pending.pop();size+=1
            for e in m.faces[i].edge_ids:
                for j in uses[e]:
                    if j in unseen:unseen.remove(j);pending.append(j)
        components.append(size)
    assert len(components)==1,components
    counts=Counter(f.g3 for f in m.faces)
    assert {i:n for i,n in counts.items() if i}=={p['id']:1 for p in points}
    for p in points:
        f=next(f for f in m.faces if f.g3==p['id'])
        assert poly(m,f).contains(Point(p['position'][:2])),p
        assert any(len(uses[e])==1 for e in f.edge_ids),p
        assert abs(source_height(m,*p['position'][:2])-p['position'][2])<.15,p
    # 3D T junctions: a vertex must never stop in the middle of another edge.
    for e in m.edges:
        a,b=m.vertices[e.va],m.vertices[e.vb];v=[b[i]-a[i] for i in range(3)];length=sum(c*c for c in v)
        for k,p in enumerate(m.vertices):
            if k in(e.va,e.vb):continue
            t=sum((p[i]-a[i])*v[i] for i in range(3))/length
            if 1e-5<t<1-1e-5:
                d=math.dist(p,[a[i]+t*v[i] for i in range(3)])
                assert d>2e-5,('T junction',e,k,d)
    return {'connected_components':components,'station_ids':dict(counts),'topology':validate_topology(m,False)}

def main():
    report={};manifest=[]
    for ship in SHIPS:
        p=MAP/'NavMeshPrefabs'/f'dadg_ws_{ship}_navmesh.bin';raw=p.read_bytes()
        assert struct.unpack('<I',raw[:4])[0]==len(raw)-4
        original=NavMesh.parse(raw[4:]);assert original.serialize()==raw[4:]
        m=copy.deepcopy(original);points=stations(ship)
        if ship=='english_boat_b':
            # Old outboard aft corners crossed the bench collision. Pull only
            # those boundary vertices onto the surveyed clear low deck.
            for k,(x,y,z) in enumerate(m.vertices):
                if abs(abs(x)-.557)<1e-4 and abs(y+2.2842)<1e-4:
                    m.vertices[k]=f32((math.copysign(.5,x),-2.1,z))
            # The foredeck already exists: split its one quad into left/right
            # triangles without widening the boat or changing the aft helm area.
            f=m.faces.pop(1);a,b,c,d=f.vertex_ids
            for ids,g in [([a,b,c],4),([a,c,d],3)]:m.faces.append(Face(ids,[],g3=g))
        elif ship=='kogge':
            m.faces=[f for f in m.faces if not f.g3]
            for station in points:
                x,y,z=station['position'];side=-1 if x<0 else 1
                # Use the exact existing f32 boundary coordinate.
                inner=min(abs(v[0]) for v in original.vertices if abs(abs(v[0])-2.2)<1e-5)
                boundary_patch(m,original,side,y,x,z,station['id'],inner,2.8,1.8)
            for side in (-1,1):
                boundary_patch(m,original,side,-.3,side*3,1.66846,0,inner,3.05,1.8,halfwidth=.3)
        elif ship=='carrack':
            # Existing side lips also support the rope-climber landings.
            # Keep them as ordinary walkable faces after moving the boarding.
            for f in m.faces:f.g3=0
            for station in points:
                x,y,z=station['position'];side=-1 if x<0 else 1
                if y<8:
                    inner=min(abs(v[0]) for v in original.vertices if abs(abs(v[0])-2.45)<1e-5)
                    boundary_patch(m,original,side,y,x,z,station['id'],inner,inner,1.9,face_inner=2.0)
                else:
                    inner=min(abs(v[0]) for v in original.vertices if abs(abs(v[0])-1.4)<1e-5)
                    boundary_patch(m,original,side,y,x,z,station['id'],inner,1.2,.6,face_inner=1.0)
        compact(m);checks=validate(m,points)
        payload=m.serialize();data=struct.pack('<I',len(payload))+payload
        assert NavMesh.parse(data[4:]).serialize()==payload
        files=[(p,data),prefab(ship,points)]
        if ship=='kogge':files.append(kogge_climbers())
        for live,content in files:
            rel=live.relative_to(MAP);backup=OUT/'backup'/rel;target=OUT/'stage'/rel
            backup.parent.mkdir(parents=True,exist_ok=True);target.parent.mkdir(parents=True,exist_ok=True)
            before=live.read_bytes()
            if backup.exists():assert backup.read_bytes()==before,'Source changed since backup'
            else:backup.write_bytes(before)
            target.write_bytes(content)
            manifest.append({'path':str(rel),'before_sha256':hashlib.sha256(before).hexdigest(),'after_sha256':hashlib.sha256(content).hexdigest()})
        (OUT/f'{ship}-after.obj').write_text(m.to_obj())
        (OUT/f'{ship}-after.json').write_text(json.dumps({'vertices':m.vertices,'faces':[{'v':f.vertex_ids,'id':f.g3} for f in m.faces]},indent=2))
        report[ship]={'before':original.stats(),'after':m.stats(),'stations':points,'checks':checks}
        print(ship,original.stats(),'->',m.stats(),'PASS')
    (OUT/'verification.json').write_text(json.dumps(report,indent=2))
    (OUT/'manifest.json').write_text(json.dumps(manifest,indent=2))

if __name__=='__main__':main()
