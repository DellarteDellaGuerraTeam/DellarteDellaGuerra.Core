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
        baseline = paint(native, {'profile': 'recolor_blue'}, source, source)
        result = paint(native, {'profile': 'header', 'border_style': 'silhouette',
                                'border': 3, 'border_color': '#E7BC50FF',
                                'silhouette_feather_radius': 0.55}, source, source)
        transitions = [result.getpixel((x, 24))[:3] for x in range(4, 12)]
        baseline_pixels = [baseline.getpixel((x, 24))[:3] for x in range(4, 12)]
        self.assertTrue(any(pixel != original and pixel != (231, 188, 80)
                            for pixel, original in zip(transitions, baseline_pixels)))
        self.assertNotEqual(result.getpixel((24, 24))[:3], (231, 188, 80))
        self.assertEqual(result.getchannel('A').tobytes(), alpha.tobytes())

    def test_silhouette_can_recolor_a_native_inset_contour(self):
        source = Image.new('RGBA', (80, 80), '#e7bc50')
        native = Image.new('RGBA', (80, 80), '#304d68')
        ImageDraw.Draw(native).line((14, 30, 14, 60), fill='#b8b8b8', width=2)
        native.putpixel((40, 40), (184, 184, 184, 255))
        alpha = Image.new('L', native.size, 0)
        ImageDraw.Draw(alpha).rounded_rectangle((4, 4, 75, 75), radius=20, fill=255)
        native.putalpha(alpha)
        entry = {'profile': 'header', 'border_style': 'silhouette',
                 'border': 3, 'border_color': '#E7BC50FF',
                 'native_contour': {'box': [8, 24, 50, 68],
                                    'luminance_threshold': 100,
                                    'max_depth': 20,
                                    'feather_radius': 0.55}}
        baseline = paint(native, {key: value for key, value in entry.items()
                                  if key != 'native_contour'}, source, source)
        result = paint(native, entry,
                       source, source)
        self.assertGreater(result.getpixel((14, 40))[0], result.getpixel((14, 40))[2])
        self.assertGreater(result.getpixel((20, 40))[2], result.getpixel((20, 40))[0])
        self.assertGreater(result.getpixel((40, 40))[2], result.getpixel((40, 40))[0])
        self.assertGreater(result.getpixel((13, 40))[0], baseline.getpixel((13, 40))[0])
        self.assertLess(result.getpixel((13, 40))[0], result.getpixel((14, 40))[0])
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
