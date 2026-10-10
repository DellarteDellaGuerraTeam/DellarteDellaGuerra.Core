"""The gold campaign expander must not restyle save rows or alter its geometry."""
import json
from pathlib import Path
import unittest

from PIL import Image, ImageOps
from build_shared_surfaces import paint

HERE = Path(__file__).resolve().parent


class SaveLoadHeaderContrastTests(unittest.TestCase):
    def test_recipe_changes_header_only(self):
        recipe = json.loads((HERE / 'saveload-header-contrast.json').read_text())
        self.assertEqual([e['name'] for e in recipe['parts']], ['SaveLoad\\load_collapser'])
        self.assertFalse(recipe.get('brush_overrides'))
        self.assertIn('SaveLoad\\load_tuple', recipe['protected'])
        self.assertIn('SaveLoad\\load_tuple_selected', recipe['protected'])

    def test_palette_preserves_alpha_and_uses_antique_gold(self):
        native = Image.new('RGBA', (120, 64), '#756450')
        source = Image.new('RGBA', (80, 80), '#e7bc50')
        entry = json.loads((HERE / 'saveload-header-contrast.json').read_text())['parts'][0]
        old = paint(native, {'profile': 'header'}, source, source)
        new = paint(native, entry, source, source)
        self.assertEqual(new.size, old.size)
        self.assertEqual(new.getchannel('A').tobytes(), old.getchannel('A').tobytes())
        center = new.getpixel((60, 32))
        self.assertGreater(center[0], center[1])
        self.assertGreater(center[1], center[2] * 2)
        self.assertEqual(new.getpixel((60, 63))[:3], (240, 199, 90))

    def test_default_palette_remains_identical(self):
        native = Image.new('RGBA', (120, 64), '#756450')
        source = Image.new('RGBA', (80, 80), '#e7bc50')
        for profile, selected in [('row', False), ('row', True), ('header', False), ('recolor_blue', False)]:
            with self.subTest(profile=profile, selected=selected):
                entry = {'profile': profile, 'selected': selected}
                default = paint(native, entry, source, source)
                palette = ['#07172a', '#2870a9' if selected or profile == 'header' else '#174467']
                explicit = paint(native, dict(entry, blue_palette=palette), source, source)
                self.assertEqual(default.tobytes(), explicit.tobytes())
                expected = ImageOps.colorize(ImageOps.grayscale(native), *palette)
                self.assertEqual(default.getpixel((60, 32))[:3], expected.getpixel((60, 32)))


if __name__ == '__main__':
    unittest.main()
