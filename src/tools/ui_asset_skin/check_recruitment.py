"""Check the recruitment pilot against installed native XML and packed pixels."""
import hashlib
import json
import re
from pathlib import Path
import xml.etree.ElementTree as ET

from PIL import Image

root = Path(__file__).resolve().parents[2]
staging = root / '.codex-work/ui-asset-skin/build/DadgRecruitment'
native = root / '../../SandBox/GUI/Prefabs/Recruitment'
code = (root / 'DellarteDellaGuerra.Integration/UI/Recruitment/RecruitmentSkinPrefabExtensions.cs').read_text()
patches = re.findall(r'\[PrefabExtension\("([^"]+)", "([^"]+)"\)\].*?Attributes => new\(\)(.*?)\n}', code, re.S)
assert len(patches) == 11
changes = []
patched_documents = {}
for prefab, xpath, body in patches:
    xpath = xpath.replace('\\\\', '\\').replace('descendant::', './/')
    document = patched_documents.setdefault(prefab, ET.parse(native / f'{prefab}.xml'))
    targets = document.findall(xpath)
    assert len(targets) == 1, (prefab, xpath, len(targets))
    attributes = re.findall(r'new Attribute\("([^"]+)", "([^"]+)"\)', body)
    assert attributes
    for name, value in attributes:
        assert name in ('Sprite', 'Brush', 'Brush.FontColor', 'SaturationFactor', 'MarginTop',
                        'PartyBackgroundBrush', 'CharacterBackgroundBrush')
        if name == 'MarginTop':
            assert targets[0].get(name) == '75' and value == '70'
        targets[0].set(name, value.replace('\\\\', '\\'))
    changes.append({'prefab': prefab, 'xpath': xpath, 'attributes': attributes})

for document in patched_documents.values():
    for element in document.iter():
        assert not any(value.startswith('GameMenu.Tableau.Background.') for value in element.attrib.values())

data = ET.parse(staging / 'GUI/DadgRecruitmentSpriteData.xml')
assert data.find('.//AlwaysLoad') is not None
assert data.findtext('.//SpriteSheetCount') == '1'
atlas = Image.open(staging / 'AssetSources/GauntletUI/ui_dadg_recruitment_1.png').convert('RGBA')
parts = {}
for part in data.findall('.//SpritePart'):
    name = part.findtext('Name')
    x, y, w, h = [int(part.findtext(n)) for n in ('SheetX', 'SheetY', 'Width', 'Height')]
    image = Image.open(staging / 'GUI/SpriteParts/ui_dadg_recruitment' / (name.replace('\\', '/') + '.png')).convert('RGBA')
    assert image.size == (w, h)
    assert image.tobytes() == atlas.crop((x, y, x + w, y + h)).tobytes(), name
    parts[name] = (w, h)
assert len(parts) == 7
brushes = ET.parse(root / 'tools/ui_asset_skin/DADGRecruitment.xml')
for layer in brushes.findall('.//BrushLayer[@Sprite]'):
    assert layer.get('Sprite') in parts or layer.get('Sprite') == 'BlankWhiteSquare_9'
highlight = brushes.find("./Brush[@Name='DADG.Recruitment.Highlight']")
for state in ('Default', 'Disabled'):
    assert highlight.find(f"./Styles/Style[@Name='{state}']/BrushLayer").get('Color') == '#FFFFFF00'
for state in ('Hovered', 'Pressed', 'Selected'):
    color = highlight.find(f"./Styles/Style[@Name='{state}']/BrushLayer").get('Color')
    assert color.startswith('#FFFFFF') and int(color[-2:], 16) > 0
# A complete source border is necessary, but does not prove runtime mip/scale visibility.
for name in ('troop', 'cart', 'owner'):
    card = Image.open(staging / f'GUI/SpriteParts/ui_dadg_recruitment/DADG/Recruitment/{name}.png').convert('RGBA')
    for inset, color in ((0, (209, 154, 50, 255)), (1, (245, 214, 117, 255))):
        assert all(card.getpixel((x, y)) == color for x in range(inset, card.width - inset)
                   for y in (inset, card.height - 1 - inset)), name
        assert all(card.getpixel((x, y)) == color for x in (inset, card.width - 1 - inset)
                   for y in range(inset, card.height - inset)), name
assert parts['DADG\\Recruitment\\panel'] == (1500, 960)
assert parts['DADG\\Recruitment\\troop'] == (124, 91)
assert parts['DADG\\Recruitment\\cart'] == (172, 81)
assert parts['DADG\\Recruitment\\owner'] == (134, 99)
for name, size in (('left', (1122, 780)), ('right', (248, 780))):
    assert parts[f'DADG\\Recruitment\\{name}'] == size
    panel = Image.open(staging / f'GUI/SpriteParts/ui_dadg_recruitment/DADG/Recruitment/{name}.png').convert('RGBA')
    w, h = size
    edges = [ [(w // 2, i) for i in range(4)], [(w // 2, h - 1 - i) for i in range(4)],
              [(i, h // 2) for i in range(4)], [(w - 1 - i, h // 2) for i in range(4)] ]
    for edge in edges:
        assert any(r >= 220 and g >= 160 and r > g > b and a == 255
                   for r, g, b, a in (panel.getpixel(point) for point in edge)), (name, edge)
title = Image.open(staging / 'GUI/SpriteParts/ui_dadg_recruitment/DADG/Recruitment/title.png').convert('RGBA')
assert title.getchannel('A').getextrema() == (0, 255), 'Title needs transparent margins and visible art'
baseline = json.loads((root / '.codex-work/ui-asset-skin/review/recruitment-contract.json').read_text())
for path, digest in baseline['sources'].items():
    assert hashlib.sha256(Path(path).read_bytes()).hexdigest() == digest, path
print(json.dumps({'patch_targets': changes, 'packed_sprites': parts, 'atlas': atlas.size,
                  'native_source_hashes': 'unchanged', 'runtime_verified': False}, indent=2))
