"""Scoped crafting cards reuse established navy/gold art, not battle-card sprites."""
from pathlib import Path
import unittest
import xml.etree.ElementTree as ET

HERE = Path(__file__).resolve().parent
MODULES = HERE.parents[3]


class CraftingCategoryBrushes(unittest.TestCase):
    def setUp(self):
        self.brushes = {b.get('Name'): b for b in ET.parse(
            HERE / 'DADGCraftingCategorySurface.xml').findall('Brush')}
        self.native = {b.get('Name'): b for b in ET.parse(
            MODULES / 'SandBox/GUI/Brushes/Crafting.xml').findall('Brush')}

    def test_exact_scope_and_native_behavior(self):
        self.assertEqual(set(self.brushes), {
            'Crafting.Order.Card', 'Crafting.Weapon.Type.Selection.Button'})
        for name, brush in self.brushes.items():
            native = self.native[name]
            self.assertEqual(brush.attrib, native.attrib)
            self.assertEqual(
                [s.attrib for s in brush.findall('./SoundProperties/EventSounds/EventSound')],
                [s.attrib for s in native.findall('./SoundProperties/EventSounds/EventSound')])
            self.assertEqual({s.get('Name') for s in brush.findall('./Styles/Style')},
                             {s.get('Name') for s in native.findall('./Styles/Style')})
            for style in native.findall('./Styles/Style'):
                actual = brush.find(f"./Styles/Style[@Name='{style.get('Name')}']/BrushLayer[@Name='Default']")
                source = style.find('BrushLayer')
                for key in ('ColorFactor', 'SaturationFactor', 'AlphaFactor'):
                    self.assertEqual(actual.get(key), source.get(key), (name, style.get('Name'), key))

    def test_dark_navy_and_aligned_existing_gold_frame(self):
        reference = ET.parse(HERE / 'DADGFaceGenCreationSurface.xml').find(
            "./Brush/Layers/BrushLayer[@Name='DADGFrame']")
        for brush in self.brushes.values():
            fill = brush.find("./Layers/BrushLayer[@Name='Default']")
            self.assertEqual(fill.get('Sprite'), 'BlankWhiteSquare_9')
            self.assertEqual(fill.get('Color'), '#071C2EFF')
            self.assertNotIn('OverlaySprite', fill.attrib)
            self.assertEqual(brush.find("./Layers/BrushLayer[@Name='DADGFrame']").attrib, reference.attrib)
            self.assertFalse(brush.findall('.//*[@FontColor]'))
        card = self.brushes['Crafting.Order.Card']
        self.assertEqual(card.find("./Layers/BrushLayer[@Name='Default']").get('AlphaFactor'), '0.75')
        self.assertNotEqual(card.find("./Styles/Style[@Name='Selected']/BrushLayer").get('Color'), '#071C2EFF')
        disabled = card.find("./Styles/Style[@Name='Disabled']/BrushLayer[@Name='DADGFrame']")
        self.assertEqual(disabled.get('SaturationFactor'), '-100')
        self.assertEqual(disabled.get('AlphaFactor'), '0.5')

    def test_both_popups_share_scoped_card_and_content_stays_native(self):
        for name in ('CraftingOrderPopup', 'CraftingTemplateSelectionPopup'):
            root = ET.parse(MODULES / f'SandBox/GUI/Prefabs/Crafting/{name}.xml')
            self.assertTrue(root.findall(".//*[@Brush='Crafting.Order.Card']"))
        category = ET.parse(MODULES / 'SandBox/GUI/Prefabs/Crafting/CraftingCategory.xml')
        button = category.find(".//*[@Id='FreeModeClassSelectionButton']")
        self.assertEqual(button.get('Brush'), 'Crafting.Weapon.Type.Selection.Button')
        self.assertEqual(button.get('Command.Click'), 'ExecuteOpenWeaponClassSelectionPopup')
        # No override for difficulty/reward/portrait/icon content; native red warnings survive.
        negative = self.native['Crafting.Order.Card.Property.Value.Negative']
        self.assertEqual(negative.find('./Styles/Style').get('FontColor'), '#ED1C24FF')
        self.assertNotIn('Crafting.Order.Card.Property.Value.Negative', self.brushes)


if __name__ == '__main__':
    unittest.main()
