"""Exact native contracts: allow only explicitly listed palette changes."""
import copy
import json
from pathlib import Path
import unittest
import xml.etree.ElementTree as ET


HERE = Path(__file__).resolve().parent
MODULES = HERE.parents[3]


def signature(node):
    return node.tag, node.attrib, [signature(child) for child in node]


class ClanKingdomContracts(unittest.TestCase):
    def test_palette_only_overrides(self):
        for family, collapser in [('Clan', 'Clan.Management.Collapser'),
                                  ('Kingdom', 'Kingdom.Policy.Toggle.Tuple')]:
            native = {b.get('Name'): b for b in ET.parse(MODULES / 'SandBox/GUI/Brushes' / (family+'.xml')).findall('Brush')}
            overrides = ET.parse(HERE / ('DADG'+family+'Surface.xml')).findall('Brush')
            names = {collapser, family+'.Item.Tuple'}
            if family == 'Kingdom':
                names.add('Kingdom.Wars.AtWar.Tuple')
            self.assertEqual({b.get('Name') for b in overrides}, names)
            for override in overrides:
                expected = copy.deepcopy(native[override.get('Name')])
                layer = expected.find('./Layers/BrushLayer')
                if override.get('Name') == 'Kingdom.Wars.AtWar.Tuple':
                    # Preserve the original inherited diplomacy tint while its parent becomes neutral.
                    layer.set('Color', native['Kingdom.Item.Tuple'].find('./Layers/BrushLayer').get('Color'))
                    style = ET.SubElement(ET.SubElement(expected, 'Styles'), 'Style', Name='Selected')
                    color = native['Kingdom.Item.Tuple'].find('./Styles/Style[@Name="Selected"]/BrushLayer').get('Color')
                    ET.SubElement(style, 'BrushLayer', Name='Default', Color=color)
                    self.assertEqual(signature(override), signature(expected))
                    continue
                elif override.get('Name') == collapser:
                    layer.set('SaturationFactor', '0')
                    layer.set('HueFactor', '0')
                else:
                    layer.set('Color', '#FFFFFFFF')
                expected.find('./Styles/Style[@Name="Selected"]/BrushLayer').set('Color', '#FFFFFFFF')
                self.assertEqual(signature(override), signature(expected), override.get('Name'))

    def test_live_scope_and_protected_art(self):
        docs = [p.read_text(encoding='utf-8-sig') for module in ('Native', 'SandBox', 'SandBoxCore', 'NavalDLC')
                for folder in ('Brushes', 'Prefabs')
                for p in (MODULES / module / 'GUI' / folder).rglob('*.xml')]
        for family in ('clan', 'kingdom'):
            recipe = json.loads((HERE / (family+'-surfaces.json')).read_text())
            selected = {p['name'] for p in recipe['parts']}
            self.assertFalse(selected.intersection(recipe['protected']))
            self.assertEqual(len(selected), len(recipe['parts']))
            for name in selected:
                self.assertTrue(any(name in text for text in docs), name)
            self.assertFalse(any('icon' in name or 'banner' in name or 'progress_bar_fill' in name for name in selected))


if __name__ == '__main__':
    unittest.main()
