"""Contract checks for the inventory/party surface pass."""
import copy
import json
from pathlib import Path
import unittest
import xml.etree.ElementTree as ET

from PIL import Image, ImageStat

from build_shared_surfaces import paint


ROOT = Path(__file__).resolve().parents[2]
MODULES = ROOT.parent.parent
RECIPES = Path(__file__).parent


def tree(element):
    return (element.tag, dict(element.attrib), (element.text or '').strip(),
            tuple(tree(child) for child in element))


class InventoryPartySurfaceTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.inventory = json.loads((RECIPES / 'inventory-surfaces.json').read_text())
        cls.party = json.loads((RECIPES / 'party-surfaces.json').read_text())
        approved = MODULES / 'DellarteDellaGuerraMap/GUI/DadgRecruitmentSpriteParts/ui_dadg_recruitment/DADG/Recruitment'
        cls.panel = Image.open(approved / 'panel.png').convert('RGBA')
        cls.inner = Image.open(approved / 'left.png').convert('RGBA')
        cls.parts = {p.findtext('Name'): p for p in ET.parse(MODULES / 'SandBox/GUI/SandBoxSpriteData.xml').findall('./SpriteParts/SpritePart')}

    def test_inventory_scope_and_tuple_brush_override_are_exact(self):
        names = {p['name'] for p in self.inventory['parts']}
        expected = {'Inventory\\header_left', 'Inventory\\header_right'}
        expected.update({
            'Inventory\\tuple_left', 'Inventory\\tuple_left_pressed',
            'Inventory\\tuple_right', 'Inventory\\tuple_right_pressed',
            'Inventory\\mode_center', 'Inventory\\mode_center_highlighted',
            'Inventory\\mode_left', 'Inventory\\mode_left_highlighted',
            'Inventory\\mode_right', 'Inventory\\mode_right_highlighted',
        })
        for side in ('left', 'right'):
            for column in range(1, 5):
                stem = f'Inventory\\header_button_{side}_{column}'
                expected.update({stem, stem + '_hover', stem + '_pressed', stem + '_selected'})
        self.assertEqual(names, expected)
        self.assertEqual(len(names), 44)
        self.assertEqual(self.inventory.get('brush_overrides'), [{
            'source': 'DADGInventoryTuple.xml',
            'native': 'SandBox/GUI/Brushes/Inventory.xml',
        }])
        for name in ('Inventory\\armorSlot', 'Inventory\\armorSlotHighlight',
                     'Inventory\\bannerSlot', 'Inventory\\bannerSlotHighlight',
                     'Inventory\\filterAll', 'Inventory\\filterArmors',
                     'Inventory\\filterFood', 'Inventory\\filterMounts',
                     'Inventory\\filterShields', 'Inventory\\filterWeapons'):
            self.assertIn(name, self.inventory['protected'])

    def test_structural_headers_have_no_pigment_tints(self):
        # If native consumers acquire a tint, the PNG-only assumption needs review.
        families = [
            ('Inventory.xml', {'Inventory\\header_left', 'Inventory\\header_right'}, 2),
            ('PartyScreen.xml', {p['name'] for p in self.party['parts']}, 6),
        ]
        for filename, names, expected_count in families:
            consumers = []
            for brush in ET.parse(MODULES / 'SandBox/GUI/Brushes' / filename).findall('Brush'):
                if any(layer.get('Sprite') in names for layer in brush.iter('BrushLayer')):
                    consumers.append(brush)
                    for layer in brush.iter('BrushLayer'):
                        for key in ('Color', 'HueFactor', 'SaturationFactor'):
                            self.assertNotIn(key, layer.attrib, (brush.get('Name'), key))
            self.assertEqual(len(consumers), expected_count)
        self.assertFalse(self.party.get('brush_overrides'))

    def test_inventory_native_warning_and_interaction_semantics_remain_in_place(self):
        native_root = ET.parse(MODULES / 'SandBox/GUI/Brushes/Inventory.xml').getroot()
        brushes = {brush.get('Name'): brush for brush in native_root.findall('Brush')}
        override_root = ET.parse(RECIPES / 'DADGInventoryTuple.xml').getroot()
        self.assertEqual({brush.get('Name') for brush in override_root.findall('Brush')},
                         {'Inventory.Tuple.Left', 'Inventory.Tuple.Right'})
        for side in ('Left', 'Right'):
            warning = brushes[f'Inventory.Tuple.CharacterCantUse.{side}']
            layer = warning.find('./Layers/BrushLayer')
            self.assertEqual(layer.get('Color'), '#FF4239FF')
            self.assertEqual(layer.get('Sprite'), f'Inventory\\tuple_{side.lower()}')

            ordinary = brushes[f'Inventory.Tuple.{side}']
            expected = copy.deepcopy(ordinary)
            expected.find('./Layers/BrushLayer').set('Color', '#2870A9FF')
            self.assertEqual(tree(expected), tree(override_root.find(
                f"Brush[@Name='Inventory.Tuple.{side}']")))
            styles = {style.get('Name'): style.find('BrushLayer') for style in ordinary.findall('./Styles/Style')}
            self.assertEqual(styles['Default'].get('Sprite'), f'Inventory\\tuple_{side.lower()}')
            self.assertEqual(styles['Pressed'].get('Sprite'), f'Inventory\\tuple_{side.lower()}_pressed')
            self.assertEqual(styles['Hovered'].get('ValueFactor'), '10')
            self.assertEqual(styles['Selected'].get('ValueFactor'), '5')

        for position, stem in [('Center', 'center'), ('Side.Left', 'left'), ('Side.Right', 'right')]:
            brush = brushes[f'Inventory.Mode.Button.{position}']
            layers = {layer.get('Name'): layer for layer in brush.findall('./Layers/BrushLayer')}
            self.assertEqual(layers['Default'].get('Sprite'), f'Inventory\\mode_{stem}')
            self.assertEqual(layers['Highlighted'].get('Sprite'), f'Inventory\\mode_{stem}_highlighted')

    def test_inventory_states_remain_distinct_with_native_alpha(self):
        atlas = Image.open(ROOT / '.codex-work/ui-migration/compiled/SandBox/ui_inventory_1.png').convert('RGBA')
        rendered = {}
        for entry in self.inventory['parts']:
            part = self.parts[entry['name']]
            x, y, w, h = [int(part.findtext(k)) for k in ('SheetX', 'SheetY', 'Width', 'Height')]
            native = atlas.crop((x, y, x+w, y+h))
            result = paint(native, entry, self.panel, self.inner)
            self.assertEqual(result.size, native.size)
            self.assertEqual(result.getchannel('A').tobytes(), native.getchannel('A').tobytes())
            rendered[entry['name']] = result

            if entry['name'].startswith('Inventory\\tuple_'):
                self.assertEqual(entry['profile'], 'native_restore')
                self.assertEqual(result.tobytes(), native.tobytes())

        for side in ('left', 'right'):
            variants = [rendered[f'Inventory\\tuple_{side}'], rendered[f'Inventory\\tuple_{side}_pressed']]
            self.assertEqual(len({image.tobytes() for image in variants}), 2, side + ' tuple')
            for column in range(1, 5):
                stem = f'Inventory\\header_button_{side}_{column}'
                states = [rendered[stem + suffix] for suffix in ('', '_hover', '_pressed', '_selected')]
                self.assertEqual(len({image.tobytes() for image in states}), 4, stem)
                selected_light = ImageStat.Stat(states[-1].convert('L')).mean[0]
                self.assertGreater(selected_light, ImageStat.Stat(states[0].convert('L')).mean[0], stem)

        for stem in ('center', 'left', 'right'):
            normal = rendered[f'Inventory\\mode_{stem}']
            highlighted = rendered[f'Inventory\\mode_{stem}_highlighted']
            self.assertNotEqual(normal.tobytes(), highlighted.tobytes(), stem + ' mode')
            self.assertGreater(ImageStat.Stat(highlighted.convert('L')).mean[0],
                               ImageStat.Stat(normal.convert('L')).mean[0], stem + ' mode')

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
