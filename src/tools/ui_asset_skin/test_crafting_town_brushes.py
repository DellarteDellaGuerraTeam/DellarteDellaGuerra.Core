"""Scope contracts for the crafting-only structural pass and town art exclusion."""
import json
from pathlib import Path
import unittest
import xml.etree.ElementTree as ET
from PIL import Image
from build_shared_surfaces import paint

HERE = Path(__file__).resolve().parent
MODULES = HERE.parents[3]


class CraftingTownContracts(unittest.TestCase):
    def test_live_scope_and_protected_art(self):
        recipe = json.loads((HERE / 'crafting-surfaces.json').read_text())
        selected = {entry['name'] for entry in recipe['parts']}
        self.assertEqual(len(selected), 29)
        self.assertFalse(selected.intersection(recipe['protected']))
        self.assertFalse(recipe.get('brush_overrides'))
        docs = [p.read_text(encoding='utf-8-sig') for folder in ('Brushes', 'Prefabs')
                for p in (MODULES / 'SandBox/GUI' / folder).rglob('*.xml')]
        for name in selected:
            self.assertTrue(any(name in text for text in docs), name)
            self.assertFalse(any(word in name for word in ('material', 'WeaponTypes', 'icon', 'locked', 'disabled', 'fill', 'glow')))
        # Do not select source regions already shadowed by bespoke DADG registration.
        custom = {p.findtext('Name') for xml in (MODULES / 'DellarteDellaGuerraMap/GUI').glob('*SpriteData.xml')
                  for p in ET.parse(xml).findall('./SpriteParts/SpritePart')}
        self.assertFalse(selected.intersection(custom))

    def test_interaction_variants_and_frame_only_gold(self):
        recipe = json.loads((HERE / 'crafting-surfaces.json').read_text())
        entries = {e['name'].split('\\')[-1]: e for e in recipe['parts']}
        for prefix in ('smelt_tuple', 'order_mode_button'):
            self.assertFalse(entries[prefix + '_default'].get('selected', False))
            self.assertFalse(entries[prefix + '_pressed'].get('selected', False))
            self.assertTrue(entries[prefix + '_selected']['selected'])
        self.assertTrue(entries['tuple_refine_selected']['selected'])
        gold = {name for name, e in entries.items() if e['profile'] == 'recolor_gold'}
        self.assertTrue(all('frame' in name or name == 'bottom_crafting' or name.startswith('main_tab_') for name in gold))

    def test_header_art_only_removed_and_all_tab_states_gold(self):
        recipe = json.loads((HERE / 'crafting-surfaces.json').read_text())
        entries = {e['name'].split('\\')[-1]: e for e in recipe['parts']}
        header = entries['refine_panel_header']
        native = Image.new('RGBA', (575, 51), '#FFFFFFFF')
        cleared = paint(native, header, None, None)
        self.assertEqual(cleared.size, native.size)
        self.assertIsNone(cleared.getbbox())
        prefab = ET.parse(MODULES / 'SandBox/GUI/Prefabs/Crafting/RefinementCategory.xml')
        self.assertIsNotNone(prefab.find(".//*[@Sprite='Crafting\\refine_panel_header']/Children/TextWidget[@Text='@RefinementText']"))
        for state in ('default', 'hovered', 'pressed', 'selected'):
            self.assertEqual(entries['main_tab_' + state]['profile'], 'recolor_gold')

    def test_town_sheet_is_illustration_only(self):
        parts = [p for p in ET.parse(MODULES / 'SandBox/GUI/SandBoxSpriteData.xml').findall('./SpriteParts/SpritePart')
                 if p.findtext('CategoryName') == 'ui_town_management']
        self.assertEqual(len(parts), 52)
        self.assertTrue(all(p.findtext('Name').startswith('building_') for p in parts))
        self.assertFalse((HERE / 'town-surfaces.json').exists(), 'Do not repaint building art as blue panels')


if __name__ == '__main__':
    unittest.main()
