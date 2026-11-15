"""Read-only GUID/provenance audit of cache files; never deletes generated data."""
from pathlib import Path
from datetime import datetime, timezone
import hashlib, json, subprocess

OUT = Path(__file__).resolve().parent
PRIMARY = OUT.parents[4]
TARGET = PRIMARY.with_name('Mount & Blade II Bannerlord-v1.4')
MAP = TARGET/'Modules/DellarteDellaGuerraMap'
index = json.loads((OUT/'map-cache-index.json').read_text())
rudders = json.loads((OUT/'rudder-cache-index.json').read_text())
by_guid = {r['PackageGuid'].lower():r for r in index}
assert len(by_guid) == len(index), 'Duplicate package identities need investigation'
rudder_guids = {r['PackageGuid'].lower() for r in rudders}
assert len(rudder_guids) == 6
backup_index=json.loads((OUT/'backup-cache-index.json').read_text())
old_guid={r['PackageGuid'].lower():r for r in backup_index if 'PackageGuid' in r}

def git(root, *args):
    return subprocess.check_output(['rtk','proxy','git','-C',str(root),*args], text=True, stderr=subprocess.PIPE)
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()

repos=[]
for install in (PRIMARY,TARGET):
    for module in ('DellarteDellaGuerra','DellarteDellaGuerraMap','DellarteDellaGuerraScenes','DellarteDellaGuerra.Core'):
        root=install/'Modules'/module
        if not (root/'.git').exists(): continue
        files=git(root,'ls-files','--others','--exclude-standard','RuntimeDataCache').splitlines()
        repos.append({'root':str(root),'untracked_caches':len(files),'paths':files})

files=git(MAP,'ls-files','--others','--exclude-standard','RuntimeDataCache').splitlines()
rows=[]
for name in files:
    path=MAP/name; guid=path.stem.lower(); owner=by_guid.get(guid)
    source=PRIMARY/'Modules/DellarteDellaGuerraMap'/name
    row={'path':name,'bytes':path.stat().st_size,'sha256':sha(path),
         'modified_utc':datetime.fromtimestamp(path.stat().st_mtime,timezone.utc).isoformat(),
         'current_package':owner,'rudder_import':guid in rudder_guids,
         'primary_exists':source.exists(), 'primary_identical':source.exists() and sha(source)==sha(path)}
    previous=old_guid.get(guid)
    row['previous_package']=previous
    if previous:
        oldpath=Path(previous['Package']).resolve()
        marker=Path('Modules/DellarteDellaGuerraMap/Assets')
        relative=Path('Assets')/oldpath.relative_to(OUT/'v14-mirror/backup'/marker)
        current=next((r for r in index if Path(r['Package']).relative_to(MAP)==relative),None)
        assert current is not None and current['PackageGuid'].lower()!=guid
        row['replacement_package']=current
        replacement_cache=MAP/'RuntimeDataCache'/(current['PackageGuid'].upper()+'.rdc')
        assert replacement_cache.is_file()
        assert git(MAP,'ls-files','--',replacement_cache.relative_to(MAP).as_posix()).strip()
        row['replacement_cache_exists_and_is_tracked']=True
    if source.exists():
        row['primary_tracked']=bool(git(source.parent.parent,'ls-files','--',name).strip())
    rows.append(row)
report={'repositories':repos,'untracked_map_cache_count':len(rows),
        'rudder_cache_count':sum(r['rudder_import'] for r in rows),
        'other_current_packages':sum(r['current_package'] is not None and not r['rudder_import'] for r in rows),
        'no_current_map_package':sum(r['current_package'] is None for r in rows),
        'matched_previous_texture_packages':sum(r['previous_package'] is not None for r in rows),
        'untracked_map_cache_bytes':sum(r['bytes'] for r in rows),'caches':rows}
main_index=json.loads((OUT/'main-cache-index.json').read_text())
main_guids={r['PackageGuid'].lower():r for r in main_index if 'PackageGuid' in r}
report['main_module_packages_unreadable']=[r for r in main_index if 'Error' in r]
report['main_module_untracked_caches']=[]
for repo in repos:
    if Path(repo['root']).name!='DellarteDellaGuerra':continue
    for name in repo['paths']:
        p=Path(repo['root'])/name
        report['main_module_untracked_caches'].append({'path':str(p),'bytes':p.stat().st_size,
            'sha256':sha(p),'modified_utc':datetime.fromtimestamp(p.stat().st_mtime,timezone.utc).isoformat(),
            'owner':main_guids.get(p.stem.lower())})
(OUT/'untracked-cache-audit.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps({k:v for k,v in report.items() if k not in ('repositories','caches','main_module_packages_unreadable')},indent=2))
print('Unreadable main-module packages:',len(report['main_module_packages_unreadable']))
for repo in repos: print(repo['root'],repo['untracked_caches'])
for row in rows:
    owner=row['current_package']
    print(row['path'],row['bytes'], 'rudder' if row['rudder_import'] else 'other',
          Path(owner['Package']).name if owner else 'NO CURRENT MAP PACKAGE',
          'primary-identical='+str(row['primary_identical']))
