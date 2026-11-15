"""Mirror and commit the six native packages with their exact generated RDC pairs."""
from pathlib import Path
import hashlib, json, shutil, subprocess

OUT=Path(__file__).resolve().parent
PRIMARY=OUT.parents[4]
TARGET=PRIMARY.with_name('Mount & Blade II Bannerlord-v1.4')
SOURCE=TARGET/'Modules/DellarteDellaGuerraMap'
index=json.loads((OUT/'rudder-cache-index.json').read_text())
assert len(index)==6 and len({r['PackageGuid'] for r in index})==6
expected={f'dadg_ws_{ship}_{role}_v1' for ship in ('english_boat_b','kogge','carrack') for role in ('body_no_rudder','rudder')}
assert {r['Name'] for r in index}==expected
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def git(root,*args):
    return subprocess.check_output(['rtk','proxy','git','-c','gc.auto=0','-C',str(root),*args],text=True,stderr=subprocess.PIPE)

paths=[]; records=[]
for row in index:
    package=Path(row['Package']);relative=package.relative_to(SOURCE).as_posix()
    cache=SOURCE/'RuntimeDataCache'/(row['PackageGuid'].upper()+'.rdc')
    assert package.is_file() and cache.is_file() and row['Meshes']
    assert all(m['VertexCount']>0 for m in row['Meshes'])
    paths.extend((relative,cache.relative_to(SOURCE).as_posix()))
    records.append({'mesh':row['Name'],'metamesh_guid':row['Guid'],'package_guid':row['PackageGuid'],
                    'package':relative,'package_sha256':sha(package),
                    'cache':cache.relative_to(SOURCE).as_posix(),'cache_sha256':sha(cache),
                    'submesh_count':len(row['Meshes'])})
report={'status':'six_native_metameshes_and_six_matching_generated_caches_verified',
        'prefab_activation_performed':False,'in_game_steering_verified':False,'imports':records}
report_path='Tools/WarSailsShips/visual-fixes-20261114/native-import-verification.json'
paths.append(report_path)
results={}
for install in (TARGET,PRIMARY):
    root=install/'Modules/DellarteDellaGuerraMap'
    for rel in paths:
        if rel==report_path:continue
        src=SOURCE/rel;dst=root/rel
        if src.resolve()!=dst.resolve():
            if dst.exists() and sha(src)!=sha(dst):
                backup=OUT/'native-import-backup'/install.name/rel
                backup.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(dst,backup)
            dst.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(src,dst)
        assert sha(src)==sha(dst)
    (root/report_path).write_text(json.dumps(report,indent=2)+'\n')
    def other_index():return [s for s in git(root,'ls-files','--stage').splitlines() if s.split('\t',1)[-1] not in paths]
    before=other_index()
    pathfile=OUT/f'{install.name}-native-import-paths.txt';pathfile.write_text('\n'.join(paths)+'\n')
    git(root,'add','--pathspec-from-file='+str(pathfile))
    output=git(root,'commit','--only','-m','fix(naval): add imported rudder bodies and generated caches','--pathspec-from-file='+str(pathfile))
    assert other_index()==before,'Unrelated staged entries changed'
    # Verify Git LFS pointers name exactly the committed binary payloads.
    for rel in paths:
        if not rel.endswith(('.tpac','.rdc')):continue
        pointer=git(root,'show','HEAD:'+rel)
        assert 'oid sha256:'+sha(root/rel) in pointer,(root,rel)
    assert not git(root,'status','--porcelain','--',*paths).strip()
    results[str(root)]=git(root,'rev-parse','HEAD').strip()
    print(str(root),results[str(root)],output.splitlines()[0],flush=True)
(OUT/'native-import-commits.json').write_text(json.dumps(results,indent=2)+'\n')
print('Verified 6 TPAC + 6 RDC LFS payloads in each install; unrelated index entries preserved.')
