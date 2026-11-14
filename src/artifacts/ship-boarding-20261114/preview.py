"""Offline before/after ship navmesh plot; not a game/editor screenshot."""
import json,sys
from pathlib import Path
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
from matplotlib.collections import PolyCollection
OUT=Path(__file__).resolve().parent
before=json.loads((OUT/'nav-before.json').read_text())
fig,axs=plt.subplots(2,3,figsize=(13,13),layout='constrained')
for col,ship in enumerate(('english_boat_b','kogge','carrack')):
    staged=OUT/f'{ship}-after.json'
    if not staged.exists():continue
    after=json.loads(staged.read_text())
    for row,(name,m) in enumerate([('Before',before[ship]),('After',after)]):
        ax=axs[row,col];verts=m['vertices'];faces=m['faces']
        polygons=[[verts[k][:2] for k in f['v']] for f in faces]
        colors=['#f9aa54' if f['id'] else '#dce7ef' for f in faces]
        ax.add_collection(PolyCollection(polygons,facecolors=colors,edgecolors='#496479',linewidths=.35))
        ax.autoscale();ax.set_aspect('equal');ax.set_title(f'{ship} — {name}');ax.set_xlabel('Ship X (m)');ax.set_ylabel('Ship Y (m)')
        for f in faces:
            if not f['id']:continue
            ps=[verts[k] for k in f['v']];x=sum(p[0] for p in ps)/len(ps);y=sum(p[1] for p in ps)/len(ps)
            ax.text(x,y,str(f['id']),fontsize=8,ha='center',va='center',color='#742c00')
        ax.grid(alpha=.15)
fig.suptitle('Ship navmesh boarding faces (orange IDs) — offline diagnostic, Z projected onto XY')
fig.savefig(OUT/'navmesh-before-after.png',dpi=170)
plt.close(fig)
