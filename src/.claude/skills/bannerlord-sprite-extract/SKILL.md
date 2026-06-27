---
name: bannerlord-sprite-extract
description: Extract a named sprite from a Bannerlord texture atlas PNG by cropping it using coordinates from the SpriteData XML, saving it as a standalone PNG file. Use this skill whenever the user wants to extract, crop, or export a specific sprite from an atlas, or says things like "extract sprite", "get sprite from atlas", "export sprite", "crop sprite out of the atlas", or "I want the PNG for sprite X".
---

# bannerlord-sprite-extract

Extract a named sprite from a Bannerlord texture atlas PNG using its SpriteData XML metadata.

## Usage

```
/bannerlord-sprite-extract "<sprite name>" --atlas <atlas.png> --xml <SpriteData.xml> --output <dir>
```

- `<sprite name>` — e.g. `StdAssets\banner_flat` or `Order\SiegeIcons\falconet`
- `--atlas` — path to the atlas PNG (must be extracted from the .tpac first using TpacTool — see Prerequisites)
- `--xml` — path to the SpriteData XML that describes this atlas
- `--output` — directory where the extracted PNG will be saved

## Prerequisites

The atlas PNG must be extracted from its `.tpac` file before this skill can use it.
Use **TpacTool** (https://github.com/szszss/TpacTool/releases):
1. Open TpacTool → File → Open → select the `.tpac` file
2. Find the atlas texture (names start with `ui_`) → Export as PNG

## Instructions

The user has invoked this skill with: $ARGUMENTS

Parse the arguments above to extract:
- The sprite name (first positional argument, before any `--` flags; strip surrounding quotes)
- `--atlas` value
- `--xml` value
- `--output` value (if omitted, default to the directory containing the atlas file)

First verify Python 3 and Pillow are available:
```bash
python --version
python -c "from PIL import Image; print('Pillow OK')"
```
If Pillow is missing, install it:
```bash
pip install Pillow
```

Now run the following Python script, substituting the real values for SPRITE_NAME, ATLAS_PATH, XML_PATH, OUTPUT_DIR:

```python
import sys
import os
import xml.etree.ElementTree as ET
from PIL import Image

sprite_name = r"SPRITE_NAME"
atlas_path  = r"ATLAS_PATH"
xml_path    = r"XML_PATH"
output_dir  = r"OUTPUT_DIR"

# Find the SpritePart in the XML
tree = ET.parse(xml_path)
root = tree.getroot()

part = None
for sp in root.findall(".//SpritePart"):
    if (sp.findtext("Name") or "").strip() == sprite_name:
        part = sp
        break

if part is None:
    print(f"ERROR: Sprite '{sprite_name}' not found in {xml_path}")
    print("Tip: use /bannerlord-sprite-list to see available sprites.")
    sys.exit(1)

sheet_x = int(part.findtext("SheetX") or 0)
sheet_y = int(part.findtext("SheetY") or 0)
width   = int(part.findtext("Width")  or 0)
height  = int(part.findtext("Height") or 0)
category = (part.findtext("CategoryName") or "").strip()

print(f"Found: {sprite_name}")
print(f"  Category : {category}")
print(f"  Position : SheetX={sheet_x}, SheetY={sheet_y}")
print(f"  Size     : {width} x {height} px")

# Crop from atlas
img = Image.open(atlas_path)
box = (sheet_x, sheet_y, sheet_x + width, sheet_y + height)
cropped = img.crop(box)

# Build output filename — replace backslashes/slashes with underscores
safe_name = sprite_name.replace("\\", "_").replace("/", "_") + ".png"
os.makedirs(output_dir, exist_ok=True)
out_path = os.path.join(output_dir, safe_name)
cropped.save(out_path)

print(f"\nSaved: {out_path}  ({width}x{height} px)")
```

Pass the script to Python via the Bash tool.
Report the output path and dimensions to the user.
