---
name: bannerlord-sprite-list
description: List all sprites defined in a Bannerlord SpriteData XML file, with optional name filtering, showing sprite name, category, dimensions, and atlas sheet position. Use this skill whenever the user wants to browse or search sprites in a SpriteData XML, find out what sprites exist in a mod or vanilla atlas, or says things like "list sprites", "what sprites are in this xml", "find sprite by name", "show all sprites in", "search sprites", or "what's in the SpriteData file".
---

# bannerlord-sprite-list

List all sprites defined in a Bannerlord SpriteData XML file.

## Usage

```
/bannerlord-sprite-list --xml <path> [--filter <substring>]
```

- `--xml` — path to a SpriteData XML file (vanilla `NativeSpriteData.xml` or a mod file like `DadgCannonsSpriteData.xml`)
- `--filter` — optional case-insensitive substring to filter sprite names

## Instructions

The user has invoked this skill with: $ARGUMENTS

Parse the arguments above to extract `--xml` and (optionally) `--filter`.

First check Python is available:
```powershell
python --version
```
If Python is not found, stop and tell the user to install Python 3 from https://python.org.

Then check Pillow (needed for consistency with other sprite skills, though this skill only uses stdlib):
```powershell
python -c "import xml.etree.ElementTree" 2>&1
```

Now run the following Python script via PowerShell, substituting the actual path for XML_PATH and the filter string for FILTER_VALUE (empty string if not provided):

```python
import sys
import xml.etree.ElementTree as ET

xml_path = r"XML_PATH"
name_filter = "FILTER_VALUE".lower()

tree = ET.parse(xml_path)
root = tree.getroot()

parts = []
for sp in root.findall(".//SpritePart"):
    name = (sp.findtext("Name") or "").strip()
    category = (sp.findtext("CategoryName") or "").strip()
    width = (sp.findtext("Width") or "?").strip()
    height = (sp.findtext("Height") or "?").strip()
    sheet_x = (sp.findtext("SheetX") or "?").strip()
    sheet_y = (sp.findtext("SheetY") or "?").strip()
    sheet_id = (sp.findtext("SheetID") or "1").strip()
    if name_filter and name_filter not in name.lower():
        continue
    parts.append((name, category, width, height, sheet_x, sheet_y, sheet_id))

if not parts:
    print("No sprites found" + (f" matching '{name_filter}'" if name_filter else "") + ".")
    sys.exit(0)

# Column widths
w_name = max(len("Sprite Name"), max(len(p[0]) for p in parts))
w_cat  = max(len("Category"),    max(len(p[1]) for p in parts))
w_dim  = max(len("Size"),        max(len(f"{p[2]}x{p[3]}") for p in parts))
w_pos  = max(len("Position"),    max(len(f"({p[4]},{p[5]})") for p in parts))

header = f"{'Sprite Name':{w_name}}  {'Category':{w_cat}}  {'Size':{w_dim}}  {'Position':{w_pos}}  Sheet"
sep    = "-" * len(header)
print(f"\n{header}")
print(sep)
for name, cat, w, h, sx, sy, sid in parts:
    dim = f"{w}x{h}"
    pos = f"({sx},{sy})"
    print(f"{name:{w_name}}  {cat:{w_cat}}  {dim:{w_dim}}  {pos:{w_pos}}  {sid}")

print(sep)
print(f"Total: {len(parts)} sprite(s)")
```

Pass the script to Python via PowerShell, either by writing it to a temp file or by using `python -c`.
After running, show the output to the user.
