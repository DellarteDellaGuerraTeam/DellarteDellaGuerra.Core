#!/usr/bin/env python3
"""Render a read-only top-down Bannerlord navmesh preview over scene terrain."""
from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path

DEFAULT_SCENE = Path(
    r"D:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord\Modules"
    r"\DellarteDellaGuerraMap\SceneObj\Main_map"
)
DEFAULT_STUDIO = Path(r"C:\Users\Joe\Desktop\bannerlord-navmesh-studio")
WATER_RE = re.compile(r'<property\s+name="water_level"\s+value="([^"]+)"')


def read_water_level(path: Path) -> float | None:
    match = WATER_RE.search(path.read_text(encoding="utf-8", errors="replace"))
    return float(match.group(1)) if match else None


def bilinear(heights, world_x, world_y, xs, ys):
    import numpy as np

    rows, cols = heights.shape
    gx = np.clip(xs / world_x * (cols - 1), 0, cols - 1)
    gy = np.clip(ys / world_y * (rows - 1), 0, rows - 1)
    x0, y0 = gx.astype(int), gy.astype(int)
    x1, y1 = np.minimum(x0 + 1, cols - 1), np.minimum(y0 + 1, rows - 1)
    tx, ty = gx - x0, gy - y0
    return (
        heights[y0, x0] * (1 - tx) * (1 - ty)
        + heights[y0, x1] * tx * (1 - ty)
        + heights[y1, x0] * (1 - tx) * ty
        + heights[y1, x1] * tx * ty
    )


def render(args: argparse.Namespace) -> None:
    scene = args.scene.resolve()
    navmesh = (args.navmesh or scene / "navmesh.bin").resolve()
    xscene = scene / "scene.xscene"
    terrain_path = scene / "terrain.bin"
    for path in (navmesh, xscene, terrain_path):
        if not path.is_file():
            raise FileNotFoundError(path)
    if not args.studio.is_dir():
        raise FileNotFoundError(f"Bannerlord Navmesh Studio not found: {args.studio}")

    sys.path.insert(0, str(args.studio.resolve()))
    from blnav import nmg9, rnm1
    from blnav.terrain import Terrain
    from blnav.xscene import read_terrain_params

    import matplotlib

    matplotlib.use("Agg")
    import matplotlib.pyplot as plt
    import numpy as np
    from matplotlib.collections import PolyCollection
    from matplotlib.colors import LinearSegmentedColormap
    from matplotlib.patches import Patch

    mesh = nmg9.NavMesh.parse(rnm1.read(str(navmesh)))
    params = read_terrain_params(str(xscene))
    raw_grid = Terrain.load(str(terrain_path)).heightmap_grid(
        (params.node_dim_x, params.node_dim_y)
    )
    heights = params.decode_heights(raw_grid)
    x0, x1, y0, y1 = args.bounds
    if x0 >= x1 or y0 >= y1:
        raise ValueError("bounds must satisfy X0 < X1 and Y0 < Y1")

    xs, ys = np.meshgrid(
        np.linspace(x0, x1, args.samples), np.linspace(y0, y1, args.samples)
    )
    sampled = bilinear(
        heights, params.world_size_x, params.world_size_y, xs.ravel(), ys.ravel()
    ).reshape(xs.shape)
    water = read_water_level(xscene)

    polygons = [np.asarray([mesh.vertices[v][:2] for v in face.vertex_ids]) for face in mesh.faces]
    visible = [
        index
        for index, polygon in enumerate(polygons)
        if polygon[:, 0].max() >= x0
        and polygon[:, 0].min() <= x1
        and polygon[:, 1].max() >= y0
        and polygon[:, 1].min() <= y1
    ]

    fig, ax = plt.subplots(figsize=(12, 12))
    low, high = float(sampled.min()), float(sampled.max())
    land_floor = water if water is not None else low
    land = np.ma.masked_less_equal(sampled, land_floor) if water is not None else sampled
    land_high = high if high > land_floor else land_floor + 1.0
    land_levels = np.linspace(land_floor, land_high, 14)
    land_cmap = LinearSegmentedColormap.from_list(
        "land_elevation", ["#dcebc5", "#a8bd82", "#9a7d61", "#ded8cf", "#fafafa"]
    )
    if water is not None:
        ax.contourf(xs, ys, sampled, levels=[-np.inf, water], colors=["#b9ddec"])
        if low < water < high:
            ax.contour(xs, ys, sampled, levels=[water], colors=["#286d98"], linewidths=0.8)
    ax.contourf(xs, ys, land, levels=land_levels, cmap=land_cmap)

    face_colors = [
        "#7b2cbf80" if mesh.faces[index].g3 == 7
        else "#2667d980" if mesh.faces[index].g3 == 8
        else "#ffffff00"
        for index in visible
    ]
    ax.add_collection(
        PolyCollection(
            [polygons[index] for index in visible],
            facecolors=face_colors,
            edgecolors="#20202099",
            linewidths=0.45,
        )
    )
    vertex_ids = sorted({vertex for index in visible for vertex in mesh.faces[index].vertex_ids})
    if vertex_ids:
        vertices = np.asarray([mesh.vertices[index][:2] for index in vertex_ids])
        ax.scatter(vertices[:, 0], vertices[:, 1], s=4, c="#d7191c", linewidths=0, zorder=4)

    ax.set(
        xlim=(x0, x1),
        ylim=(y0, y1),
        aspect="equal",
        xlabel="world X (m)",
        ylabel="world Y (m)",
        title="Offline navmesh preview: purple Mountain, blue Lake, red vertices",
    )
    ax.grid(alpha=0.15)
    legend_items = [
        Patch(facecolor="#b3c88e", label="Terrain elevation (land ramp)"),
        Patch(facecolor="#7b2cbf80", label="Navmesh Mountain (g3=7)"),
        Patch(facecolor="#2667d980", label="Navmesh Lake (g3=8)"),
    ]
    if water is not None:
        legend_items.insert(1, Patch(facecolor="#b9ddec", label=f"Terrain water (height ≤ {water:g} m)"))
    ax.legend(handles=legend_items, loc="upper left", framealpha=0.92)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    fig.savefig(args.output, dpi=args.dpi, bbox_inches="tight")
    plt.close(fig)
    print(
        f"Wrote {args.output.resolve()} ({len(visible)} visible faces, "
        f"{len(vertex_ids)} visible vertices, water={water!r})"
    )


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("scene", nargs="?", type=Path, default=DEFAULT_SCENE)
    parser.add_argument("--navmesh", type=Path, help="candidate navmesh.bin (defaults to scene/navmesh.bin)")
    parser.add_argument("--bounds", nargs=4, required=True, type=float, metavar=("X0", "X1", "Y0", "Y1"))
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--studio", type=Path, default=DEFAULT_STUDIO)
    parser.add_argument("--samples", type=int, default=700)
    parser.add_argument("--dpi", type=int, default=150)
    args = parser.parse_args()
    if args.samples < 2:
        parser.error("--samples must be at least 2")
    return args


if __name__ == "__main__":
    render(parse_args())
