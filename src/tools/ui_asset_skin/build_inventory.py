"""Record candidate atlas contracts and source hashes without changing the game."""
import argparse
import hashlib
import json
from pathlib import Path
import xml.etree.ElementTree as ET

from PIL import Image


def source_info(path, expected=None):
    if not path.is_file():
        return None
    with Image.open(path) as image:
        actual = list(image.size)
    if expected is not None and actual != expected:
        raise ValueError(f'Unexpected sheet dimensions: {path}: {actual} != {expected}')
    with path.open('rb') as stream:
        checksum = hashlib.file_digest(stream, 'sha256').hexdigest()
    return {'path': str(path.resolve()), 'sha256': checksum, 'size': actual}


def reusable_sources(module, rows):
    """Index loose art and registered atlas regions; presence is not runtime usage."""
    gui = module / 'GUI'
    sources = []
    for folder in sorted(gui.glob('*SpriteParts')):
        for path in sorted(folder.rglob('*.png')):
            relative = path.relative_to(folder)
            if len(relative.parts) < 2:
                continue
            sources.append(dict(source_info(path), name='\\'.join(relative.with_suffix('').parts[1:]),
                                kind='loose_png', folder=folder.name, category=relative.parts[0]))
    atlas_info = {}
    for xml in sorted(gui.glob('*SpriteData.xml')):
        for part in ET.parse(xml).findall('./SpriteParts/SpritePart'):
            category = part.findtext('CategoryName')
            sheet = category + '_' + part.findtext('SheetID')
            path = module / 'AssetSources/GauntletUI' / (sheet + '.png')
            if path not in atlas_info:
                atlas_info[path] = source_info(path)
            if not atlas_info[path]:
                continue
            rect = [int(part.findtext(k)) for k in ('SheetX', 'SheetY', 'Width', 'Height')]
            x, y, width, height = rect
            size = atlas_info[path]['size']
            if min(rect) < 0 or x + width > size[0] or y + height > size[1]:
                raise ValueError(f'Registered DADG sprite outside source atlas: {xml}: {part.findtext("Name")}')
            package = module / 'Assets/GauntletUI' / (sheet + '_tex.tpac')
            sources.append(dict(atlas_info[path], name=part.findtext('Name'), kind='registered_atlas',
                                category=category, rectangle=rect, registration=str(xml.resolve()),
                                package_exists=package.is_file(), runtime_usage='not_verified'))
    by_name = {}
    for source in sources:
        by_name.setdefault(source['name'], []).append(source)
    images = {}
    try:
        for row in rows:
            with Image.open(row['native_reference']['path']) as native:
                for part in row['parts']:
                    x, y, width, height = part['rectangle']
                    reference = native.crop((x, y, x + width, y + height)).convert('RGBA')
                    alternatives = []
                    for source in by_name.get(part['name'], []):
                        path = source['path']
                        if path not in images:
                            with Image.open(path) as image:
                                images[path] = image.convert('RGBA')
                        candidate = images[path]
                        if 'rectangle' in source:
                            sx, sy, sw, sh = source['rectangle']
                            candidate = candidate.crop((sx, sy, sx + sw, sy + sh))
                        same_size = candidate.size == reference.size
                        alternatives.append(dict(source, matches_native_size=same_size,
                            equals_native_pixels=same_size and candidate.tobytes() == reference.tobytes(),
                            equals_native_alpha=same_size and candidate.getchannel('A').tobytes() == reference.getchannel('A').tobytes()))
                    part['reuse_candidates'] = alternatives
    finally:
        for image in images.values():
            image.close()
    return sources


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--modules', type=Path, required=True)
    parser.add_argument('--exports', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    if args.output.resolve().is_relative_to(args.modules.resolve()) and not args.output.resolve().is_relative_to(Path.cwd().resolve()):
        raise ValueError('Inventory output must not be written into a live content module')
    scope = json.loads(Path(__file__).with_name('scope.json').read_text(encoding='utf-8'))
    rows = []
    for module, candidates in scope['candidate_sheets'].items():
        categories, parts = {}, []
        for xml_path in sorted((args.modules / module / 'GUI').glob('*SpriteData.xml')):
            root = ET.parse(xml_path).getroot()
            for category in root.findall('./SpriteCategories/SpriteCategory'):
                for sheet in category.findall('SpriteSheetSize'):
                    name = category.findtext('Name') + '_' + sheet.get('ID')
                    categories[name] = [int(sheet.get('Width')), int(sheet.get('Height'))]
            parts.extend(root.findall('./SpriteParts/SpritePart'))
        for name in candidates:
            size = categories[name]
            category_name, sheet_id = name.rsplit('_', 1)
            rectangles = []
            for part in parts:
                if part.findtext('CategoryName') != category_name or part.findtext('SheetID') != sheet_id:
                    continue
                rect = [int(part.findtext(key)) for key in ('SheetX', 'SheetY', 'Width', 'Height')]
                if min(rect) < 0 or rect[0] + rect[2] > size[0] or rect[1] + rect[3] > size[1]:
                    raise ValueError(f'Sprite outside atlas: {name}: {part.findtext("Name")}')
                rectangles.append({'name': part.findtext('Name'), 'rectangle': rect})
            if not rectangles:
                raise ValueError(f'No sprite parts for {name}')
            rows.append({'module': module, 'sheet': name, 'size': size,
                         'status': 'visual_review_required', 'parts': rectangles,
                         'existing_dadg': source_info(args.modules / 'DellarteDellaGuerraMap/AssetSources/GauntletUI' / (name + '.png'), size),
                         'native_reference': source_info(args.exports / module / (name + '.png'), size)})
    sources = reusable_sources(args.modules / 'DellarteDellaGuerraMap', rows)
    report = {'deployed': False, 'layout_files_changed': 0, 'candidate_count': len(rows),
              'existing_dadg_count': sum(row['existing_dadg'] is not None for row in rows),
              'native_reference_count': sum(row['native_reference'] is not None for row in rows),
              'loose_png_count': sum(s['kind'] == 'loose_png' for s in sources),
              'registered_atlas_region_count': sum(s['kind'] == 'registered_atlas' for s in sources),
              'scope': scope, 'sheets': rows, 'dadg_source_index': sources}
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(report, indent=2), encoding='utf-8')
    print(json.dumps({key: value for key, value in report.items() if key not in ('scope', 'sheets', 'dadg_source_index')}, indent=2))


if __name__ == '__main__':
    main()
