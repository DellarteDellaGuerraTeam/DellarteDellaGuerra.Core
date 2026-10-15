"""Choice-button palette and native-layout contracts; no Bannerlord required."""
from pathlib import Path
import unittest
import xml.etree.ElementTree as ET

from PIL import Image


TOOLS = Path(__file__).resolve().parent
MODULES = TOOLS.parents[3]


class CharacterCreationChoiceTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.root = ET.parse(TOOLS / 'DADGFaceGenCreationSurface.xml').getroot()
        cls.brush = cls.root.find("Brush[@Name='FaceGen.Generic.Button']")

    def test_only_generic_choice_brush_is_overridden(self):
        self.assertEqual([b.get('Name') for b in self.root], ['FaceGen.Generic.Button'])
        self.assertEqual(self.brush.attrib, {'Name': 'FaceGen.Generic.Button'})

    def test_fill_is_deeper_navy_and_gold_is_not_double_tinted(self):
        fill = self.brush.find("Layers/BrushLayer[@Name='Default']")
        frame = self.brush.find("Layers/BrushLayer[@Name='DADGFrame']")
        self.assertEqual(fill.attrib, {'Name': 'Default', 'Sprite': 'BlankWhiteSquare_9',
                                      'Color': '#071C2EFF'})
        self.assertEqual(frame.get('Sprite'), 'gold_frame_9')
        self.assertEqual(frame.get('Color'), '#FFFFFFFF')
        default = self.brush.find("Styles/Style[@Name='Default']/BrushLayer[@Name='DADGFrame']")
        self.assertEqual(default.get('ColorFactor'), '1')

    def test_frame_extension_matches_existing_gold_artwork_padding(self):
        module = MODULES / 'DellarteDellaGuerraMap'
        sprites = ET.parse(module / 'GUI/DadgMainUISpriteData.xml').getroot()
        part = next(p for p in sprites.findall('.//SpritePart')
                    if p.findtext('Name') == r'SPGeneral\TownManagement\gold_frame')
        x, y, w, h = [int(part.findtext(k)) for k in ('SheetX', 'SheetY', 'Width', 'Height')]
        with Image.open(module / 'AssetSources/GauntletUI/DADG_customui3_1.png') as atlas:
            sprite = atlas.crop((x, y, x+w, y+h)).convert('RGBA')
        # Opaque gold, not the translucent shadow, must meet the blue fill edge.
        horizontal = [x for x in range(w) if sprite.getpixel((x, h//2))[3] == 255]
        vertical = [y for y in range(h) if sprite.getpixel((w//2, y))[3] == 255]
        measured = dict(zip(('ExtendLeft', 'ExtendTop', 'ExtendRight', 'ExtendBottom'),
                            (min(horizontal), min(vertical),
                             w-1-max(horizontal), h-1-max(vertical))))
        self.assertEqual(measured, {'ExtendLeft': 5, 'ExtendTop': 5,
                                    'ExtendRight': 5, 'ExtendBottom': 6})
        frame = self.brush.find("Layers/BrushLayer[@Name='DADGFrame']")
        self.assertEqual({key: int(frame.get(key)) for key in measured}, measured)

    def test_native_states_and_choice_sound_are_preserved(self):
        native = ET.parse(MODULES / 'Native/GUI/Brushes/FaceGen.xml').getroot()
        original = native.find("Brush[@Name='FaceGen.Generic.Button']")
        self.assertEqual([s.get('Name') for s in self.brush.find('Styles')],
                         [s.get('Name') for s in original.find('Styles')])
        self.assertEqual(self.brush.find('.//EventSound').attrib,
                         original.find('.//EventSound').attrib)
        disabled = self.brush.find("Styles/Style[@Name='Disabled']/BrushLayer[@Name='DADGFrame']")
        self.assertLess(float(disabled.get('AlphaFactor')), 1)

    def test_native_choices_keep_content_height_and_text_padding(self):
        for name in ('CharacterCreationGenericStage', 'CharacterCreationNarrativeStage'):
            root = ET.parse(MODULES / f'Native/GUI/Prefabs/CharacterCreation/{name}.xml').getroot()
            button = root.find(".//ButtonWidget[@Brush='FaceGen.Generic.Button']")
            self.assertEqual(button.get('HeightSizePolicy'), 'CoverChildren')
            self.assertEqual(button.get('MarginBottom'), '8')
            text = button.find('Children/TextWidget')
            self.assertEqual((text.get('MarginTop'), text.get('MarginBottom')), ('7', '7'))


if __name__ == '__main__':
    unittest.main()
