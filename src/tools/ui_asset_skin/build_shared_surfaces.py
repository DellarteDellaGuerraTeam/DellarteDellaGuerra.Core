"""Reuse approved recruitment PNGs in selected native atlas rectangles, without HTML.

No layout/registration/compiled-file writes. Outputs are isolated for inspection
and editor import; all unselected pixels and each native alpha mask are checked.
"""
import argparse
import hashlib
import json
from pathlib import Path
import xml.etree.ElementTree as ET

from PIL import Image, ImageChops, ImageDraw, ImageFilter, ImageOps


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def check_selection(recipe):
    names = [entry['name'] for entry in recipe['parts']]
    if not names or len(names) != len(set(names)):
        raise ValueError('Batch must select unique sprite names')
    protected = set(recipe.get('protected', [])) & set(names)
    if protected:
        raise ValueError(f'Batch selects protected sprites: {sorted(protected)}')


def nine_slice(image, size, edge=12):
    """Preserve approved gold-band thickness rather than stretch the frame."""
    w, h = size
    if min(w, h, image.width, image.height) <= 2 * edge:
        raise ValueError('Panel too small for the approved frame')
    result = Image.new('RGBA', size)
    sx, sy = [0, edge, image.width-edge, image.width], [0, edge, image.height-edge, image.height]
    dx, dy = [0, edge, w-edge, w], [0, edge, h-edge, h]
    for y in range(3):
        for x in range(3):
            piece = image.crop((sx[x], sy[y], sx[x+1], sy[y+1]))
            piece = piece.resize((dx[x+1]-dx[x], dy[y+1]-dy[y]), Image.Resampling.LANCZOS)
            result.paste(piece, (dx[x], dy[y]))
    return result


def paint(native, entry, panel, inner):
    # The PNGs are the already approved code-rendered art, not newly generated art.
    surface = inner.crop((16, 16, inner.width-16, inner.height-16))
    if entry['profile'] == 'inner_surface':
        result = surface.resize(native.size, Image.Resampling.LANCZOS)
    elif entry['profile'] == 'framed_surface':
        result = nine_slice(inner, native.size)
    elif entry['profile'] in ('row', 'header', 'recolor_blue', 'recolor_gold'):
        # Retain native relief/shading without retaining its brown pigment.
        grey = ImageOps.grayscale(native)
        if entry['profile'] == 'recolor_gold':
            result = ImageOps.colorize(grey, '#603907', '#fff2a2').convert('RGBA')
        else:
            bright = entry.get('selected', False) or entry['profile'] == 'header'
            shadow, highlight = entry.get('blue_palette', ['#07172a', '#2870a9' if bright else '#174467'])
            result = ImageOps.colorize(grey, shadow, highlight).convert('RGBA')
        if entry['profile'] in ('row', 'header'):
            # Draw within the opaque silhouette, not the native shadow padding.
            box = native.getchannel('A').point(lambda a: 255 if a >= 240 else 0).getbbox()
            if box is None:
                raise ValueError('Row has no opaque frame region')
            x1, y1, x2, y2 = box
            draw = ImageDraw.Draw(result)
            border = entry.get('border', 3 if entry.get('selected') else 2)
            if border < 1 or border * 2 >= min(x2-x1, y2-y1):
                raise ValueError('Invalid row border')
            border_style = entry.get('border_style', 'frame')
            if border_style not in ('frame', 'silhouette', 'underline'):
                raise ValueError('Invalid row border style')
            for i in range(border):
                color = entry.get('border_color', inner.getpixel((inner.width//2, min(i+1, 3))))
                if border_style == 'underline':
                    draw.line((x1, y2-i-1, x2-1, y2-i-1), fill=color)
                else:
                    draw.rectangle((x1+i, y1+i, x2-i-1, y2-i-1), outline=color)
            if border_style == 'silhouette':
                # Large headers can have a curved alpha edge inside their rectangular
                # sprite bounds. Add matching inner rings so that edge receives the
                # same gold treatment as the straight frame without changing alpha.
                threshold = entry.get('alpha_border_threshold', 128)
                opaque = native.getchannel('A').point(lambda a: 255 if a >= threshold else 0)
                previous = opaque
                for i in range(border):
                    eroded = opaque.filter(ImageFilter.MinFilter(2 * (i + 1) + 1))
                    ring = ImageChops.subtract(previous, eroded)
                    color = entry.get('border_color', inner.getpixel((inner.width//2, min(i+1, 3))))
                    result.paste(Image.new('RGBA', result.size, color), (0, 0), ring)
                    previous = eroded
                native_contour = entry.get('native_contour')
                if native_contour:
                    cx1, cy1, cx2, cy2 = native_contour['box']
                    if not (0 <= cx1 < cx2 <= native.width and 0 <= cy1 < cy2 <= native.height):
                        raise ValueError('Native contour box outside original sprite')
                    contour_threshold = native_contour.get('luminance_threshold', 128)
                    if not 0 <= contour_threshold <= 255:
                        raise ValueError('Invalid native contour luminance threshold')
                    contour_mask = grey.point(
                        lambda value: 255 if value >= contour_threshold else 0)
                    max_depth = native_contour.get('max_depth')
                    if max_depth is not None:
                        if max_depth < 1 or max_depth * 2 >= min(native.size):
                            raise ValueError('Invalid native contour maximum depth')
                        eroded = opaque.filter(ImageFilter.MinFilter(2 * max_depth + 1))
                        contour_mask = ImageChops.multiply(
                            contour_mask, ImageChops.subtract(opaque, eroded))
                    clip = Image.new('L', native.size, 0)
                    ImageDraw.Draw(clip).rectangle((cx1, cy1, cx2-1, cy2-1), fill=255)
                    contour_mask = ImageChops.multiply(contour_mask, clip)
                    contour_mask = ImageChops.multiply(contour_mask, native.getchannel('A'))
                    gold = ImageOps.colorize(grey, '#603907', '#fff2a2').convert('RGBA')
                    result.paste(gold, (0, 0), contour_mask)
    elif entry['profile'] == 'character_panel':
        outer = panel.crop((16, 16, panel.width-16, panel.height-16))
        result = outer.resize(native.size, Image.Resampling.LANCZOS)
        x1, y1, x2, y2 = entry['inner_box']
        if not (0 <= x1 < x2 <= native.width and 0 <= y1 < y2 <= native.height):
            raise ValueError('Inner panel outside original sprite')
        result.paste(nine_slice(inner, (x2-x1, y2-y1)), (x1, y1))
    else:
        raise ValueError(entry['profile'])
    result.putalpha(native.getchannel('A'))
    assert result.size == native.size
    assert result.getchannel('A').tobytes() == native.getchannel('A').tobytes()
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--modules', type=Path, default=Path('../..'))
    parser.add_argument('--native-exports', type=Path, default=Path('.codex-work/ui-migration/compiled'))
    parser.add_argument('--output', type=Path, default=Path('.codex-work/ui-asset-skin/shared-surfaces-01'))
    parser.add_argument('--recipe', type=Path, default=Path(__file__).with_name('shared-surfaces.json'))
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[2]
    output = args.output.resolve()
    if not output.is_relative_to(root / '.codex-work'):
        raise ValueError('Batch output must stay in the workspace .codex-work directory')
    recipe = json.loads(args.recipe.read_text())
    check_selection(recipe)
    modules = args.modules.resolve()
    map_root = modules / 'DellarteDellaGuerraMap'
    xml = modules / recipe['module'] / 'GUI' / recipe.get('sprite_data', recipe['module'] + 'SpriteData.xml')
    contract_hash = sha(xml)
    atlas_path = map_root / 'AssetSources/GauntletUI' / (recipe['sheet'] + '.png')
    native_path = args.native_exports / recipe['module'] / atlas_path.name
    if contract_hash != recipe['native_sprite_data_sha256'] or sha(native_path) != recipe['native_atlas_sha256']:
        raise ValueError('Native sprite contract/reference changed; re-audit this batch before rebuilding')
    source_hash = sha(atlas_path) if atlas_path.is_file() else None
    approved = map_root / 'GUI/DadgRecruitmentSpriteParts/ui_dadg_recruitment/DADG/Recruitment'
    panel_path, inner_path = approved / 'panel.png', approved / 'left.png'
    expected = {panel_path: 'bde5a21c4f3573e26a87dd41b08199b5038993c9965739d1feb2b588bda1af5d',
                inner_path: 'a08d2718e6083082717e5ccc16edb64d24d3ecb80bb10e1f2d9484e8857c78ed'}
    for path, digest in expected.items():
        if sha(path) != digest:
            raise ValueError(f'Approved recruitment source changed: {path}')
    panel, inner = Image.open(panel_path).convert('RGBA'), Image.open(inner_path).convert('RGBA')
    original = Image.open(atlas_path if source_hash else native_path).convert('RGBA')
    native_atlas = Image.open(native_path).convert('RGBA')
    assert original.size == native_atlas.size == tuple(recipe.get('size', [4096, 4096]))
    result = original.copy()
    allowed = Image.new('L', original.size, 0)
    mask = ImageDraw.Draw(allowed)
    data = ET.parse(xml)
    parts = {p.findtext('Name'): p for p in data.findall('./SpriteParts/SpritePart')}
    records = []
    snapshots = []
    output.mkdir(parents=True, exist_ok=True)
    for entry in recipe['parts']:
        part = parts[entry['name']]
        assert part.findtext('CategoryName') + '_' + part.findtext('SheetID') == recipe['sheet']
        x, y, w, h = [int(part.findtext(k)) for k in ('SheetX', 'SheetY', 'Width', 'Height')]
        assert 0 <= x < x+w <= original.width and 0 <= y < y+h <= original.height
        box = (x, y, x+w, y+h)
        assert allowed.crop(box).getbbox() is None, 'Overlapping batch entries'
        native = native_atlas.crop(box)
        replacement = paint(native, entry, panel, inner)
        result.paste(replacement, (x, y))
        mask.rectangle((x, y, x+w-1, y+h-1), fill=255)
        loose_relative = Path('GUI/DadgMainUISpriteParts/DADG_customui2') / (entry['name'].replace('\\', '/') + '.png')
        loose = map_root / loose_relative
        record = dict(entry, rectangle=[x, y, w, h], native_alpha_sha256=hashlib.sha256(native.getchannel('A').tobytes()).hexdigest())
        if loose.is_file():
            assert Image.open(loose).size == (w, h)
            target = output / loose_relative
            target.parent.mkdir(parents=True, exist_ok=True)
            replacement.save(target)
            record.update(loose_path=str(loose_relative), original_loose_sha256=sha(loose), output_loose_sha256=sha(target))
        snapshots.append((entry['name'], original.crop(box), replacement))
        records.append(record)
    # Compare all RGBA channels, including RGB beneath transparent pixels.
    delta = ImageChops.difference(original, result)
    for channel in delta.split():
        assert ImageChops.multiply(channel, ImageChops.invert(allowed)).getbbox() is None
    for name, part in parts.items():
        if name in {e['name'] for e in recipe['parts']}:
            continue
        if part.findtext('CategoryName') + '_' + part.findtext('SheetID') != recipe['sheet']:
            continue
        x, y, w, h = [int(part.findtext(k)) for k in ('SheetX', 'SheetY', 'Width', 'Height')]
        assert original.crop((x, y, x+w, y+h)).tobytes() == result.crop((x, y, x+w, y+h)).tobytes(), name
    target_atlas = output / 'AssetSources/GauntletUI' / atlas_path.name
    target_atlas.parent.mkdir(parents=True, exist_ok=True)
    result.save(target_atlas)
    assert Image.open(target_atlas).convert('RGBA').tobytes() == result.tobytes()
    assert sha(xml) == contract_hash
    assert (sha(atlas_path) if atlas_path.exists() else None) == source_hash
    consumers = []
    for module in ('Native', 'SandBox', 'SandBoxCore', 'NavalDLC'):
        gui = modules / module / 'GUI'
        for folder in ('Prefabs', 'Brushes'):
            for path in sorted((gui / folder).rglob('*.xml')):
                content = path.read_text(encoding='utf-8-sig')
                for entry in recipe['parts']:
                    if entry['name'] in content:
                        consumers.append({'sprite': entry['name'], 'path': str(path.relative_to(modules)), 'sha256': sha(path)})
    # PNG evidence only: original marked atlas crop next to actual output pixels.
    contact = Image.new('RGB', (1000, 420 * len(snapshots)), '#202733')
    draw = ImageDraw.Draw(contact)
    for row, (name, before, after) in enumerate(snapshots):
        for col, (label, sprite) in enumerate((('before', before), ('after', after))):
            sprite.thumbnail((470, 375))
            contact.paste(sprite, (col*500+10, row*420+35), sprite)
            draw.text((col*500+10, row*420+8), name + ' ' + label, fill='white')
    contact.save(output / 'source-comparison.png')
    # Keep larger batches inspectable at normal resolution instead of shrinking
    # a many-thousand-pixel contact sheet into one unreadable preview.
    for start in range(0, len(snapshots), 5):
        page = contact.crop((0, start*420, 1000, min(start+5, len(snapshots))*420))
        page.save(output / f'source-comparison-{start//5+1:02d}.png')
    overrides = []
    for override in recipe.get('brush_overrides', []):
        source = args.recipe.parent / override['source']
        native_brush = modules / override['native']
        native_names = {b.get('Name') for b in ET.parse(native_brush).findall('Brush')}
        brush_names = [b.get('Name') for b in ET.parse(source).findall('Brush')]
        assert brush_names and len(brush_names) == len(set(brush_names))
        assert set(brush_names) <= native_names
        relative = Path('GUI/Brushes') / source.name
        destination = map_root / relative
        staged = output / relative
        staged.parent.mkdir(parents=True, exist_ok=True)
        staged.write_bytes(source.read_bytes())
        overrides.append(dict(relative=str(relative), before=sha(destination) if destination.exists() else None,
                              after=sha(staged), brushes=brush_names))
        consumers.append({'path': str(native_brush.relative_to(modules)), 'sha256': sha(native_brush)})
    report = dict(batch=recipe['batch'], atlas_relative=str(atlas_path.relative_to(map_root)), source_atlas=str(atlas_path), source_sha256=source_hash,
                  output_atlas=str(target_atlas), output_sha256=sha(target_atlas), native_reference_sha256=sha(native_path),
                  sprite_data=str(xml), sprite_data_sha256=contract_hash, parts=records, consumers=consumers, brush_overrides=overrides,
                  checks={'native_alpha_preserved': True, 'unselected_pixels_identical': True, 'layout_and_registration_unchanged': True},
                  deployed=False, runtime_verified=False, editor_reimport=recipe['sheet'] + '_tex')
    (output / 'report.json').write_text(json.dumps(report, indent=2) + '\n')
    print(json.dumps({'output': str(output), 'parts': len(records), 'direct_xml_consumers': len(consumers), 'checks': report['checks']}, indent=2))


if __name__ == '__main__':
    main()
