"""Narrow clan-name banner preview contract; no Bannerlord process required."""
from pathlib import Path
import re
import unittest
import xml.etree.ElementTree as ET


TOOLS = Path(__file__).resolve().parent
SRC = TOOLS.parents[1]
MODULES = TOOLS.parents[3]


class ClanNamingBannerTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.prefab = ET.parse(MODULES / 'Native/GUI/Prefabs/CharacterCreation/CharacterCreationClanNamingStage.xml').getroot()
        cls.patch = (SRC / 'DellarteDellaGuerra.Integration/UI/CharacterCreation/ClanNamingSquareBannerPatch.cs').read_text()
        cls.brushes = ET.parse(TOOLS / 'DADGClanNamingBanner.xml').getroot()

    def test_patch_targets_only_native_dynamic_clan_banner(self):
        selector = ".//MaskedTextureWidget[@DataSource='{ClanBanner}'][@Brush='ClanNaming.Stage.Banner']"
        widgets = self.prefab.findall(selector)
        self.assertEqual(len(widgets), 1)
        self.assertIn('CharacterCreationClanNamingStage', self.patch)
        self.assertIn(selector.replace('.//', 'descendant::'), self.patch)
        self.assertEqual(widgets[0].get('ImageId'), '@Id')
        self.assertEqual(widgets[0].get('AdditionalArgs'), '@AdditionalArgs')
        self.assertEqual(widgets[0].get('TextureProviderName'), '@TextureProviderName')

    def test_square_size_fits_native_column_without_moving_input(self):
        attributes = dict(re.findall(r'new Attribute\("([^"]+)", "([^"]+)"\)', self.patch))
        self.assertEqual(attributes, {'SuggestedWidth': '440', 'SuggestedHeight': '440',
                                      'Brush': 'DADG.ClanNaming.SquareBanner'})
        inner = self.prefab.find(".//Widget[@Id='InnerPanel']")
        stack = inner.find(".//ListPanel[@Id='List']")
        available = int(inner.get('SuggestedWidth')) - int(stack.get('MarginLeft')) - int(stack.get('MarginRight'))
        self.assertLess(int(attributes['SuggestedWidth']), available)
        self.assertEqual(stack.find("Children/Widget[@Sprite='General\\CharacterCreation\\name_input_area']").get('SuggestedHeight'), '55')

    def test_reuses_square_encyclopedia_mask_without_dimming_banner_colors(self):
        self.assertEqual([b.get('Name') for b in self.brushes], ['DADG.ClanNaming.SquareBanner'])
        layer = self.brushes.find('Brush/Layers/BrushLayer')
        encyclopedia = ET.parse(MODULES / 'SandBox/GUI/Brushes/Encyclopedia.xml').getroot()
        reference = encyclopedia.find("Brush[@Name='Encyclopedia.Clan.SubElement.Banner']/Layers/BrushLayer")
        self.assertEqual(layer.get('Sprite'), reference.get('Sprite'))
        self.assertEqual(layer.get('Color'), '#FFFFFFFF')
        self.assertEqual(layer.get('ColorFactor'), '0.5')
        self.assertEqual((layer.get('OverlayXOffset'), layer.get('OverlayYOffset')), ('0', '0'))

    def test_overlay_base_preserves_dynamic_banner_channels_instead_of_turning_white(self):
        # Native Shaders/Sources/two_dimensional_ui.rs: blendOverlay2.
        # The white mask is multiplied by ColorFactor before this blend.
        base = float(self.brushes.find('Brush/Layers/BrushLayer').get('ColorFactor'))
        for channel in (0.0, 0.08, 0.25, 0.5, 0.8, 1.0):
            result = (2 * base * channel if base < 0.5
                      else 1 - 2 * (1 - base) * (1 - channel))
            self.assertAlmostEqual(result, channel)


if __name__ == '__main__':
    unittest.main()
