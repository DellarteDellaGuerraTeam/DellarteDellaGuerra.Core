#!/usr/bin/env python3
"""Project Scottish datasource points into calibrated DADG map coordinates."""
from __future__ import annotations

import argparse
import csv
import io
import json
import math
import re
import sys
import unicodedata
import zipfile
from pathlib import Path
from xml.etree import ElementTree as ET

SKILL_DIR = Path(__file__).resolve().parents[1]
ASSETS = SKILL_DIR / "assets"
DEFAULT_SOURCES = [
    ASSETS / "crown-of-scotland-locations.kmz",
    ASSETS / "lordship-of-the-isles-locations.kmz",
]
DEFAULT_CONTROLS = ASSETS / "scotland-calibration.json"


def normalized(value: str) -> str:
    value = unicodedata.normalize("NFKD", value).encode("ascii", "ignore").decode()
    return re.sub(r"[^a-z0-9]+", "_", value.lower()).strip("_")


def local_name(tag: str) -> str:
    return tag.rsplit("}", 1)[-1]


def point_coordinates(placemark: ET.Element) -> tuple[float, float] | None:
    for element in placemark.iter():
        if local_name(element.tag) != "Point":
            continue
        for child in element.iter():
            if local_name(child.tag) == "coordinates" and child.text:
                fields = child.text.strip().split()[0].split(",")
                if len(fields) >= 2:
                    return float(fields[0]), float(fields[1])
    return None


def child_text(element: ET.Element, tag: str) -> str:
    for child in element:
        if local_name(child.tag) == tag:
            return (child.text or "").strip()
    return ""


def parse_kml_bytes(data: bytes, source: Path) -> list[dict]:
    root = ET.fromstring(data)
    records: list[dict] = []

    def walk(element: ET.Element, folders: list[str]) -> None:
        for child in element:
            tag = local_name(child.tag)
            if tag in {"Document", "kml"}:
                walk(child, folders)
            elif tag == "Folder":
                name = child_text(child, "name")
                walk(child, folders + ([name] if name else []))
            elif tag == "Placemark":
                coords = point_coordinates(child)
                if coords is None:
                    continue
                lon, lat = coords
                records.append({
                    "name": child_text(child, "name"),
                    "folder": " / ".join(folders),
                    "longitude": lon,
                    "latitude": lat,
                    "source": source.name,
                })

    walk(root, [])
    return records


def load_source(path: Path) -> list[dict]:
    suffix = path.suffix.lower()
    if suffix == ".kmz":
        with zipfile.ZipFile(path) as archive:
            member = next((n for n in archive.namelist() if n.lower().endswith(".kml")), None)
            if member is None:
                raise ValueError(f"No KML member in {path}")
            return parse_kml_bytes(archive.read(member), path)
    if suffix == ".kml":
        return parse_kml_bytes(path.read_bytes(), path)
    if suffix == ".json":
        payload = json.loads(path.read_text(encoding="utf-8"))
        rows = payload.get("settlements", payload) if isinstance(payload, dict) else payload
        result = []
        for row in rows:
            lon = row.get("longitude", row.get("lon"))
            lat = row.get("latitude", row.get("lat"))
            if lon is None or lat is None:
                continue
            result.append({
                "name": str(row.get("name", "")),
                "folder": str(row.get("folder", "")),
                "longitude": float(lon),
                "latitude": float(lat),
                "source": path.name,
            })
        return result
    raise ValueError(f"Unsupported datasource: {path}")


def solve3(matrix: list[list[float]], vector: list[float]) -> list[float]:
    augmented = [row[:] + [value] for row, value in zip(matrix, vector)]
    for col in range(3):
        pivot = max(range(col, 3), key=lambda row: abs(augmented[row][col]))
        if abs(augmented[pivot][col]) < 1e-12:
            raise ValueError("Calibration controls do not define a full affine fit")
        augmented[col], augmented[pivot] = augmented[pivot], augmented[col]
        pivot_value = augmented[col][col]
        augmented[col] = [value / pivot_value for value in augmented[col]]
        for row in range(3):
            if row == col:
                continue
            factor = augmented[row][col]
            augmented[row] = [a - factor * b for a, b in zip(augmented[row], augmented[col])]
    return [augmented[row][3] for row in range(3)]


def least_squares(controls: list[dict], target: str) -> list[float]:
    ata = [[0.0] * 3 for _ in range(3)]
    atb = [0.0] * 3
    for control in controls:
        row = [float(control["longitude"]), float(control["latitude"]), 1.0]
        for i in range(3):
            atb[i] += row[i] * float(control[target])
            for j in range(3):
                ata[i][j] += row[i] * row[j]
    return solve3(ata, atb)


def project(longitude: float, latitude: float, cx: list[float], cy: list[float]) -> tuple[float, float]:
    row = [float(longitude), float(latitude), 1.0]
    return sum(a * b for a, b in zip(cx, row)), sum(a * b for a, b in zip(cy, row))


def fit_controls(path: Path) -> tuple[dict, list[dict]]:
    payload = json.loads(path.read_text(encoding="utf-8"))
    controls = payload["controls"]
    if len(controls) < 3:
        raise ValueError("At least three calibration controls are required")
    cx = least_squares(controls, "map_x")
    cy = least_squares(controls, "map_y")
    residuals = []
    for control in controls:
        x, y = project(control["longitude"], control["latitude"], cx, cy)
        residuals.append({
            "id": control["id"],
            "name": control["source_name"],
            "error": math.hypot(x - control["map_x"], y - control["map_y"]),
        })
    report = {
        "control_count": len(controls),
        "map_x_coefficients": cx,
        "map_y_coefficients": cy,
        "mean_residual": sum(r["error"] for r in residuals) / len(residuals),
        "max_residual": max(r["error"] for r in residuals),
    }
    return report, residuals


def deduplicate(records: list[dict]) -> list[dict]:
    unique = {}
    for record in records:
        key = (normalized(record["name"]), round(record["longitude"], 7), round(record["latitude"], 7))
        unique.setdefault(key, record)
    return list(unique.values())


def select_records(records: list[dict], selectors: list[str], all_records: bool) -> list[dict]:
    if all_records:
        return records
    selected = []
    for selector in selectors:
        key = normalized(selector)
        exact = [record for record in records if normalized(record["name"]) == key]
        matches = exact or [record for record in records if key in normalized(record["name"])]
        if not matches:
            raise ValueError(f"No datasource record matches {selector!r}")
        distinct = {(round(r["longitude"], 7), round(r["latitude"], 7)) for r in matches}
        if len(distinct) > 1:
            names = ", ".join(f'{r["source"]}:{r["folder"]}:{r["name"]}' for r in matches)
            raise ValueError(f"Ambiguous selector {selector!r}: {names}")
        selected.append(matches[0])
    return deduplicate(selected)


def write_csv(rows: list[dict], stream: io.TextIOBase) -> None:
    fields = ["name", "folder", "source", "longitude", "latitude", "map_x", "map_y", "on_map"]
    writer = csv.DictWriter(stream, fieldnames=fields, extrasaction="ignore", lineterminator="\n")
    writer.writeheader()
    writer.writerows(rows)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source", action="append", type=Path, help="KMZ, KML, or JSON datasource; repeatable")
    parser.add_argument("--controls", type=Path, default=DEFAULT_CONTROLS)
    parser.add_argument("--name", action="append", default=[], help="Exact name preferred; repeatable")
    parser.add_argument("--all", action="store_true", help="Project all point records")
    parser.add_argument("--list", action="store_true", help="List datasource names without projecting")
    parser.add_argument("--calibration-report", action="store_true")
    parser.add_argument("--format", choices=("json", "csv"), default="json")
    parser.add_argument("--output", type=Path)
    args = parser.parse_args()

    report, residuals = fit_controls(args.controls)
    if args.calibration_report:
        print(json.dumps({"calibration": report, "residuals": residuals}, indent=2))
        if not args.name and not args.all and not args.list:
            return 0

    sources = args.source or DEFAULT_SOURCES
    records = deduplicate([record for source in sources for record in load_source(source)])
    records.sort(key=lambda row: (normalized(row["name"]), row["source"], row["folder"]))
    if args.list:
        for record in records:
            print(f'{record["name"]}\t{record["folder"]}\t{record["source"]}')
        return 0
    if not args.name and not args.all:
        parser.error("provide at least one --name, or use --all/--list/--calibration-report")

    cx = report["map_x_coefficients"]
    cy = report["map_y_coefficients"]
    selected = select_records(records, args.name, args.all)
    rows = []
    for record in selected:
        x, y = project(record["longitude"], record["latitude"], cx, cy)
        rows.append({
            **record,
            "map_x": round(x, 3),
            "map_y": round(y, 3),
            "on_map": 0.0 <= x <= 1120.0 and 0.0 <= y <= 1120.0,
        })

    if args.format == "json":
        rendered = json.dumps({"calibration": report, "settlements": rows}, ensure_ascii=False, indent=2) + "\n"
        if args.output:
            args.output.write_text(rendered, encoding="utf-8", newline="\n")
        else:
            sys.stdout.write(rendered)
    elif args.output:
        with args.output.open("w", encoding="utf-8", newline="") as stream:
            write_csv(rows, stream)
    else:
        write_csv(rows, sys.stdout)
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except (OSError, ValueError, KeyError, zipfile.BadZipFile) as exc:
        print(f"error: {exc}", file=sys.stderr)
        raise SystemExit(2)
