"""The shared empty-list backing is clean near-black navy, not marked red art."""
import json
from pathlib import Path
import unittest
from PIL import Image
from build_shared_surfaces import paint


class ListCanvasSurfaces(unittest.TestCase):
    def test_scope_color_and_native_alpha(self):
        recipe = json.loads(Path(__file__).with_name('list-canvas-surfaces.json').read_text())
        self.assertEqual([p['name'] for p in recipe['parts']],
                         ['SPGeneral\\Frame1.Broken\\canvas'])
        self.assertIn('SPGeneral\\Frame1.Broken\\frame', recipe['protected'])
        native = Image.new('RGBA', (64, 64), (20, 18, 15, 197))
        reference = Image.new('RGBA', (64, 64))
        result = paint(native, recipe['parts'][0], reference, reference)
        self.assertEqual(result.size, native.size)
        self.assertEqual(result.getchannel('A').tobytes(), native.getchannel('A').tobytes())
        red, green, blue, _ = result.getpixel((32, 32))
        self.assertLess(red, green)
        self.assertLess(green, blue)
        self.assertLess(blue, 25)


if __name__ == '__main__':
    unittest.main()
