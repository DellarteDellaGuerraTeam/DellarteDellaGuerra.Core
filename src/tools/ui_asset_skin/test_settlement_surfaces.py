"""Bannerlord-free contracts for the settlement overlay UI skin."""
import copy
import json
from pathlib import Path
import unittest
import xml.etree.ElementTree as ET

from PIL import Image

from build_shared_surfaces import check_selection, paint


HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]
MODULES = HERE.parents[3]


def tree(element):
    """Ignore XML indentation while retaining attributes and ordered children."""
    return (element.tag, dict(element.attrib), (element.text or '').strip(),
            tuple(tree(child) for child in element))


class SettlementSurfaceContracts(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.header = json.loads((HERE / 'settlement-header-surfaces.json').read_text())
        cls.frame = json.loads((HERE / 'settlement-frame-surfaces.json').read_text())
        cls.sprite_parts = {
            part.findtext('Name'): part
            for part in ET.parse(MODULES / 'Native/GUI/NativeSpriteData.xml').findall('./SpriteParts/SpritePart')
        }
        cls.native_settlement = ET.parse(MODULES / 'SandBox/GUI/Brushes/Settlement.xml').getroot()
        cls.native_game_menu = ET.parse(MODULES / 'Native/GUI/Brushes/GameMenu.xml').getroot()
        cls.settlement_override = ET.parse(HERE / 'DADGSettlementSurface.xml').getroot()
        cls.tableau_override = ET.parse(HERE / 'DADGGameMenuTableau.xml').getroot()

    def test_scope_is_only_three_structural_sprites(self):
        expected = {
            'SPGeneral\\MapOverlay\\Settlement\\settlement_info_header': ('ui_group1', '2'),
            'SPGeneral\\MapOverlay\\Settlement\\settlement_parties_characters_header': ('ui_group1', '2'),
            'SPGeneral\\MapOverlay\\Settlement\\parties_characters_group_frame': ('ui_group1', '1'),
        }
        selected = {
            entry['name']
            for recipe in (self.header, self.frame)
            for entry in recipe['parts']
        }
        self.assertEqual(selected, set(expected))
        portrait_header = next(entry for entry in self.header['parts']
                               if entry['name'].endswith('settlement_parties_characters_header'))
        self.assertEqual(portrait_header.get('border_style'), 'silhouette')
        for recipe in (self.header, self.frame):
            check_selection(recipe)
        for name, (category, sheet_id) in expected.items():
            with self.subTest(sprite=name):
                part = self.sprite_parts[name]
                self.assertEqual((part.findtext('CategoryName'), part.findtext('SheetID')),
                                 (category, sheet_id))

    def test_icons_toggles_art_and_action_background_stay_out_of_scope(self):
        selected = {entry['name'] for recipe in (self.header, self.frame) for entry in recipe['parts']}
        protected = set(self.header['protected']) | set(self.frame['protected'])
        self.assertFalse(selected & protected)
        for suffix in ('icon_garrison_big', 'icon_militia_big', 'icon_prosperity_big',
                       'garrison_toggle_pressed', 'marble_top', 'marble_bot',
                       'parties_characters_group_background', 'plus_button'):
            self.assertTrue(any(name.endswith(suffix) for name in protected), suffix)
        self.assertNotIn('GameMenu.Button', {brush.get('Name') for root in
                         (self.settlement_override, self.tableau_override) for brush in root.findall('Brush')})

    def test_atlas_rendering_preserves_native_geometry_and_alpha(self):
        approved = MODULES / 'DellarteDellaGuerraMap/GUI/DadgRecruitmentSpriteParts/ui_dadg_recruitment/DADG/Recruitment'
        panel = Image.open(approved / 'panel.png').convert('RGBA')
        inner = Image.open(approved / 'left.png').convert('RGBA')
        for recipe in (self.header, self.frame):
            atlas = Image.open(ROOT / f'.codex-work/ui-migration/compiled/Native/{recipe["sheet"]}.png').convert('RGBA')
            for entry in recipe['parts']:
                with self.subTest(sprite=entry['name']):
                    part = self.sprite_parts[entry['name']]
                    x, y, w, h = [int(part.findtext(key)) for key in ('SheetX', 'SheetY', 'Width', 'Height')]
                    native = atlas.crop((x, y, x + w, y + h))
                    rendered = paint(native, entry, panel, inner)
                    self.assertEqual(rendered.size, (w, h))
                    self.assertEqual(rendered.getchannel('A').tobytes(), native.getchannel('A').tobytes())
                    self.assertNotEqual(rendered.tobytes(), native.tobytes())

    def test_palette_overrides_are_exact_native_brushes_other_than_colour(self):
        cases = (
            (self.native_settlement, self.settlement_override,
             'SettlementOverlay.PartyList.Button', '#C67000FF', '#2870A9FF'),
            (self.native_settlement, self.settlement_override,
             'SettlementOverlay.CharacterList.Button', '#A9A100FF', '#E7BC50FF'),
            (self.native_game_menu, self.tableau_override,
             'GameMenu.Tableau.Background.Party', '#c6700022', '#2870A922'),
            (self.native_game_menu, self.tableau_override,
             'GameMenu.Tableau.Background.Character', '#A9A10022', '#E7BC5022'),
        )
        for native_root, override_root, name, before, after in cases:
            with self.subTest(brush=name):
                expected = copy.deepcopy(native_root.find(f"Brush[@Name='{name}']"))
                layer = expected.find('Layers/BrushLayer')
                self.assertEqual(layer.get('Color'), before)
                layer.set('Color', after)
                self.assertEqual(tree(expected), tree(override_root.find(f"Brush[@Name='{name}']")))

    def test_settlement_actions_and_portrait_commands_remain_native(self):
        game_menu_item = ET.parse(MODULES / 'Native/GUI/Prefabs/GameMenu/GameMenuItem.xml')
        action = game_menu_item.find(".//ButtonWidget[@Id='ParentButton']")
        self.assertEqual((action.get('Brush'), action.get('Command.Click'), action.get('IsEnabled')),
                         ('GameMenu.Button', 'ExecuteAction', '@IsEnabled'))
        button = self.native_game_menu.find("Brush[@Name='GameMenu.Button']")
        self.assertEqual({style.get('Name') for style in button.findall('Styles/Style')},
                         {'Default', 'Hovered', 'Pressed', 'Disabled'})

        portrait = ET.parse(MODULES / 'SandBox/GUI/Prefabs/Map/GameMenuPartyItem.xml').getroot().find(
            ".//GameMenuPartyItemButtonWidget")
        self.assertEqual(portrait.get('Command.Click'), 'ExecuteSetAsContextMenuItem')
        self.assertEqual(portrait.get('Command.AlternateClick'), 'ExecuteOpenEncyclopedia')
        self.assertEqual(portrait.get('Command.HoverBegin'), 'ExecuteOpenTooltip')
        self.assertEqual(portrait.get('Command.HoverEnd'), 'ExecuteCloseTooltip')
        self.assertEqual(portrait.get('PartyBackgroundBrush'), 'GameMenu.Tableau.Background.Party')
        self.assertEqual(portrait.get('CharacterBackgroundBrush'), 'GameMenu.Tableau.Background.Character')


if __name__ == '__main__':
    unittest.main()
