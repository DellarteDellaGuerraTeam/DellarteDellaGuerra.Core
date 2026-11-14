"""Prepare uniquely named native body imports in both installs; no live cutover."""
from pathlib import Path
import copy, hashlib, json, shutil, subprocess, sys
import xml.etree.ElementTree as ET

OUT = Path(__file__).resolve().parent
CORE = OUT.parents[1]
PRIMARY = CORE.parents[2]
INSTALLS = (PRIMARY, PRIMARY.with_name('Mount & Blade II Bannerlord-v1.4'))
NEW = Path('D:/AI/blender-mcp/data/dadg_ship_unique_body_import_20261114_v1')
audit = json.loads((NEW/'independent-verification.json').read_text())
prepared = json.loads((NEW/'preparation-audit.json').read_text())
assert len(audit['results']) == 3 and all(r['all_checks_passed'] for r in audit['results'])
assert all(r['geometry_children_exact'] and r['complete_expected_fbx_semantics_exact'] for r in prepared['files'])

def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def save(p, data):
    p.parent.mkdir(parents=True, exist_ok=True)
    if p.exists() and p.read_bytes() != data:
        backup = OUT/'backup'/p.relative_to(PRIMARY.parent)
        backup.parent.mkdir(parents=True, exist_ok=True)
        if not backup.exists(): shutil.copy2(p, backup)
    p.write_bytes(data)
def jsave(p, data): save(p, (json.dumps(data, indent=2)+'\n').encode())
def canon(tree): return ET.canonicalize(ET.tostring(tree.getroot(), encoding='unicode'), strip_text=True)

# Preflight every file that will be retired before mutating either install.
for install in INSTALLS:
    dest = install/'Modules/DellarteDellaGuerraMap/AssetSources/dadg_warsails/rudder'
    for row in prepared['files']:
        old = dest/Path(row['source']).name
        if old.exists():
            assert old.resolve().parent == dest.resolve() and sha(old) == row['source_sha256'], old
        new = NEW/(row['id']+'.fbx')
        assert sha(new) == row['fbx_sha256']

receipt = []
for install in INSTALLS:
    module = install/'Modules/DellarteDellaGuerraMap'
    package = module/'Tools/WarSailsShips/visual-fixes-20261114'
    author = package/'authoring'
    dest = module/'AssetSources/dadg_warsails/rudder'
    core = install/'Modules/DellarteDellaGuerra.Core/src'
    auth = json.loads((author/'source-authoring.json').read_text())
    oldplan = json.loads((core/'artifacts/ship-rudder-import-20261114/import-plan.json').read_text())
    live_hashes = {}
    for ship, a in auth.items():
        ident = f'dadg_ws_{ship}_body_no_rudder_v1'
        source = next(r for r in prepared['files'] if r['id'] == ident)
        a['original_body_metamesh'] = a.get('original_body_metamesh', a['body_metamesh'])
        a['body_metamesh'] = ident
        a['body_fbx'] = str(author/(ident+'.fbx'))
        for ext in ('.fbx', '.blend'):
            save(author/(ident+ext), (NEW/(ident+ext)).read_bytes())
        # Retire only an exact, verified obsolete handoff source, keeping a backup.
        old = dest/Path(source['source']).name
        if old.exists():
            assert old.resolve().parent == dest.resolve() and sha(old) == source['source_sha256']
            backup = OUT/'backup'/install.name/'retired-sources'/old.name
            backup.parent.mkdir(parents=True, exist_ok=True)
            if not backup.exists(): shutil.copy2(old, backup)
            assert sha(backup) == sha(old)
            old.unlink()
        # Clone live holder and change exactly one mesh-name attribute; preserve all IDs.
        live = module/f'NestedPrefabs/dadg_ws_{ship}_ship_nested/mesh_holder.xml'
        live_hashes[str(live)] = sha(live)
        tree = ET.parse(live)
        matches = tree.findall(f'.//meta_mesh_component[@name="{a["original_body_metamesh"]}"]')
        assert len(matches) == 1
        matches[0].set('name', ident)
        target = author/f'prefab-stage/{ship}/mesh_holder.xml'
        ET.indent(tree, space='  ')
        import io
        stream = io.BytesIO(); tree.write(stream, encoding='utf-8', xml_declaration=True)
        save(target, stream.getvalue())
        verified = ET.parse(target)
        for node in verified.getroot().iter():
            if node.tag in ('entity','script') or node.tag.endswith('_component'):
                assert node.find('prefab_link/instance') is not None, (target, node.tag)
                assert node.find('prefab_link/prefab') is not None, (target, node.tag)
        replacement = verified.find(f'.//meta_mesh_component[@name="{ident}"]')
        assert replacement is not None
        replacement.set('name', a['original_body_metamesh'])
        assert canon(verified) == canon(ET.parse(live))
        previous = next(r for r in oldplan if r['ship'] == ship)
        assert sha(dest/Path(a['rudder_fbx']).name) == previous['rudder_sha256']
    jsave(author/'source-authoring.json', auth)
    combined = {'preparation': prepared, 'independent_verification': audit,
                'native_import_verified': False, 'live_prefab_cutover': False}
    jsave(author/'unique-body-import-audit.json', combined)
    files = json.loads((package/'files.json').read_text())
    names = {r['path'] for r in files}
    names.update(r['id']+ext for r in prepared['files'] for ext in ('.fbx', '.blend'))
    names.add('unique-body-import-audit.json')
    names.update(f'prefab-stage/{ship}/mesh_holder.xml' for ship in auth)
    jsave(package/'files.json', [{'path':name, 'bytes':(author/name).stat().st_size,
                                'sha256':sha(author/name)} for name in sorted(names)])
    # Keep local authoring copies and generators current without changing compiled packages.
    local_author = core/'artifacts/ship-visual-fixes-20261114/authoring'
    changed = ['source-authoring.json','unique-body-import-audit.json']
    changed += [r['id']+ext for r in prepared['files'] for ext in ('.fbx','.blend')]
    changed += [f'prefab-stage/{ship}/mesh_holder.xml' for ship in auth]
    for name in changed: save(local_author/name, (author/name).read_bytes())
    save(core/'artifacts/ship-visual-fixes-20261114/files.json', (package/'files.json').read_bytes())
    scripts = [f'artifacts/ship-visual-fixes-20261114/{name}' for name in
               ('author_sources.py','verify_roundtrip.py','build_prefabs.py')]
    scripts += [f'artifacts/ship-rudder-import-20261114/{name}' for name in ('prepare.py','write_handoff.py')]
    if core != CORE:
        for name in scripts: save(core/name, (CORE/name).read_bytes())
    for name in ('prepare.py','write_handoff.py'):
        subprocess.run([sys.executable, str(core/'artifacts/ship-rudder-import-20261114'/name)], check=True)
    newplan = json.loads((core/'artifacts/ship-rudder-import-20261114/import-plan.json').read_text())
    expected = {Path(r[k+'_fbx']).name for r in newplan for k in ('body','rudder')}
    assert {p.name for p in dest.glob('*.fbx')} == expected and len(expected) == 6
    assert all(sha(dest/Path(r[k+'_fbx']).name) == r[k+'_sha256'] for r in newplan for k in ('body','rudder'))
    assert all(sha(Path(p)) == h for p,h in live_hashes.items())
    readme = ('# Inactive ship visual source handoff\n\n'
              'Use [RUDDER_IMPORT.md](RUDDER_IMPORT.md) for current instructions. Import the three uniquely named '
              '`*_body_no_rudder_v1.fbx` files as new meshes; keep the three matching imported rudders. '
              'The six current sources are in `AssetSources/dadg_warsails/rudder`. Original body resources remain intact. '
              'The staged mesh_holder.xml and rudders_holder.xml must be activated together after native verification. '
              'SDF water clipping is separate and remains inactive.\n')
    save(package/'README.md', readme.encode())
    report = (package/'REPORT.md').read_text(encoding='utf-8-sig')
    start = report.index('## Required native import and cutover')
    end = report.index('## Deliverables and repeatable verification', start)
    report = report[:start]+'''## Required native import and cutover

Current actionable instructions are in **RUDDER_IMPORT.md**. The earlier body FBXs reused existing body mesh names; importing them in a new folder left empty packages and did not change the original bodies. They are retained only as historical authoring evidence.

1. Import the three new `dadg_ws_<ship>_body_no_rudder_v1.fbx` sources as new meshes. Keep original body resources and the three already imported matching rudders. Import meshes ON, convert to unit m, Convert to Z-up OFF; no skeleton, animations or physics import.
2. Verify all six distinct mesh resources and their materials. Independent Blender CLI verification proves the new body exports preserve geometry arrays, world coordinates, UVs, materials, colors and transforms; native import remains unverified. See `authoring/unique-body-import-audit.json`.
3. Only after verification, activate each ship's staged `mesh_holder.xml` and `rudders_holder.xml` together. The body holder changes only the body mesh name; all existing transforms, prefab identities, physics markers and steering controllers are preserved. Keep the built DADG Integration DLL with kogge/carrack custom tags.
4. SDF imports and native baking are a separate operation. Validate interior fit and use the generated SDF resource name before activating `ship_water_clip.xml`.
5. Restart/rescan after loose prefab changes, then verify one rudder per ship, neutral alignment and both steering directions, fire, waves, and save/reload in a sea battle. Runtime checks remain pending.

'''+report[end:]
    report = report.replace('The package includes three `.blend` assemblies, nine FBXs,',
                            'The package retains the original three `.blend` assemblies and nine FBXs as historical evidence, and adds three uniquely named body FBXs with matching `.blend` files,')
    save(package/'REPORT.md', report.encode())
    receipt.append({'install':str(install),'source_folder':str(dest),'fbx_files':sorted(expected),
                    'live_body_prefabs_unchanged':live_hashes,'native_import_verified':False})
jsave(OUT/'deployment-verification.json', receipt)
print('Verified six current FBX sources in each install; live body/rudder references and compiled packages untouched.')
