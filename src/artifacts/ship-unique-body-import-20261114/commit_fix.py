"""Commit only the owned import correction; preserve all unrelated index entries."""
from pathlib import Path
import json, shutil, subprocess

OUT = Path(__file__).resolve().parent
CORE = OUT.parents[1]
PRIMARY = CORE.parents[2]
INSTALLS = (PRIMARY, PRIMARY.with_name('Mount & Blade II Bannerlord-v1.4'))
results = {}
core_paths = [f'src/artifacts/ship-visual-fixes-20261114/{n}' for n in
              ('author_sources.py','verify_roundtrip.py','build_prefabs.py','files.json')]
core_paths += [f'src/artifacts/ship-rudder-import-20261114/{n}' for n in
               ('prepare.py','write_handoff.py','import-plan.json')]
core_paths += [f'src/artifacts/ship-unique-body-import-20261114/{n}' for n in
               ('deploy.py','inspect_visibility.py','prepare_unique_bodies.py','verify_unique_bodies.py',
                'deployment-verification.json','commit_fix.py')]
core_paths += [f'src/artifacts/ship-rudder-import-20261114/tpac-compare/{n}' for n in ('Program.cs','tpac-compare.csproj')]
for install in INSTALLS:
    for module in ('DellarteDellaGuerraMap','DellarteDellaGuerra.Core'):
        root = install/'Modules'/module
        def git(*args):
            return subprocess.check_output(['rtk','proxy','git','-c','gc.auto=0','-C',str(root),*args],
                                           text=True, stderr=subprocess.PIPE)
        if module.endswith('.Core'):
            paths = core_paths
            if install != PRIMARY:
                for name in paths:
                    src = PRIMARY/'Modules'/module/name
                    dst = root/name
                    if src.resolve() != dst.resolve():
                        dst.parent.mkdir(parents=True, exist_ok=True); shutil.copy2(src,dst)
        else:
            prefix = 'Tools/WarSailsShips/visual-fixes-20261114/'
            plan = json.loads((root/prefix/'rudder-import-plan.json').read_text())['sources']
            paths = [prefix+n for n in ('README.md','REPORT.md','RUDDER_IMPORT.md','files.json','rudder-import-plan.json')]
            paths += [prefix+'authoring/'+n for n in ('source-authoring.json','unique-body-import-audit.json')]
            oldnames = {'english_boat_b':'dadg_ws_english_boat_b_rudder_separated_v1.fbx',
                        'kogge':'dadg_ws_kogge_body_without_yards_v1_rudder_separated_v1.fbx',
                        'carrack':'dadg_ws_carrack_body_without_yards_v1_rudder_separated_v1.fbx'}
            for row in plan:
                name = row['body_metamesh']
                paths += [prefix+f'authoring/{name}{ext}' for ext in ('.fbx','.blend')]
                paths += [prefix+f'authoring/prefab-stage/{row["ship"]}/mesh_holder.xml',
                          f'AssetSources/dadg_warsails/rudder/{name}.fbx',
                          'AssetSources/dadg_warsails/rudder/'+oldnames[row['ship']]]
        paths = sorted(set(paths))
        def unrelated_index():
            return [line for line in git('ls-files','--stage').splitlines() if line.split('\t',1)[-1] not in paths]
        before = unrelated_index()
        pathfile = OUT/f'{install.name}-{module}-paths.txt'
        pathfile.write_text('\n'.join(paths)+'\n')
        git('add','-f',f'--pathspec-from-file={pathfile}')
        result = git('commit','--only','-m','fix(naval): use unique mesh names for separated body imports',f'--pathspec-from-file={pathfile}')
        assert unrelated_index() == before, root
        results[str(root)] = git('rev-parse','HEAD').strip()
        print(str(root), results[str(root)], result.splitlines()[0], flush=True)
(OUT/'commits.json').write_text(json.dumps(results,indent=2)+'\n')
