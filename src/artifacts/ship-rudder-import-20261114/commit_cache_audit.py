"""Commit the focused import/cache audit in both Core repositories."""
from pathlib import Path
import json, shutil, subprocess

OUT=Path(__file__).resolve().parent
PRIMARY=OUT.parents[4]
names=['CACHE_AUDIT.md','audit_untracked_cache.py','commit_native_imports.py','commit_cache_audit.py',
       'rudder-cache-index.json','map-cache-index.json','backup-cache-index.json',
       'untracked-cache-before-commit.json','untracked-cache-audit.json','native-import-commits.json',
       'tpac-compare/Program.cs']
paths=['src/artifacts/ship-rudder-import-20261114/'+n for n in names]
results={}
for install in (PRIMARY,PRIMARY.with_name('Mount & Blade II Bannerlord-v1.4')):
    root=install/'Modules/DellarteDellaGuerra.Core'
    def git(*args):
        return subprocess.check_output(['rtk','proxy','git','-c','gc.auto=0','-C',str(root),*args],text=True,stderr=subprocess.PIPE)
    if install!=PRIMARY:
        for name,rel in zip(names,paths):
            dst=root/rel;dst.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(OUT/name,dst)
    def unrelated_index():return [s for s in git('ls-files','--stage').splitlines() if s.split('\t',1)[-1] not in paths]
    before=unrelated_index()
    pathfile=OUT/f'{install.name}-cache-audit-paths.txt';pathfile.write_text('\n'.join(paths)+'\n')
    git('add','--pathspec-from-file='+str(pathfile))
    output=git('commit','--only','-m','chore(naval): record native import and orphan cache audit','--pathspec-from-file='+str(pathfile))
    assert before==unrelated_index()
    assert not git('status','--porcelain','--',*paths).strip()
    results[str(root)]=git('rev-parse','HEAD').strip()
    print(str(root),results[str(root)],output.splitlines()[0],flush=True)
(OUT/'cache-audit-commits.json').write_text(json.dumps(results,indent=2)+'\n')
