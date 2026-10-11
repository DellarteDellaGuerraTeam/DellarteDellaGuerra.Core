"""Validate the review scope and extract unmodified crops for an offline review board."""
import argparse
from collections import Counter
import hashlib
from html import escape
import json
from pathlib import Path
import shutil

from PIL import Image


def checksum(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def build_preview(inventory, output, map_gui):
    """Extract exact source rectangles for a browser-only composition, not a game atlas."""
    directory = output / 'preview'
    directory.mkdir(exist_ok=True)
    extracted = []

    def crop(source, rectangle, filename):
        x, y, width, height = rectangle
        with Image.open(source['path']) as atlas:
            if not (0 <= x < x + width <= atlas.width and 0 <= y < y + height <= atlas.height):
                raise ValueError(f'Invalid preview crop: {filename}')
            atlas.crop((x, y, x + width, y + height)).save(directory / filename)
        extracted.append(dict(file='preview/' + filename, source=source['path'],
                              source_sha256=source['sha256'], rectangle=rectangle))

    sheets = {s['sheet']: s for s in inventory['sheets']}
    registered = {s['name']: s for s in inventory['dadg_source_index'] if s['kind'] == 'registered_atlas'}
    pilot = set(inventory['scope']['pilot'])
    assert 'ui_mapbar_1' in pilot
    selected = {'army_button_background', 'mapbar_center_circle_canvas', 'mapbar_center_circle_frame',
                'mapbar_center_frame', 'mapbar_left_frame', 'mapbar_right_frame'}
    for part in sheets['ui_mapbar_1']['parts']:
        suffix = part['name'].split('\\')[-1]
        crop(sheets['ui_mapbar_1']['native_reference'], part['rectangle'], suffix + '-native.png')
        if suffix in selected:
            source = registered[part['name']]
            if source['rectangle'][2:] != part['rectangle'][2:]:
                raise ValueError(f'Preview selection unexpectedly changes dimensions: {suffix}')
            crop(source, source['rectangle'], suffix + '-dadg.png')
    panel = next(p for p in sheets['ui_group1_1']['parts'] if p['name'] == 'StdAssets\\standart_popup')
    for kind, suffix in (('native_reference', 'native'), ('existing_dadg', 'dadg')):
        crop(sheets['ui_group1_1'][kind], panel['rectangle'], 'panel-' + suffix + '.png')
    gold = registered['SPGeneral\\TownManagement\\gold_frame']
    crop(gold, gold['rectangle'], 'gold-frame-dadg.png')
    native_gold = next((s, p) for s in sheets.values() for p in s['parts']
                       if p['name'] == 'SPGeneral\\TownManagement\\gold_frame')
    crop(native_gold[0]['native_reference'], native_gold[1]['rectangle'], 'gold-frame-native.png')
    recruitment = {
        'recruit-left': 'SPGeneral\\SPRecruitment\\left_dent',
        'recruit-right': 'SPGeneral\\SPRecruitment\\right_dent',
        'recruit-title': 'StdAssets\\tabbar_popup',
        'recruit-frame': 'SPGeneral\\Frame1\\frame',
    }
    for filename, name in recruitment.items():
        sheet, part = next((s, p) for s in sheets.values() for p in s['parts'] if p['name'] == name)
        crop(sheet['native_reference'], part['rectangle'], filename + '-native.png')
    modules = map_gui.resolve().parent.parent
    references = ['SandBox/GUI/Prefabs/Recruitment/RecruitmentPopup.xml',
                  'SandBox/GUI/Brushes/Recruitment.xml', 'Native/GUI/NativeSpriteData.xml']
    contract = dict(screen='Recruit Troops', frame_size=[1500, 960],
                    left_panel=[55, 85, 1122, 780], right_panel=[1191.5, 90, 248, 780],
                    dadg_preview_right_panel=[1191.5, 85, 248, 780],
                    native_frame_slice=[29, 28, 30, 28], dadg_css_edge=6,
                    sources={str(modules / p): checksum(modules / p) for p in references},
                    limitations=['Offline geometry study, not a Gauntlet renderer.',
                                 'DADG preview raises the right panel 5px; native layout is unchanged on disk.',
                                 'Rows and footer contents are placeholders; no game bindings.',
                                 'Native saturation and stone overlay are not emulated.',
                                 'Gold edge is browser-only CSS/SVG; no deployable frame asset created.'])
    (output / 'recruitment-contract.json').write_text(json.dumps(contract, indent=2), encoding='utf-8')
    banners = [s for s in registered.values() if s['category'] == 'ui_dadg_group1']
    banner_cards = []
    for index, source in enumerate(banners):
        filename = f'protected-banner-{index}.png'
        crop(source, source['rectangle'], filename)
        banner_cards.append(f'<figure><img src="preview/{filename}" alt="{escape(source["name"])}">'
                            f'<figcaption>{escape(source["name"])}</figcaption></figure>')
    (output / 'preview-sources.json').write_text(json.dumps(extracted, indent=2), encoding='utf-8')
    template = Path(__file__).with_name('preview.html').read_text(encoding='utf-8')
    return template.replace('<!-- BANNER_CARDS -->', ''.join(banner_cards))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--inventory', type=Path, required=True)
    parser.add_argument('--map-gui', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    # Derived evidence belongs in local staging, never in a live module.
    staging = Path.cwd().resolve() / '.codex-work'
    if not args.output.resolve().is_relative_to(staging):
        raise ValueError('Review output must be inside the workspace .codex-work directory')
    inventory = json.loads(args.inventory.read_text(encoding='utf-8'))
    review = json.loads(Path(__file__).with_name('review.json').read_text(encoding='utf-8'))
    sheets = {sheet['sheet']: sheet for sheet in inventory['sheets']}
    parts = {name: {part['name']: part['rectangle'] for part in sheet['parts']}
             for name, sheet in sheets.items()}
    alternatives = {(sheet['sheet'], part['name']): part.get('reuse_candidates', [])
                    for sheet in sheets.values() for part in sheet['parts']}
    for path, expected in {s['path']: s['sha256'] for s in inventory.get('dadg_source_index', [])}.items():
        if checksum(Path(path)) != expected:
            raise ValueError(f'Reuse source changed since inventory: {path}')
    for sheet in sheets.values():
        for kind in ('native_reference', 'existing_dadg'):
            source = sheet[kind]
            if source and checksum(Path(source['path'])) != source['sha256']:
                raise ValueError(f'Source changed since inventory: {source["path"]}')
    if set(review['sheet_reviews']) != {name for name, sheet in sheets.items() if sheet['existing_dadg']}:
        raise ValueError('Every existing DADG source sheet needs an overview decision')
    rows = {}
    for group in review['region_groups']:
        for name in group['parts']:
            key = (group['sheet'], name)
            if key in rows:
                raise ValueError(f'Duplicate review decision: {key}')
            rows[key] = {key: value for key, value in group.items() if key != 'parts'}
            rows[key].update(name=name, rectangle=parts[group['sheet']][name], pilot=False)
    protected = {}
    for sheet, names in review['pilot'].items():
        if len(names) != len(set(names)):
            raise ValueError(f'Duplicate pilot region: {sheet}')
        for name in names:
            key = (sheet, name)
            if key not in rows:
                rows[key] = dict(sheet=sheet, name=name, rectangle=parts[sheet][name],
                                 decision='pilot_surface', note='Review existing DADG sources first; this is not a request to paint a new surface.')
            rows[key]['pilot'] = True
        protected[sheet] = sorted(set(parts[sheet]) - set(names))
    gui_files = sorted(args.map_gui.rglob('*.xml'))
    if not gui_files:
        raise ValueError('Map GUI directory is missing or empty')
    gui_hashes = {str(path.relative_to(args.map_gui)): checksum(path) for path in gui_files}
    args.output.mkdir(parents=True, exist_ok=True)
    crop_dir = args.output / 'crops'
    crop_dir.mkdir(exist_ok=True)
    preview = build_preview(inventory, args.output, args.map_gui)
    cards = []
    for index, row in enumerate(rows.values()):
        x, y, width, height = row['rectangle']
        images = []
        for kind, label in (('native_reference', 'Native reference'), ('existing_dadg', 'Existing DADG')):
            source = sheets[row['sheet']][kind]
            if not source:
                continue
            name = f'{index:02d}-{kind}.png'
            with Image.open(source['path']) as atlas:
                atlas.crop((x, y, x + width, y + height)).save(crop_dir / name)
            images.append(f'<figure><figcaption>{label}</figcaption><img src="crops/{name}" alt="{escape(row["name"])}"></figure>')
        row['reuse_candidates'] = alternatives[(row['sheet'], row['name'])]
        for candidate_index, source in enumerate(row['reuse_candidates']):
            name = f'{index:02d}-reuse-{candidate_index:02d}.png'
            with Image.open(source['path']) as image:
                if 'rectangle' in source:
                    sx, sy, sw, sh = source['rectangle']
                    image = image.crop((sx, sy, sx + sw, sy + sh))
                image.save(crop_dir / name)
            label = source['kind'] + ': ' + source['category']
            status = ('same native size' if source['matches_native_size'] else 'SIZE DIFFERS')
            status += ' / ' + ('same native pixels' if source['equals_native_pixels'] else 'different artwork')
            status += ' / ' + ('same native alpha' if source['equals_native_alpha'] else 'ALPHA DIFFERS')
            if source['kind'] == 'registered_atlas':
                status += ' / ' + ('TPAC present' if source['package_exists'] else 'TPAC not found')
                status += ' / runtime usage unverified'
            images.append(f'<figure><figcaption>{escape(label)}<br>{escape(status)}</figcaption>'
                          f'<img src="crops/{name}" alt="{escape(row["name"])}">'
                          f'<p class="source">{escape(source["path"])}</p></figure>')
        pilot = ' | PILOT' if row['pilot'] else ''
        cards.append(f'<section><h3>{escape(row["name"])}</h3><p>{row["sheet"]} | {row["decision"]}{pilot}</p>'
                     f'<p>{escape(row["note"])}</p><div class="pair">{"".join(images)}</div></section>')
    overview = ''.join(f'<li><b>{sheet}</b>: {escape(note)}</li>' for sheet, note in review['sheet_reviews'].items())
    constraints = ''.join(f'<li>{escape(note)}</li>' for note in review['pilot_constraints'])
    limitations = ''.join(f'<li>{escape(note)}</li>' for note in review['limitations'])
    findings = ''.join(f'<li>{escape(note)}</li>' for note in review.get('reuse_findings', []))
    references = []
    for index, item in enumerate(review.get('user_reference_baseline', [])):
        filename = f'preview/user-reference-{index}.png'
        shutil.copyfile(item['path'], args.output / filename)
        references.append(f'<figure><img src="{filename}" alt="User DADG reference">'
                          f'<figcaption>{escape(item["note"])}</figcaption></figure>')
    references = ''.join(references)
    html = ('<!doctype html><html lang="en"><meta charset="utf-8"><title>DADG asset-only review</title>'
            '<style>body{background:#131e2a;color:#eee;font:16px system-ui;max-width:1400px;margin:32px auto;padding:0 20px}'
            'h1,h2,h3{color:#d8bc77}section{border-top:1px solid #53606e;padding:20px 0}li{margin:10px 0}'
            '.pair{display:grid;grid-template-columns:repeat(auto-fit,minmax(260px,1fr));gap:20px;align-items:start}'
            'figure{margin:0;min-width:0}.source{font-size:11px;overflow-wrap:anywhere}'
            'img{max-width:100%;height:auto;max-height:520px;object-fit:contain;object-position:left top;background:#333}'
            'figcaption{margin-bottom:8px}</style><h1>DADG asset-only review</h1>'
            '<p>Reuse-first review. No artwork edited or deployed. Native layouts remain unchanged.</p>'
            '<p>' + escape(review['illustration_policy']) + '</p>'
            '<p>Alternatives include loose SpriteParts and registered custom DADG atlases. '
            'A TPAC on disk does not prove its pixels match the source PNG or that the game selects it.</p>'
            + preview + '<details><summary>Source audit and user references</summary>'
            '<h2>User-provided visual baseline</h2>' + references + '<h2>Reuse findings</h2><ul>' + findings + '</ul>'
            '<h2>Existing artwork</h2><ul>' + overview + '</ul><h2>Pilot boundaries</h2><ul>' + constraints +
            '</ul><h2>Limitations</h2><ul>' + limitations + '</ul><h2>Region comparisons</h2>' + ''.join(cards) + '</details></html>')
    (args.output / 'index.html').write_text(html, encoding='utf-8')
    report = dict(review=review, reviewed_regions=list(rows.values()), protected_pilot_parts=protected,
                  map_gui_baseline_sha256=gui_hashes, inventory_sha256=checksum(args.inventory),
                  decision_counts=dict(Counter(row['decision'] for row in rows.values())),
                  pilot_region_count=sum(row['pilot'] for row in rows.values()), deployed=False)
    (args.output / 'review-report.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
    print(json.dumps(dict(regions=len(rows), pilot_regions=report['pilot_region_count'],
                          protected_mapbar_parts=len(protected['ui_mapbar_1']),
                          gui_baseline_files=len(gui_hashes), decisions=report['decision_counts'],
                          output=str(args.output.resolve()), deployed=False), indent=2))


if __name__ == '__main__':
    main()
