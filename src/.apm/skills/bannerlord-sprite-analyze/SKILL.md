---
name: bannerlord-sprite-analyze
description: Show a visual text-art occupancy map of a Bannerlord sprite atlas PNG — which pixel regions are claimed by sprites and which are free — along with a sorted sprite listing and largest free-space regions. Use this skill whenever the user wants to inspect atlas occupancy, check how much free space is available in a sprite atlas, visualize sprite layout, or says things like "show atlas occupancy", "what space is free in the atlas", "analyze the sprite atlas", "how full is my atlas", or "where can I fit a new sprite".
---

# bannerlord-sprite-analyze

Show a visual occupancy map of a Bannerlord sprite atlas — which pixels are claimed by sprites and which are free.

## Usage

```
/bannerlord-sprite-analyze --atlas <atlas.png> --xml <SpriteData.xml>
```

- `--atlas` — path to the atlas PNG (must be extracted from .tpac via TpacTool first)
- `--xml` — path to the SpriteData XML that describes this atlas

## Prerequisites

The atlas PNG must first be extracted from its `.tpac` using **TpacTool** (https://github.com/szszss/TpacTool/releases):
1. Open TpacTool → File → Open → select the `.tpac`
2. Export the atlas texture as PNG

## Instructions

The user has invoked this skill with: $ARGUMENTS

Parse the arguments to extract `--atlas` and `--xml`.

First verify Python 3 and Pillow are available:
```powershell
python --version
python -c "from PIL import Image; print('Pillow OK')"
```
If Pillow is missing, install it:
```powershell
pip install Pillow
```

Now run the following Python script, substituting real values for ATLAS_PATH and XML_PATH:

```python
import sys
import xml.etree.ElementTree as ET
from PIL import Image

atlas_path = r"ATLAS_PATH"
xml_path   = r"XML_PATH"

# Load atlas dimensions
img = Image.open(atlas_path)
atlas_w, atlas_h = img.size
print(f"Atlas: {atlas_path}")
print(f"Actual PNG size: {atlas_w} x {atlas_h} px")

# Parse sprite parts
tree = ET.parse(xml_path)
root = tree.getroot()
parts = []
for sp in root.findall(".//SpritePart"):
    name     = (sp.findtext("Name")         or "").strip()
    category = (sp.findtext("CategoryName") or "").strip()
    try:
        sx = int(sp.findtext("SheetX") or 0)
        sy = int(sp.findtext("SheetY") or 0)
        sw = int(sp.findtext("Width")  or 0)
        sh = int(sp.findtext("Height") or 0)
    except ValueError:
        continue
    parts.append((name, category, sx, sy, sw, sh))

total_sprite_px = sum(w * h for _, _, _, _, w, h in parts)
total_atlas_px  = atlas_w * atlas_h
coverage = total_sprite_px / total_atlas_px * 100 if total_atlas_px else 0

print(f"Sprites defined: {len(parts)}")
print(f"Sprite pixel coverage: {total_sprite_px:,} / {total_atlas_px:,} px  ({coverage:.1f}%)")

# --- Build text-art occupancy grid ---
GRID_W = 64  # characters wide
GRID_H = 32  # characters tall

cell_w = atlas_w / GRID_W
cell_h = atlas_h / GRID_H

# Mark occupied cells
grid = [['.' for _ in range(GRID_W)] for _ in range(GRID_H)]
for (name, cat, sx, sy, sw, sh) in parts:
    # Convert sprite rect to grid cells
    gx1 = int(sx / cell_w)
    gy1 = int(sy / cell_h)
    gx2 = min(GRID_W - 1, int((sx + sw - 1) / cell_w))
    gy2 = min(GRID_H - 1, int((sy + sh - 1) / cell_h))
    for gy in range(gy1, gy2 + 1):
        for gx in range(gx1, gx2 + 1):
            grid[gy][gx] = '#'

print(f"\nOccupancy map  (# = used, . = free)  each cell ≈ {cell_w:.0f}x{cell_h:.0f}px")
print("+" + "-" * GRID_W + "+")
for row in grid:
    print("|" + "".join(row) + "|")
print("+" + "-" * GRID_W + "+")

# --- Sprite list sorted by position ---
print(f"\n{'Sprite Name':<50}  {'Category':<20}  {'Size':>10}  {'Position':>15}")
print("-" * 100)
for name, cat, sx, sy, sw, sh in sorted(parts, key=lambda p: (p[3], p[2])):
    size = f"{sw}x{sh}"
    pos  = f"({sx},{sy})"
    print(f"{name:<50}  {cat:<20}  {size:>10}  {pos:>15}")

# --- Free regions (row-based scan) ---
print(f"\nLargest free horizontal strips:")
free_rows = []
for gy in range(GRID_H):
    run_start = None
    for gx in range(GRID_W):
        if grid[gy][gx] == '.' and run_start is None:
            run_start = gx
        elif grid[gy][gx] == '#' and run_start is not None:
            free_rows.append((gy, run_start, gx - 1))
            run_start = None
    if run_start is not None:
        free_rows.append((gy, run_start, GRID_W - 1))

# Show the 5 largest free strips
free_rows.sort(key=lambda r: -(r[2] - r[1] + 1))
for (gy, gx1, gx2) in free_rows[:5]:
    px_x1 = int(gx1 * cell_w)
    px_y1 = int(gy  * cell_h)
    px_x2 = int((gx2 + 1) * cell_w)
    px_y2 = int((gy  + 1) * cell_h)
    print(f"  Row {gy:2d}  cols {gx1}-{gx2}  ≈ pixel rect ({px_x1},{px_y1})-({px_x2},{px_y2})  width≈{px_x2-px_x1}px")
```

Pass the script to Python via PowerShell.
Present the output (grid map, sprite list, free space summary) to the user.
