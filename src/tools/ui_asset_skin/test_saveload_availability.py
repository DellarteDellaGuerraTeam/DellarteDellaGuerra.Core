"""Availability palette and narrowly scoped native-row patch contracts."""
import copy
import json
from pathlib import Path
import unittest
import xml.etree.ElementTree as ET

from PIL import Image, ImageOps, ImageStat
from build_shared_surfaces import paint
from test_surface_brushes import tree

HERE = Path(__file__).resolve().parent
MODULES = HERE.parents[3]


class SaveLoadAvailabilityTests(unittest.TestCase):
    def test_text_overrides_only_add_disabled_colours(self):
        native = ET.parse(MODULES / 'SandBox/GUI/Brushes/SaveLoad.xml')
        overrides = ET.parse(HERE / 'DADGSaveLoadText.xml').getroot()
        self.assertEqual([b.get('Name') for b in overrides],
                         ['SaveLoad.Tuple.Title.Text', 'SaveLoad.Tuple.RealTime.Text'])
        for brush in overrides:
            disabled = brush.find("Styles/Style[@Name='Disabled']")
            self.assertIsNotNone(disabled)
            self.assertEqual(set(disabled.attrib), {'Name', 'FontColor'})
            brush.find('Styles').remove(disabled)
            self.assertEqual(tree(brush), tree(native.find(f"Brush[@Name='{brush.get('Name')}']")))

    def test_invalid_brush_changes_only_palette_not_selection(self):
        native = ET.parse(MODULES / 'SandBox/GUI/Brushes/SaveLoad.xml')
        expected = copy.deepcopy(native.find("Brush[@Name='SaveLoad.Tuple.Invalid']"))
        for layer in expected.findall('Layers/BrushLayer'):
            layer.set('SaturationFactor', '-65')
            layer.set('ValueFactor', '0')
            layer.set('AlphaFactor', '1')
        for state, factor in [('Default', '0.42'), ('Hovered', '0.50'), ('Pressed', '0.35')]:
            expected.find(f"Styles/Style[@Name='{state}']/BrushLayer").set('ColorFactor', factor)
        selected = expected.find("Styles/Style[@Name='Selected']/BrushLayer[@Name='Selected']")
        selected.set('SaturationFactor', '-65')
        selected.set('ColorFactor', '0.55')
        actual = ET.parse(HERE / 'DADGSaveLoadAvailability.xml').find("Brush[@Name='SaveLoad.Tuple.Invalid']")
        self.assertEqual(tree(expected), tree(actual))

    def test_palette_alpha_and_selection_outline(self):
        recipe = json.loads((HERE / 'saveload-availability.json').read_text())
        previous = json.loads((HERE / 'saveload-ledger.json').read_text())
        source = Image.new('RGBA', (120, 64), '#756450')
        results = [paint(source, part, source, source) for part in recipe['parts']]
        old_header = paint(source, previous['parts'][0], source, source)
        mean = lambda image: ImageStat.Stat(ImageOps.grayscale(image.crop((4, 4, 116, 60)))).mean[0]
        self.assertGreater(mean(results[0]), mean(old_header) * 1.5)
        self.assertEqual(recipe['parts'][2], previous['parts'][2])
        self.assertEqual(len(results), 3)
        for image in results:
            self.assertEqual(image.getchannel('A').tobytes(), source.getchannel('A').tobytes())
        self.assertEqual(recipe['protected'], ['SaveLoad\\load_button_canvas'])

    def test_banner_patch_targets_one_native_widget(self):
        native = ET.parse(MODULES / 'SandBox/GUI/Prefabs/SaveLoad/SavedGameTuple.xml')
        banners = native.findall(".//MaskedTextureWidget[@DataSource='{ClanBanner}']")
        self.assertEqual(len(banners), 1)
        self.assertEqual(banners[0].get('VerticalAlignment'), 'Top')
        self.assertIsNone(banners[0].get('MarginTop'))
        patch = (HERE.parents[1] / 'DellarteDellaGuerra.Integration/UI/SaveLoad/SaveLoadSkinPrefabExtensions.cs').read_text()
        self.assertIn('new Attribute("MarginTop", "6")', patch)
        self.assertNotIn('Command.', patch)


if __name__ == '__main__':
    unittest.main()
