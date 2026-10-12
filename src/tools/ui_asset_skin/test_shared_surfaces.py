"""Small deterministic tests; no game/editor/browser required."""
import unittest
from PIL import Image, ImageDraw
from build_shared_surfaces import check_selection, nine_slice, paint


class SharedSurfacesTests(unittest.TestCase):
    def test_selection_rejects_protected_and_duplicate_sprites(self):
        check_selection({'parts': [{'name': 'panel'}], 'protected': ['icon']})
        for recipe in ({'parts': []},
                       {'parts': [{'name': 'panel'}, {'name': 'panel'}]},
                       {'parts': [{'name': 'icon'}], 'protected': ['icon']}):
            with self.assertRaises(ValueError):
                check_selection(recipe)

    def test_nine_slice_keeps_each_corner_and_border_thickness(self):
        source = Image.new('RGBA', (64, 64), '#113355')
        for box, color in (((0, 0, 12, 12), '#ffff88'), ((52, 0, 64, 12), '#dd9900'),
                           ((0, 52, 12, 64), '#eecc44'), ((52, 52, 64, 64), '#bb8800')):
            source.paste(color, box)
        result = nine_slice(source, (111, 157))
        for sx, sy, dx, dy in ((0, 0, 0, 0), (52, 0, 99, 0), (0, 52, 0, 145), (52, 52, 99, 145)):
            self.assertEqual(source.crop((sx, sy, sx+12, sy+12)).tobytes(),
                             result.crop((dx, dy, dx+12, dy+12)).tobytes())

    def test_both_profiles_keep_every_native_alpha_byte(self):
        native = Image.new('RGBA', (64, 64), '#887755')
        alpha = Image.new('L', native.size)
        alpha.putdata([i % 256 for i in range(64 * 64)])
        native.putalpha(alpha)
        source = Image.new('RGBA', (80, 80), '#123456')
        for profile in ('inner_surface', 'character_panel', 'framed_surface', 'row', 'header', 'recolor_blue', 'recolor_gold'):
            result = paint(native, {'profile': profile, 'inner_box': [12, 12, 52, 52]}, source, source)
            self.assertEqual(result.size, native.size)
            self.assertEqual(result.getchannel('A').tobytes(), alpha.tobytes())

    def test_selected_rows_differ_without_geometry_changes(self):
        source = Image.new('RGBA', (80, 80), '#e7bc50')
        native = Image.new('RGBA', (120, 64), '#756450')
        normal = paint(native, {'profile': 'row'}, source, source)
        selected = paint(native, {'profile': 'row', 'selected': True}, source, source)
        self.assertNotEqual(normal.tobytes(), selected.tobytes())
        self.assertEqual(normal.getchannel('A').tobytes(), selected.getchannel('A').tobytes())
        self.assertGreater(selected.getpixel((60, 32))[2], normal.getpixel((60, 32))[2])

    def test_silhouette_border_follows_a_rounded_alpha_edge(self):
        source = Image.new('RGBA', (80, 80), '#e7bc50')
        native = Image.new('RGBA', (80, 80), '#304d68')
        alpha = Image.new('L', native.size, 0)
        ImageDraw.Draw(alpha).rounded_rectangle((4, 4, 75, 75), radius=20, fill=255)
        native.putalpha(alpha)
        result = paint(native, {'profile': 'header', 'border_style': 'silhouette',
                                'border': 3, 'border_color': '#E7BC50FF'}, source, source)
        self.assertEqual(result.getpixel((4, 24))[:3], (231, 188, 80))
        self.assertNotEqual(result.getpixel((24, 24))[:3], (231, 188, 80))
        self.assertEqual(result.getchannel('A').tobytes(), alpha.tobytes())

    def test_rejects_invalid_geometry_and_unknown_profile(self):
        source = Image.new('RGBA', (80, 80))
        with self.assertRaises(ValueError):
            nine_slice(source, (24, 80))
        with self.assertRaises(ValueError):
            paint(source, {'profile': 'character_panel', 'inner_box': [-1, 0, 40, 40]}, source, source)
        with self.assertRaises(ValueError):
            paint(source, {'profile': 'unreviewed'}, source, source)


if __name__ == '__main__':
    unittest.main()
