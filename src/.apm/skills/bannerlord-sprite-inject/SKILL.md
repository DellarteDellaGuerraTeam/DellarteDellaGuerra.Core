---
name: bannerlord-sprite-inject
description: Place a custom PNG sprite into a Bannerlord mod's SpriteParts folder and register it in the mod's SpriteData XML with automatically computed atlas coordinates using strip packing. Use this skill whenever the user wants to add, inject, or register a new custom sprite into a Bannerlord mod, or says things like "inject sprite", "add sprite to the mod", "register sprite", "add custom sprite", "put this PNG into the mod", or "add this image to the atlas".
---

# bannerlord-sprite-inject

Place a custom PNG sprite into a mod's SpriteParts folder and register it in the mod's SpriteData XML with automatically computed atlas coordinates.

## Usage

```
/bannerlord-sprite-inject "<sprite name>" --png <source.png> --spritedata <ModSpriteData.xml> --spriteparts <SpriteParts dir> [--category <category name>]
```

- `<sprite name>` — e.g. `SPGeneral\Siege\culverin` (backslash-separated namespace path)
- `--png` — path to the custom source PNG (must already exist at the correct pixel dimensions)
- `--spritedata` — path to the mod's SpriteData XML (e.g. `DadgCannonsSpriteData.xml`)
- `--spriteparts` — path to the mod's SpriteParts root folder (e.g. `GUI\SpriteParts`)
- `--category` — sprite category name (e.g. `dadg_ui_cannon`); if omitted, reads the first category from the SpriteData XML

## After This Skill Runs

You must manually repack the atlas:
1. Run **SpriteSheetGenerator.exe** from the Bannerlord Modding Kit:
   `C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\bin\Win64_Shipping_wEditor\TaleWorlds.TwoDimension.SpriteSheetGenerator.exe`
2. In the Bannerlord Modding Kit resource browser, choose "Scan new asset files" to import the rebuilt atlas
3. The compiled `.tpac` will be regenerated in your module's `Assets\GauntletUI\` folder

## Instructions

The user has invoked this skill with: $ARGUMENTS

Parse the arguments to extract:
- The sprite name (first positional argument before any `--` flag; strip surrounding quotes)
- `--png` value
- `--spritedata` value
- `--spriteparts` value
- `--category` value (optional)

First verify Python 3 and Pillow are available:
```powershell
python --version
python -c "from PIL import Image; print('Pillow OK')"
```
If Pillow is missing, install it:
```powershell
pip install Pillow
```

Now run the following Python script, substituting real values for SPRITE_NAME, PNG_PATH, SPRITEDATA_PATH, SPRITEPARTS_DIR, CATEGORY_NAME (empty string if not provided):

```python
import sys
import os
import shutil
import xml.etree.ElementTree as ET
from PIL import Image

sprite_name    = r"SPRITE_NAME"
png_path       = r"PNG_PATH"
spritedata     = r"SPRITEDATA_PATH"
spriteparts    = r"SPRITEPARTS_DIR"
category_arg   = r"CATEGORY_NAME"

# --- Read source PNG dimensions ---
src_img = Image.open(png_path)
width, height = src_img.size
print(f"Source PNG: {width} x {height} px")

# --- Parse SpriteData XML ---
ET.register_namespace("", "")
tree = ET.parse(spritedata)
root = tree.getroot()

# Resolve category
category = category_arg
if not category:
    cat_el = root.find(".//SpriteCategory/Name")
    if cat_el is None:
        print("ERROR: Could not determine category. Pass --category explicitly.")
        sys.exit(1)
    category = cat_el.text.strip()
print(f"Category: {category}")

# Get atlas dimensions for this category (SheetID=1 by default)
atlas_w, atlas_h = 1024, 1024
for sc in root.findall(".//SpriteCategory"):
    name_el = sc.find("Name")
    if name_el is not None and name_el.text.strip() == category:
        for ss in sc.findall("SpriteSheetSize"):
            if ss.get("ID", "1") == "1":
                atlas_w = int(ss.get("Width", atlas_w))
                atlas_h = int(ss.get("Height", atlas_h))
        break
print(f"Atlas size: {atlas_w} x {atlas_h}")

# Check for duplicate
existing_names = [
    (sp.findtext("Name") or "").strip()
    for sp in root.findall(".//SpritePart")
]
if sprite_name in existing_names:
    print(f"WARNING: SpritePart '{sprite_name}' already exists in {spritedata}. Skipping XML update.")
    sys.exit(0)

# --- Compute non-overlapping SheetX/SheetY using strip packing ---
PADDING = 4
occupied = []
for sp in root.findall(".//SpritePart"):
    if (sp.findtext("CategoryName") or "").strip() != category:
        continue
    sx = int(sp.findtext("SheetX") or 0)
    sy = int(sp.findtext("SheetY") or 0)
    sw = int(sp.findtext("Width")  or 0)
    sh = int(sp.findtext("Height") or 0)
    occupied.append((sx, sy, sx + sw, sy + sh))

def fits(x, y, w, h, occupied, aw, ah):
    if x + w > aw or y + h > ah:
        return False
    for (ox1, oy1, ox2, oy2) in occupied:
        if not (x + w <= ox1 or x >= ox2 or y + h <= oy1 or y >= oy2):
            return False
    return True

found_x, found_y = None, None
y = PADDING
while y + height <= atlas_h:
    x = PADDING
    while x + width <= atlas_w:
        if fits(x, y, width, height, occupied, atlas_w, atlas_h):
            found_x, found_y = x, y
            break
        x += 1
    if found_x is not None:
        break
    y += 1

if found_x is None:
    print(f"ERROR: No free space for {width}x{height} sprite in {atlas_w}x{atlas_h} atlas.")
    print("You need to create a new SpriteCategory with a larger or additional atlas sheet.")
    sys.exit(1)

print(f"Assigned position: SheetX={found_x}, SheetY={found_y}")

# --- Copy PNG to SpriteParts folder ---
rel_path = sprite_name.replace("\\", os.sep).replace("/", os.sep) + ".png"
dest_dir = os.path.join(spriteparts, category, os.path.dirname(rel_path))
dest_file = os.path.join(spriteparts, category, rel_path)
os.makedirs(dest_dir, exist_ok=True)
shutil.copy2(png_path, dest_file)
print(f"Copied PNG to: {dest_file}")

# --- Update SpriteData XML ---
parts_el = root.find("SpriteParts")
if parts_el is None:
    parts_el = ET.SubElement(root, "SpriteParts")

sp_el = ET.SubElement(parts_el, "SpritePart")
for tag, val in [
    ("SheetID", "1"),
    ("Name", sprite_name),
    ("Width", str(width)),
    ("Height", str(height)),
    ("SheetX", str(found_x)),
    ("SheetY", str(found_y)),
    ("CategoryName", category),
]:
    ET.SubElement(sp_el, tag).text = val

sprites_el = root.find("Sprites")
if sprites_el is None:
    sprites_el = ET.SubElement(root, "Sprites")

gs_el = ET.SubElement(sprites_el, "GenericSprite")
ET.SubElement(gs_el, "Name").text = sprite_name
ET.SubElement(gs_el, "SpritePartName").text = sprite_name

ET.indent(tree, space="  ")
tree.write(spritedata, encoding="utf-8", xml_declaration=True)
print(f"\nUpdated: {spritedata}")
print(f"\nNext step: run SpriteSheetGenerator.exe to repack the atlas into a .tpac")
```

Pass the script to Python via PowerShell.
Show the user the output, confirm what was changed, and remind them of the manual repack step.
