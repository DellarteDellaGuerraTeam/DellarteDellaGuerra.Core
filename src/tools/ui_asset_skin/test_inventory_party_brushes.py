"""Contract checks for the PNG-only inventory/party header and troop-row pass."""
import json
from pathlib import Path
import unittest
import xml.etree.ElementTree as ET

from PIL import Image, ImageStat

from build_shared_surfaces import paint


ROOT = Path(__file__).resolve().parents[2]
MODULES = ROOT.parent.parent
RECIPES = Path(__file__).parent


class InventoryPartySurfaceTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.inventory = json.loads((RECIPES / 'inventory-surfaces.json').read_text())
        cls.party = json.loads((RECIPES / 'party-surfaces.json').read_text())
        approved = MODULES / 'DellarteDellaGuerraMap/GUI/DadgRecruitmentSpriteParts/ui_dadg_recruitment/DADG/Recruitment'
        cls.panel = Image.open(approved / 'panel.png').convert('RGBA')
        cls.inner = Image.open(approved / 'left.png').convert('RGBA')
        cls.parts = {p.findtext('Name'): p for p in ET.parse(MODULES / 'SandBox/GUI/SandBoxSpriteData.xml').findall('./SpriteParts/SpritePart')}

    def test_inventory_warning_rows_are_out_of_scope(self):
        names = {p['name'] for p in self.inventory['parts']}
        self.assertEqual(names, {'Inventory\\header_left', 'Inventory\\header_right'})
        self.assertFalse(self.inventory.get('brush_overrides'))
        for name in ('Inventory\\tuple_left', 'Inventory\\tuple_left_pressed',
                     'Inventory\\tuple_right', 'Inventory\\tuple_right_pressed'):
            self.assertIn(name, self.inventory['protected'])

    def test_selected_brush_families_have_no_pigment_tints(self):
        # If native consumers acquire a tint, the PNG-only assumption needs review.
        for filename, recipe in [('Inventory.xml', self.inventory), ('PartyScreen.xml', self.party)]:
            names = {p['name'] for p in recipe['parts']}
            consumers = []
            for brush in ET.parse(MODULES / 'SandBox/GUI/Brushes' / filename).findall('Brush'):
                if any(layer.get('Sprite') in names for layer in brush.iter('BrushLayer')):
                    consumers.append(brush)
                    for layer in brush.iter('BrushLayer'):
                        for key in ('Color', 'HueFactor', 'SaturationFactor'):
                            self.assertNotIn(key, layer.attrib, (brush.get('Name'), key))
            self.assertEqual(len(consumers), 2 if filename == 'Inventory.xml' else 6)
        self.assertFalse(self.party.get('brush_overrides'))

    def test_party_states_remain_distinct_with_native_alpha(self):
        atlas = Image.open(ROOT / '.codex-work/ui-migration/compiled/SandBox/ui_partyscreen_1.png').convert('RGBA')
        rendered = {}
        for entry in self.party['parts']:
            part = self.parts[entry['name']]
            x, y, w, h = [int(part.findtext(k)) for k in ('SheetX', 'SheetY', 'Width', 'Height')]
            native = atlas.crop((x, y, x+w, y+h))
            result = paint(native, entry, self.panel, self.inner)
            self.assertEqual(result.size, native.size)
            self.assertEqual(result.getchannel('A').tobytes(), native.getchannel('A').tobytes())
            rendered[entry['name']] = result
        for stem, suffixes in [('button_left', ['', '_hover', '_pressed', '_selected']),
                               ('button_right', ['', '_hover', '_pressed', '_selected']),
                               ('button_collapser', ['', '_hover', '_pressed'])]:
            variants = [rendered['PartyScreen\\' + stem + s] for s in suffixes]
            self.assertEqual(len({im.tobytes() for im in variants}), len(variants), stem)
            center_light = [ImageStat.Stat(im.crop((im.width//3, im.height//3, im.width*2//3, im.height*2//3)).convert('L')).mean[0] for im in variants]
            self.assertGreater(center_light[1], center_light[0], stem + ' hover')
        selected = [p for p in self.party['parts'] if p['name'].endswith('_selected')]
        self.assertTrue(all(p.get('selected') for p in selected))
        self.assertEqual(len(selected), 2)


if __name__ == '__main__':
    unittest.main()
