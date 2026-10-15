"""Character creation name fields reuse the approved recruitment gold bands."""
import json
from pathlib import Path
import unittest
import xml.etree.ElementTree as ET

from PIL import Image

from build_shared_surfaces import paint


HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]
MODULES = HERE.parents[3]


class CreationNameSurfaceTests(unittest.TestCase):
    def test_name_field_has_approved_gold_bands_and_unchanged_navy_interior(self):
        recipe = json.loads((HERE / 'character-creation-group1-1.json').read_text())
        self.assertEqual(len(recipe['parts']), 1)
        entry = recipe['parts'][0]
        self.assertEqual(entry['name'], 'General\\CharacterCreation\\name_input_area')
        part = next(part for part in ET.parse(MODULES / 'Native/GUI/NativeSpriteData.xml')
                    .findall('./SpriteParts/SpritePart') if part.findtext('Name') == entry['name'])
        x, y, w, h = [int(part.findtext(key)) for key in ('SheetX', 'SheetY', 'Width', 'Height')]
        with Image.open(ROOT / '.codex-work/ui-migration/compiled/Native/ui_group1_1.png') as atlas:
            native = atlas.convert('RGBA').crop((x, y, x+w, y+h))
        approved = MODULES / 'DellarteDellaGuerraMap/GUI/DadgRecruitmentSpriteParts/ui_dadg_recruitment/DADG/Recruitment'
        with Image.open(approved / 'panel.png') as source:
            panel = source.convert('RGBA')
        with Image.open(approved / 'left.png') as source:
            inner = source.convert('RGBA')
        output = paint(native, entry, panel, inner)
        previous = paint(native, dict(entry, border_color='#D8AB45FF'), panel, inner)
        self.assertEqual(output.size, (397, 32))
        self.assertEqual(output.getchannel('A').tobytes(), native.getchannel('A').tobytes())
        x1, y1, x2, y2 = native.getchannel('A').point(lambda a: 255 if a >= 240 else 0).getbbox()
        for band in range(entry['border']):
            expected = inner.getpixel((inner.width//2, min(band+1, 3)))[:3]
            for point in ((w//2, y1+band), (w//2, y2-band-1),
                          (x1+band, h//2), (x2-band-1, h//2)):
                self.assertEqual(output.getpixel(point)[:3], expected)
            self.assertGreater(sum(expected[:2]), 216+171)
        self.assertEqual(output.crop((x1+2, y1+2, x2-2, y2-2)).tobytes(),
                         previous.crop((x1+2, y1+2, x2-2, y2-2)).tobytes())


if __name__ == '__main__':
    unittest.main()
