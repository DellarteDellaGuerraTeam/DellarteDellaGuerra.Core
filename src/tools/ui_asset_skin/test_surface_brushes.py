"""Compare targeted palette overrides with installed native brush contracts."""
import copy
from pathlib import Path
import unittest
import xml.etree.ElementTree as ET


TOOLS = Path(__file__).resolve().parent
MODULES = TOOLS.parents[3]


def tree(element):
    """Ignore XML indentation, but retain every attribute and ordered child."""
    return (element.tag, dict(element.attrib), (element.text or '').strip(),
            tuple(tree(child) for child in element))


class SaveLoadBrushTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.native = ET.parse(MODULES / 'SandBox/GUI/Brushes/SaveLoad.xml').getroot()
        cls.override = ET.parse(TOOLS / 'DADGSaveLoadSurface.xml').getroot()

    def test_exactly_two_overrides_preserve_save_availability_and_text(self):
        self.assertEqual([b.attrib['Name'] for b in self.override],
                         ['SaveLoad.Collapser', 'Saveload.Scrollbar.Handle'])

    def test_header_changes_only_explicit_palette_attributes(self):
        expected = copy.deepcopy(self.native.find("Brush[@Name='SaveLoad.Collapser']"))
        layer = expected.find('Layers/BrushLayer')
        self.assertEqual((layer.get('SaturationFactor'), layer.get('HueFactor')), ('20', '-10'))
        layer.set('SaturationFactor', '0')
        layer.set('HueFactor', '0')
        selected = expected.find("Styles/Style[@Name='Selected']/BrushLayer")
        self.assertEqual(selected.get('Color'), '#FFC349FF')
        selected.set('Color', '#FFFFFFFF')
        self.assertEqual(tree(expected), tree(self.override.find("Brush[@Name='SaveLoad.Collapser']")))

    def test_scrollbar_changes_only_base_colour(self):
        expected = copy.deepcopy(self.native.find("Brush[@Name='Saveload.Scrollbar.Handle']"))
        layer = expected.find('Layers/BrushLayer')
        self.assertEqual(layer.get('Color'), '#573524FF')
        layer.set('Color', '#E7BC50FF')
        self.assertEqual(tree(expected), tree(self.override.find("Brush[@Name='Saveload.Scrollbar.Handle']")))


class QuestBrushTests(unittest.TestCase):
    # Path, attribute, exact native value: no other differences are allowed.
    REMOVALS = {
        'Quest.LeftPanel.Toggle': (
            ('Layers/BrushLayer', 'SaturationFactor', '20'),
            ('Layers/BrushLayer', 'HueFactor', '-10'),
            ("Styles/Style[@Name='Selected']/BrushLayer", 'Color', '#FFC349FF')),
        'Quest.LeftPanel.QuestButton.Active': (
            ('Layers/BrushLayer', 'Color', '#FFEBC1FF'),
            ('Layers/BrushLayer', 'HueFactor', '-5')),
        'Quest.LeftPanel.QuestButton.Active.MainStory': (
            ('Layers/BrushLayer', 'Color', '#FFEBC1FF'),),
        'Quest.LeftPanel.QuestButton.Active.NavalMainStory': (
            ('Layers/BrushLayer', 'HueFactor', '170'),),
    }

    @classmethod
    def setUpClass(cls):
        cls.native = ET.parse(MODULES / 'SandBox/GUI/Brushes/Quest.xml').getroot()
        cls.override = ET.parse(TOOLS / 'DADGQuestSurface.xml').getroot()

    def test_exactly_four_sandbox_overrides_without_dlc_dependency(self):
        self.assertEqual([b.attrib['Name'] for b in self.override], list(self.REMOVALS))
        for name in self.REMOVALS:
            self.assertIsNotNone(self.native.find(f"Brush[@Name='{name}']"))

    def test_only_allowlisted_palette_attributes_removed(self):
        for name, removals in self.REMOVALS.items():
            with self.subTest(brush=name):
                expected = copy.deepcopy(self.native.find(f"Brush[@Name='{name}']"))
                for path, attribute, native_value in removals:
                    element = expected.find(path)
                    self.assertEqual(element.get(attribute), native_value)
                    del element.attrib[attribute]
                # Includes every state, brightness, geometry and sound attribute.
                self.assertEqual(tree(expected), tree(self.override.find(f"Brush[@Name='{name}']")))

    def test_story_and_selected_saturation_remain_distinct(self):
        for name, path, value in (
            ('Quest.LeftPanel.QuestButton.Active.MainStory', 'Layers/BrushLayer', '100'),
            ('Quest.LeftPanel.QuestButton.Active.NavalMainStory', 'Layers/BrushLayer', '60'),
            ('Quest.LeftPanel.QuestButton.Active', "Styles/Style[@Name='Selected']/BrushLayer", '50'),
        ):
            with self.subTest(brush=name):
                for source in (self.native, self.override):
                    self.assertEqual(source.find(f"Brush[@Name='{name}']/{path}").get('SaturationFactor'), value)


if __name__ == '__main__':
    unittest.main()
