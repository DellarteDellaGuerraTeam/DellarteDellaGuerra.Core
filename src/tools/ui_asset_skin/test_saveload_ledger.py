"""Approved ledger hierarchy without the ordinary save's full gold rectangle."""
import json
from pathlib import Path
import unittest

from PIL import Image, ImageOps, ImageStat
from build_shared_surfaces import paint

HERE = Path(__file__).resolve().parent


class SaveLoadLedgerTests(unittest.TestCase):
    def setUp(self):
        self.recipe = json.loads((HERE / 'saveload-ledger.json').read_text())
        self.native = Image.new('RGBA', (120, 64), '#756450')
        self.source = Image.new('RGBA', (80, 80), '#e7bc50')

    def render(self, entry):
        return paint(self.native, entry, self.source, self.source)

    def test_only_three_structural_sprites_selected(self):
        self.assertEqual([p['name'] for p in self.recipe['parts']], [
            'SaveLoad\\load_collapser', 'SaveLoad\\load_tuple', 'SaveLoad\\load_tuple_selected'])
        self.assertFalse(self.recipe.get('brush_overrides'))
        self.assertIn('SaveLoad\\load_button_canvas', self.recipe['protected'])

    def test_header_and_normal_row_have_only_bottom_separator(self):
        for entry in self.recipe['parts'][:2]:
            with self.subTest(sprite=entry['name']):
                image = self.render(entry)
                plain = ImageOps.colorize(ImageOps.grayscale(self.native), *entry['blue_palette']).convert('RGBA')
                self.assertEqual(image.crop((0, 0, 120, 63)).tobytes(), plain.crop((0, 0, 120, 63)).tobytes())
                color = Image.new('RGBA', (120, 1), entry['border_color'])
                self.assertEqual(image.crop((0, 63, 120, 64)).tobytes(), color.tobytes())

    def test_selected_row_keeps_full_gold_outline(self):
        image = self.render(self.recipe['parts'][2])
        gold = (231, 188, 80, 255)
        for xy in ((0, 0), (60, 1), (119, 30), (0, 30), (60, 63)):
            self.assertEqual(image.getpixel(xy), gold)
        self.assertNotEqual(image.getpixel((60, 30)), gold)

    def test_lighter_rows_and_alpha_preserved(self):
        images = [self.render(entry) for entry in self.recipe['parts']]
        means = [ImageStat.Stat(ImageOps.grayscale(image.crop((4, 4, 116, 60)))).mean[0] for image in images]
        self.assertGreater(means[1], means[0] * 2)
        self.assertGreater(means[2], means[1])
        alpha = Image.linear_gradient('L').resize(self.native.size)
        alpha.paste(255, (4, 4, 116, 60))
        self.native.putalpha(alpha)
        for entry in self.recipe['parts']:
            self.assertEqual(self.render(entry).getchannel('A').tobytes(), self.native.getchannel('A').tobytes())

    def test_unknown_border_style_rejected(self):
        with self.assertRaises(ValueError):
            self.render(dict(self.recipe['parts'][0], border_style='unknown'))


if __name__ == '__main__':
    unittest.main()
