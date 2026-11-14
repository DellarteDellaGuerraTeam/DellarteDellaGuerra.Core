"""Deploy only hash-verified station/navmesh pairs, retaining original backups."""
import hashlib,json,sys
from pathlib import Path

OUT=Path(__file__).resolve().parent
MAP=OUT.parents[1].parent.parent/'DellarteDellaGuerraMap'
def sha(data):return hashlib.sha256(data).hexdigest()

manifest=json.loads((OUT/'manifest.json').read_text())
assert len(manifest)>=6,'All three ships must be ready before deployment'
files=[]
for row in manifest:
    live=(MAP/row['path']).resolve();stage=OUT/'stage'/row['path'];backup=OUT/'backup'/row['path']
    assert live.is_relative_to(MAP.resolve())
    assert (live.name=='attachment_machine_holder.xml' and live.parent.name in ('dadg_ws_english_boat_b_ship_nested','dadg_ws_kogge_ship_nested','dadg_ws_carrack_ship_nested')) or (live.parent.name=='NavMeshPrefabs' and live.name in ('dadg_ws_english_boat_b_navmesh.bin','dadg_ws_kogge_navmesh.bin','dadg_ws_carrack_navmesh.bin')) or (live.name=='climbers_holder.xml' and live.parent.name=='dadg_ws_kogge_ship_nested')
    original=live.read_bytes();candidate=stage.read_bytes()
    assert sha(original)==row['before_sha256'],f'Live file changed: {live}'
    assert sha(backup.read_bytes())==row['before_sha256'],f'Invalid backup: {backup}'
    assert sha(candidate)==row['after_sha256'],f'Stage changed: {stage}'
    files.append((live,original,candidate))

# Preserve an explicit identity of the campaign map: only ship navmeshes belong
# to this task. It must remain byte-identical throughout deployment.
campaign=MAP/'SceneObj/Main_map/navmesh.bin'
campaign_sha=sha(campaign.read_bytes()) if campaign.exists() else None
changed=[]
try:
    for live,original,candidate in files:
        changed.append((live,original));live.write_bytes(candidate)
        assert live.read_bytes()==candidate
    assert campaign_sha is None or sha(campaign.read_bytes())==campaign_sha
except BaseException:
    for live,original in reversed(changed):live.write_bytes(original)
    raise
receipt={'files':manifest,'verified_live_sha256':{str(p.relative_to(MAP)):sha(p.read_bytes()) for p,_,_ in files},'campaign_map_navmesh_unchanged_sha256':campaign_sha}
(OUT/'deployment.json').write_text(json.dumps(receipt,indent=2))
print(f'Deployed and re-read {len(files)} files. Campaign map unchanged. Backups: {OUT / "backup"}')
