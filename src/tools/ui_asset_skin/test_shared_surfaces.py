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
        for profile in ('inner_surface', 'character_panel', 'framed_surface', 'row', 'header', 'recolor_blue', 'recolor_gold', 'medallion'):
            result = paint(native, {'profile': profile, 'inner_box': [12, 12, 52, 52],
                                    'gold_edge_width': 6, 'native_center_inset': 16}, source, source)
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

    def test_medallion_recolors_rim_and_preserves_semantic_center(self):
        source = Image.new('RGBA', (80, 80), '#e7bc50')
        native = Image.new('RGBA', (84, 84), '#56391f')
        ImageDraw.Draw(native).ellipse((1, 1, 82, 82), fill='#4a4038')
        ImageDraw.Draw(native).ellipse((22, 22, 61, 61), fill='#ef7b12')
        alpha = Image.new('L', native.size, 0)
        ImageDraw.Draw(alpha).ellipse((1, 1, 82, 82), fill=255)
        native.putalpha(alpha)
        result = paint(native, {'profile': 'medallion',
                                'gold_edge_width': 8,
                                'native_center_inset': 18}, source, source)
        self.assertGreater(result.getpixel((42, 3))[0], result.getpixel((42, 3))[2])
        self.assertGreater(result.getpixel((42, 14))[2], result.getpixel((42, 14))[0])
        self.assertEqual(result.getpixel((42, 42)), native.getpixel((42, 42)))
        self.assertEqual(result.getchannel('A').tobytes(), alpha.tobytes())

    def test_recolour_can_preserve_bright_native_detail_inside_a_declared_box(self):
        source = Image.new('RGBA', (80, 80), '#e7bc50')
        native = Image.new('RGBA', (40, 30), '#554433')
        native.putpixel((12, 12), (240, 235, 225, 255))
        native.putpixel((32, 12), (240, 235, 225, 255))
        baseline = paint(native, {'profile': 'recolor_blue'}, source, source)
        result = paint(native, {
            'profile': 'recolor_blue',
            'native_detail': {'box': [5, 5, 20, 25],
                              'luminance_threshold': 82,
                              'contrast': 5},
        }, source, source)
        self.assertEqual(result.getpixel((12, 12)), native.getpixel((12, 12)))
        self.assertEqual(result.getpixel((32, 12)), baseline.getpixel((32, 12)))
        self.assertEqual(result.getchannel('A').tobytes(), native.getchannel('A').tobytes())

    def test_recolour_can_preserve_edges_without_copying_a_bright_background(self):
        source = Image.new('RGBA', (80, 80), '#e7bc50')
        native = Image.new('RGBA', (40, 30), '#b08a45')
        ImageDraw.Draw(native).line((10, 8, 10, 22), fill='#fff7dc', width=2)
        baseline = paint(native, {'profile': 'recolor_gold'}, source, source)
        result = paint(native, {
            'profile': 'recolor_gold',
            'native_detail': {'box': [5, 5, 20, 25],
                              'high_pass_radius': 3,
                              'contrast': 10},
        }, source, source)
        self.assertEqual(result.getpixel((16, 12)), baseline.getpixel((16, 12)))
        self.assertNotEqual(result.getpixel((10, 12)), baseline.getpixel((10, 12)))
        self.assertEqual(result.getchannel('A').tobytes(), native.getchannel('A').tobytes())

    def test_silhouette_border_follows_a_rounded_alpha_edge(self):
        source = Image.new('RGBA', (80, 80), '#e7bc50')
        native = Image.new('RGBA', (80, 80), '#304d68')
        alpha = Image.new('L', native.size, 32)
        ImageDraw.Draw(alpha).rounded_rectangle((4, 4, 75, 75), radius=20, fill=255)
        native.putalpha(alpha)
        baseline = paint(native, {'profile': 'recolor_blue',
                                  'blue_palette': ['#07172a', '#2870a9']},
                         source, source)
        result = paint(native, {'profile': 'header', 'border_style': 'silhouette',
                                'border': 3, 'border_color': '#E7BC50FF',
                                'silhouette_feather_radius': 0.55}, source, source)
        transitions = [result.getpixel((x, 24))[:3] for x in range(4, 12)]
        baseline_pixels = [baseline.getpixel((x, 24))[:3] for x in range(4, 12)]
        self.assertTrue(any(pixel != original and pixel != (231, 188, 80)
                            for pixel, original in zip(transitions, baseline_pixels)))
        self.assertNotEqual(result.getpixel((24, 24))[:3], (231, 188, 80))
        self.assertEqual(result.getpixel((4, 4))[:3], baseline.getpixel((4, 4))[:3])
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

    def test_silhouette_can_remove_a_long_native_shadow_tail(self):
        source = Image.new('RGBA', (80, 80), '#e7bc50')
        native = Image.new('RGBA', (80, 80), '#304d68')
        alpha = Image.new('L', native.size, 32)
        ImageDraw.Draw(alpha).rounded_rectangle((8, 8, 71, 71), radius=20, fill=255)
        native.putalpha(alpha)
        result = paint(native, {'profile': 'header', 'border_style': 'silhouette',
                                'border': 3, 'border_color': '#E7BC50FF',
                                'silhouette_feather_radius': 0.55,
                                'alpha_cleanup': {'threshold': 128,
                                                  'feather_radius': 0.55}},
                       source, source)
        cleaned_alpha = result.getchannel('A')
        self.assertNotEqual(cleaned_alpha.tobytes(), alpha.tobytes())
        self.assertEqual(cleaned_alpha.getpixel((0, 40)), 0)
        self.assertEqual(cleaned_alpha.getpixel((40, 40)), 255)
        self.assertTrue(any(0 < value < 255 for value in cleaned_alpha.tobytes()))

    def test_rejects_invalid_geometry_and_unknown_profile(self):
        source = Image.new('RGBA', (80, 80))
        with self.assertRaises(ValueError):
            nine_slice(source, (24, 80))
        with self.assertRaises(ValueError):
            paint(source, {'profile': 'character_panel', 'inner_box': [-1, 0, 40, 40]}, source, source)
        with self.assertRaises(ValueError):
            paint(source, {'profile': 'unreviewed'}, source, source)
        with self.assertRaises(ValueError):
            paint(source, {'profile': 'header', 'alpha_cleanup': {'threshold': 128}},
                  source, source)
        with self.assertRaises(ValueError):
            paint(source, {'profile': 'medallion', 'gold_edge_width': 20,
                           'native_center_inset': 18}, source, source)


if __name__ == '__main__':
    unittest.main()
