"""Bannerlord-free contracts for the Banner Editor UI skin."""
import copy
import json
from pathlib import Path
import unittest
import xml.etree.ElementTree as ET

from PIL import Image, ImageStat

from build_shared_surfaces import check_selection, paint


HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]
MODULES = HERE.parents[3]


def tree(element):
    """Ignore XML indentation while retaining attributes and ordered children."""
    return (element.tag, dict(element.attrib), (element.text or '').strip(),
            tuple(tree(child) for child in element))


class BannerEditorSurfaceContracts(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.recipe = json.loads((HERE / 'banner-editor-surfaces.json').read_text())
        cls.sprite_parts = {
            part.findtext('Name'): part
            for part in ET.parse(MODULES / 'Native/GUI/NativeSpriteData.xml').findall(
                './SpriteParts/SpritePart')
        }
        cls.native_brushes = ET.parse(MODULES / 'Native/GUI/Brushes/FaceGen.xml').getroot()
        cls.override = ET.parse(HERE / 'DADGBannerEditorSurface.xml').getroot()

    def test_scope_is_four_shared_ui_group1_2_primitives(self):
        expected = {
            'General\\CharacterCreation\\facegen_card',
            'General\\CharacterCreation\\facegen_card_selected',
            'SPGeneral\\SPOptions\\standart_slider_canvas',
            'SPGeneral\\SPOptions\\standart_slider_frame',
        }
        self.assertEqual({entry['name'] for entry in self.recipe['parts']}, expected)
        self.assertFalse(expected & set(self.recipe['protected']))
        check_selection(self.recipe)
        for name in expected:
            with self.subTest(sprite=name):
                part = self.sprite_parts[name]
                self.assertEqual((part.findtext('CategoryName'), part.findtext('SheetID')),
                                 ('ui_group1', '2'))

    def test_rendering_preserves_alpha_and_establishes_blue_gold_hierarchy(self):
        approved = (MODULES /
                    'DellarteDellaGuerraMap/GUI/DadgRecruitmentSpriteParts/'
                    'ui_dadg_recruitment/DADG/Recruitment')
        panel = Image.open(approved / 'panel.png').convert('RGBA')
        inner = Image.open(approved / 'left.png').convert('RGBA')
        atlas = Image.open(
            ROOT / '.codex-work/ui-migration/compiled/Native/ui_group1_2.png').convert('RGBA')
        rendered = {}
        for entry in self.recipe['parts']:
            part = self.sprite_parts[entry['name']]
            x, y, w, h = [int(part.findtext(key))
                          for key in ('SheetX', 'SheetY', 'Width', 'Height')]
            native = atlas.crop((x, y, x + w, y + h))
            result = paint(native, entry, panel, inner)
            self.assertEqual(result.getchannel('A').tobytes(), native.getchannel('A').tobytes())
            self.assertNotEqual(result.tobytes(), native.tobytes())
            rendered[entry['name']] = result

            if entry['name'].startswith('General\\CharacterCreation\\facegen_card'):
                previous = dict(entry, blue_palette=['#07182B', '#34789A'])
                before = paint(native, previous, panel, inner)
                # Both card states keep a visibly navy interior but separate it
                # from the lighter blue surrounding panel. Do not dim heraldry.
                box = (45, 45, 95, 95)
                before_mean = ImageStat.Stat(before.crop(box)).mean[:3]
                after_mean = ImageStat.Stat(result.crop(box)).mean[:3]
                self.assertLess(after_mean[2], before_mean[2] * 0.75)
                self.assertGreater(after_mean[2], 25)
                self.assertGreater(after_mean[2], after_mean[0] * 2)
                if entry['profile'] == 'card_selected':
                    # Solid bright rim stays exact; the soft transition can
                    # blend with the newly darker blue underneath it.
                    grey = native.convert('L')
                    margin = entry['gold_edge_margin']
                    gold_pixels = [(before.getpixel((x, y)), result.getpixel((x, y)))
                                   for y in range(native.height) for x in range(native.width)
                                   if (x < margin or x >= native.width - margin or
                                       y < margin or y >= native.height - margin)
                                   and grey.getpixel((x, y)) >=
                                   entry['gold_luminance_threshold'] + 51]
                    self.assertGreater(len(gold_pixels), 100)
                    self.assertTrue(all(old == new for old, new in gold_pixels))

        card = rendered['General\\CharacterCreation\\facegen_card']
        card_mean = ImageStat.Stat(card, card.getchannel('A')).mean[:3]
        self.assertGreater(card_mean[2], card_mean[0] * 1.35)

        selected = rendered['General\\CharacterCreation\\facegen_card_selected']
        center = selected.crop((45, 45, 95, 95))
        center_mean = ImageStat.Stat(center, center.getchannel('A')).mean[:3]
        self.assertGreater(center_mean[2], center_mean[0] * 1.25)
        edge = selected.crop((12, 12, 128, 30))
        edge_mean = ImageStat.Stat(edge, edge.getchannel('A')).mean[:3]
        self.assertGreater(edge_mean[0], edge_mean[2] * 1.1)
        self.assertGreater(edge_mean[1], edge_mean[2])

        trough = rendered['SPGeneral\\SPOptions\\standart_slider_canvas']
        trough_mean = ImageStat.Stat(trough, trough.getchannel('A')).mean[:3]
        self.assertGreater(trough_mean[2], trough_mean[0] * 1.4)
        frame = rendered['SPGeneral\\SPOptions\\standart_slider_frame']
        frame_mean = ImageStat.Stat(frame, frame.getchannel('A')).mean[:3]
        self.assertGreater(frame_mean[0], frame_mean[2] * 1.2)
        self.assertGreater(frame_mean[1], frame_mean[2])
        # Gold hue alone also admits the old muddy brown. Require a bright
        # body/highlight range while retaining the carved relief and alpha.
        opaque = frame.getchannel('A').point(lambda value: 255 if value > 230 else 0)
        opaque_mean = ImageStat.Stat(frame, opaque).mean[:3]
        self.assertGreater(opaque_mean[0], 195)
        self.assertGreater(opaque_mean[1], 155)
        self.assertGreater(ImageStat.Stat(frame, opaque).stddev[0], 15)

    def test_brush_overrides_only_change_palette_brightness(self):
        scrollbar = copy.deepcopy(
            self.native_brushes.find("Brush[@Name='FaceGen.Scrollbar.Handle']"))
        scrollbar.find('Layers/BrushLayer').set('Color', '#D8AB45FF')
        self.assertEqual(
            tree(scrollbar),
            tree(self.override.find("Brush[@Name='FaceGen.Scrollbar.Handle']")))

        card = copy.deepcopy(self.native_brushes.find("Brush[@Name='FaceGen.Card']"))
        card.find("Styles/Style[@Name='Hovered']/BrushLayer[@Name='Default']").set(
            'ColorFactor', '1.35')
        card.find("Styles/Style[@Name='Selected']/BrushLayer[@Name='Selected']").set(
            'ColorFactor', '1.25')
        self.assertEqual(tree(card), tree(self.override.find("Brush[@Name='FaceGen.Card']")))

    def test_native_banner_editor_layout_and_bindings_are_untouched(self):
        prefab = ET.parse(MODULES / 'Native/GUI/Prefabs/BannerEditor/BannerEditor.xml')
        root = prefab.getroot()
        panel = root.find(".//Widget[@Sprite='General\\CharacterCreation\\character_creation_panel']")
        self.assertEqual((panel.get('SuggestedWidth'), panel.get('SuggestedHeight')), ('694', '1080'))
        cards = root.findall(".//ButtonWidget[@Brush='FaceGen.Card']")
        self.assertEqual(len(cards), 1)
        self.assertEqual(cards[0].get('Command.Click'), 'ExecuteSelectIcon')
        self.assertIsNotNone(root.find(
            ".//Widget[@Sprite='SPGeneral\\SPOptions\\standart_slider_fill']"))
        self.assertIsNotNone(root.find(
            ".//BrushWidget[@Id='SliderHandle'][@Brush='SPOptions.Slider.Handle']"))
        self.assertIsNotNone(root.find(".//ColorButtonWidget[@Brush='Creation.ColorCard']"))
        self.assertIsNotNone(root.find(".//ImageWidget[@Brush='Creation.ColorCard.Selection']"))


if __name__ == '__main__':
    unittest.main()
