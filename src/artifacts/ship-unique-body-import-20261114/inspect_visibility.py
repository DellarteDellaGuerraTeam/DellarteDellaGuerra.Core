import bpy
from io_scene_fbx import parse_fbx
from pathlib import Path
import json

ROOT = Path('D:/SteamLibrary/steamapps/common/Mount & Blade II Bannerlord/Modules/DellarteDellaGuerraMap/Tools/WarSailsShips/visual-fixes-20261114/authoring')
FILES = [
    'dadg_ws_english_boat_b_rudder_separated_v1.fbx',
    'dadg_ws_kogge_body_without_yards_v1_rudder_separated_v1.fbx',
    'dadg_ws_carrack_body_without_yards_v1_rudder_separated_v1.fbx',
    'dadg_ws_english_boat_b_rudder_v1.fbx',
]

def clean(value):
    if isinstance(value, bytes): return value.decode('utf-8', errors='replace')
    return value

for filename in FILES:
    tree, version = parse_fbx.parse(str(ROOT / filename))
    objects = next(n for n in tree.elems if n.id == b'Objects')
    models = []
    for model in objects.elems:
        if model.id != b'Model': continue
        models.append({
            'header': [clean(v) for v in model.props],
            'children': {clean(n.id): [clean(v) for v in n.props] for n in model.elems if n.id != b'Properties70'},
            'properties': [[clean(v) for v in p.props] for n in model.elems if n.id == b'Properties70' for p in n.elems],
        })
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(ROOT / filename))
    print(json.dumps({'file': filename, 'version': version, 'models': models,
                      'objects': [{'name': o.name, 'mesh': o.data.name, 'hide_render': o.hide_render, 'hide_viewport': o.hide_viewport, 'hide_get': o.hide_get()} for o in bpy.data.objects if o.type == 'MESH']}, indent=2), flush=True)
