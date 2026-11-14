"""Compare important prefab origins with their actual ship navmesh, read-only."""
import json,math,sys,xml.etree.ElementTree as ET
from pathlib import Path
import numpy as np
from shapely.geometry import Point
from build_boarding import MAP,OUT,NavMesh,poly,source_height

def vec(e,default):return np.array([float(s) for s in e.get('value').split(',')]) if e is not None else np.array(default)
def frame(e):
    t=e.find('transform');m=np.eye(4)
    if t is None:return m
    x,y,z=vec(t.find('rotation_euler'),[0,0,0]);cx,cy,cz=map(math.cos,[x,y,z]);sx,sy,sz=map(math.sin,[x,y,z])
    m[:3,:3]=np.array([[cz,-sz,0],[sz,cz,0],[0,0,1]])@np.array([[cy,0,sy],[0,1,0],[-sy,0,cy]])@np.array([[1,0,0],[0,cx,-sx],[0,sx,cx]])@np.diag(vec(t.find('scale'),[1,1,1]))
    m[:3,3]=vec(t.find('position'),[0,0,0]);return m
def walk(e,parent):
    world=parent@frame(e);yield e,world[:3,3].tolist()
    for c in e.findall('./children/entity'):yield from walk(c,world)

def main():
    report={}
    for ship in ('english_boat_b','kogge','carrack'):
        name=f'dadg_ws_{ship}_navmesh.bin'
        before=NavMesh.parse((MAP/'NavMeshPrefabs'/name).read_bytes()[4:])
        staged=OUT/'stage/NavMeshPrefabs'/name
        after=NavMesh.parse(staged.read_bytes()[4:]) if staged.exists() else before
        rows=[]
        for file in ('spawn_point_holder.xml','climbers_holder.xml','control_point_holder.xml','mesh_holder.xml'):
            rel=Path('NestedPrefabs')/f'dadg_ws_{ship}_ship_nested'/file
            tree=ET.parse(MAP/rel)
            candidate=ET.parse(OUT/'stage'/rel) if (OUT/'stage'/rel).exists() else tree
            for (e,p),(_,ap) in zip(walk(tree.getroot().find('entity'),np.eye(4)),walk(candidate.getroot().find('entity'),np.eye(4))):
                scripts=[s.get('name') for s in e.findall('./scripts/script')]
                name=e.get('name','')
                if not (name.startswith('sp_') or name=='climb_point' or any(s in scripts for s in ('ShipControllerMachine','ShipBallista'))):continue
                row={'file':file,'name':name,'position':p,'after_position':ap}
                for label,m,pos in [('before',before,p),('after',after,ap)]:
                    matches=[f for f in m.faces if poly(m,f).buffer(1e-5).covers(Point(pos[:2]))]
                    dz=min((abs(source_height(NavMesh(vertices=m.vertices,faces=[f]),*pos[:2])-pos[2]) for f in matches),default=None)
                    distance=min(poly(m,f).distance(Point(pos[:2])) for f in m.faces)
                    row[label]={'xy_faces':len(matches),'z_error':dz,'nearest_xy_m':round(distance,5)}
                assert row['after']['xy_faces'],row
                if row['before']['z_error'] is not None:
                    assert row['after']['z_error']<=row['before']['z_error']+.02,row
                rows.append(row)
        report[ship]=rows
        print(ship,'checked',len(rows),'no face',[(r['name'],r['position'],r['after']) for r in rows if not r['after']['xy_faces']])
    (OUT/'landing-coverage.json').write_text(json.dumps(report,indent=2))
if __name__=='__main__':main()
