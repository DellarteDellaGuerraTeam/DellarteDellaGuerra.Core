# Political Map: Territories Painted on the Campaign Map

*Status: proposal, 2026-10-27. Nothing here is implemented yet.*

## Goal

The live 3D campaign map itself shows the feudal state, always, with no menu to open. You
zoom out and see England carved into duchies and counties, each one washed in its holder's
colour and carrying the holder's banner, like a period map of the realm. Contested land is
visibly contested.

```
   zoomed out (strategic)                     zoomed in (normal play)
 ┌───────────────────────────────┐        ┌───────────────────────────────┐
 │ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━│        │   terrain as normal, a faint  │
 │ ┃░░░ [⚑York] ░░░┃▓▓▓▓▓▓▓▓▓▓▓▓▓│        │   wash and thin county        │
 │ ┃░ Duchy of York░┃▓[⚑Percy]▓▓▓│        │   borders under the parties   │
 │ ┃░░░┄┄┄┄┄┄░░░░░░┃▓▓▓▓▓▓▓▓▓▓▓▓▓│        │                               │
 │ ┃▒▒▒▒▒▒▒▒╱╲╱╲╱╲┃━━━━━━━━━━━━━━│        │   ━━ duchy border             │
 │ ┃▒[⚑Lanc]╱╲Durham (contested)  │        │   ┄┄ county border            │
 │ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━│        │                               │
 └───────────────────────────────┘        └───────────────────────────────┘
   ━ duchy / realm border   ┄ county border   ╱╲ hatched = occupied by a rival
```

## Engine check (1.4.8 source)

Everything needed is a public API that vanilla already uses on the map scene:

| Need | API | Vanilla precedent |
|---|---|---|
| Paint onto map terrain | `Decal.CreateDecal`, `GameEntity.AddComponent(decal)`, `Scene.AddDecalInstance` | `SettlementVisualManager` siege-engine circles, `MapCursor`, `MapScreen` |
| Tint a decal | `Decal.SetFactor1` / `SetFactor1Linear` / `SetAlpha` | the same siege circles |
| Runtime-generated texture | `Texture.CreateFromByteArray(byte[], w, h)` | `AvatarThumbnailCache` |
| Put that texture on a decal | `Material.CreateCopy()` + `SetTexture(...)`, `Decal.SetMaterial` | |
| Show/hide | `Decal.SetIsVisible` | |

(`MBMapScene.SetPoliticalColor` also exists natively, but its only managed wrapper is a
misnamed `GetGlobalIlluminationOfString` and nothing calls it. It looks like a dead TaleWorlds
feature, so don't rely on it.)

## Design

### 1. The territory wash (one texture, regenerated at runtime)

- **Offline bake (once):** a *settlement*-ID raster of the whole campaign map, e.g.
  2048×2048 with two bytes per pixel giving the settlement index. Each land pixel belongs to
  its nearest settlement (Voronoi). Water is 0. Which county a settlement belongs to, and who
  owns it, are looked up at runtime: through the de jure tree, villages through their bound
  town or castle. So both views and the fog share one raster. The land mask comes from the
  map navmesh's land/water faces, which the Navmesh Studio tool already reads. The output is
  `settlement_ids.bin` plus an index-to-settlement-id JSON file, shipped in the module.
  The de jure tree never changes shape, so this is baked once. It only needs rerunning if a
  settlement moves.
- **Runtime compose:** walk the ID raster once to build an RGBA texture.
  - **Fill:** the county holder's clan colour (banner primary), at low alpha.
  - **Borders:** a pixel whose neighbour has a different ID becomes a border pixel. Its
    thickness and darkness depend on the highest rank the two counties do *not* share:
    realm > duchy > county. Duchy borders then read as thick lines without a separate asset.
  - **Contested county:** diagonal stripes in holder and occupant colours.
  - **Vacant county:** grey, unfilled.
  - **Lookup tables:** ID → colour, and pairs of IDs → border rank, so composing is one pass
    over about 4M bytes, a few milliseconds. Recompose only when a title or holder changes:
    hook the feudal title change events, with a daily tick as a backstop.
- **Display:** `Texture.CreateFromByteArray` → a copied decal material → one decal stretched
  over the map. If one decal can't span the whole map (an unknown, see the spike), split it
  into a 4×4 grid of decals, each sampling its own sub-texture.

### 2. Banners on the land

Heraldry is painted onto the ground at each territory's centroid, like an illuminated map.
- Each banner is a decal whose material carries the holder's banner texture, taken from the
  banner tableau that the encyclopedia already renders.
- Size follows rank: the duke's banner is large and centred on the duchy, county banners are
  smaller. A contested county shows the occupant's banner small beside the holder's.
- Centroids are baked offline alongside the ID raster.

### 3. Zoom drives the detail

There is no toggle. A `MapView` reads camera height every tick:

| Camera | Wash alpha | Borders | Banners |
|---|---|---|---|
| Far (strategic) | strong | realm + duchy + county | duchy (large) + county |
| Mid | medium | duchy + county | county |
| Near (normal play) | faint or off | duchy only, thin | none |

Optional later: Alt held down forces the strategic layer at any zoom.

## Build order

1. **Spike, about 2 hours (go/no-go).** A `MapView` adds one decal over the whole map with a
   `CreateFromByteArray` checkerboard. This answers three questions:
   - Does the decal project cleanly over hills and coast?
   - What is the maximum size of a single decal?
   - Which stock decal material can be copied that samples diffuse × factor with alpha blend?

   If it fails, the fallback is flat textured meshes laid on terrain height samples, which
   looks worse but works.
2. **Offline bake script:** `tools/political_map_bake.py` writes the ID raster, centroids and
   index, then render a PNG preview to check the county shapes before going in game.
   *Superseded 2026-10-30: the mod builds the raster itself at campaign load, see
   "Settlement grid built in game" below. The script stays as an offline preview tool.*
3. **Wash + borders** with zoom fading.
4. **Banners.**
5. **Contested hatching** and event-driven refresh.

The game-side code is a `MapView`, a pure-C# composer (unit-testable in Domain: an ID raster
plus a holder table in, an RGBA byte array out) and a settlement → county lookup built from
`FeudalMap`. No changes to the feudal logic.

## Decisions (2026-10-27)

- **Always live, never a one-shot paint.** Section 1 is revised: the offline bake stores a
  *settlement*-ID raster, not a county-ID raster. Everything the player sees is composed at
  runtime from current game state, and recomposed whenever any of these fires:
  - a feudal title changes holder, or its contested occupant changes;
  - `OnSettlementOwnerChanged`;
  - `OnClanChangedKingdom`, kingdom created or destroyed;
  - war or peace declared (for the Kingdom view's war hatching);
  - fog reveal (below).

  Recomposing is throttled to at most once per second of real time. The texture is replaced
  by creating a new one, swapping it into the material, then releasing the old one. One
  compose is about 4M bytes through lookup tables, so a few ms.
- **Scotland and the Isles** are painted too. The raster covers the whole map.
- **Two views, switched by a toggle** (a hotkey, plus a small button on the map bar).
  *Superseded 2026-10-30: three views plus Off, see "Overlay as built" below.*
  - **Feudal (de jure).** Each settlement cell takes its *county's* holder clan colour.
    Borders follow realm > duchy > county rank. Banners are the duke's and counts'.
    Contested counties are hatched in holder and occupant colours.
  - **Kingdom (de facto).** Each settlement cell takes the colour of the kingdom that
    actually controls it (`Settlement.MapFaction`). Borders follow kingdom boundaries, and
    the banner is the kingdom's, one per contiguous block. Cells under siege, or at war with
    the player's kingdom, get hatching. This is the view that shows York against Lancaster
    as it really stands, because de jure England crosses both kingdoms (see the private-war
    sides note).
  - A third zoom-free state, **Off**, hides everything.
- **Fill colour.** In the Feudal view, the fill is the holder clan's own colour (`Clan.Color`,
  the banner's background). That is the colour players already associate with each house on
  its banner and its parties. A clan holding three neighbouring counties then reads as one
  block, which is the point: it shows a house's power. County borders still separate the
  counties. Vacant titles are unpainted parchment grey. In the Kingdom view, the fill is
  `Kingdom.Color`. Colours are drawn at about 35% alpha so the terrain still reads, and
  borders are drawn darker and opaque.
- **Fog of war** *(superseded 2026-10-30: cut. There is no fog, no parchment wash, no
  reveal-all switch and no saved revealed set; every settlement shows live state.)*

## Reviving `set_political_color`

The native call takes a single string and has no caller anywhere in 1.4.8, so its meaning
can only be learned by experiment. The spike exposes it as `polmap.political_color <value>`.
Planned probes, watching the map after each one:
- a hex colour (`FF0000`);
- a kingdom or clan id;
- a texture name (the sibling `GetColorGradeGridData` takes a texture name);
- an empty string.

If it turns out to drive a native territory tint, View 1's wash could use it instead of a
decal. If nothing visible happens, the decal plan stands.

**Result (2026-10-27, live on 1.4.8): dead.** Called on the campaign map with `FF0000`,
`0xFFFF0000`, `1.0 0.0 0.0 1.0`, `white`, `1`, `vlandia`, `political_map` and
`white_texture`. None changed the map: screenshot diffs against the baseline differ in at
most 0.15% of pixels, all scattered animation and JPEG noise. Nothing appeared in the engine
`rgl_log`, and nothing crashed. The empty string was not tried. The native side is most likely an empty stub
left over from a removed feature. The decal plan stands.

## Decal spike result (2026-10-27, live on 1.4.8): runtime textures don't render

These were bisected with `polmap.probe <step>` on a decal centred on the main party:
- `decal_defender_ranged_siege` never renders outside vanilla's siege setup, copy or not.
- `decal_city_circle_a` renders, both the stock material and a `CreateCopy()`, at scale 10 and
  50, tinted by `SetFactor1`. Its blend is **Modulate**: it multiplies the ground colour.
- Swapping the copy's diffuse to another *stock decal* texture (the map cursor's) renders.
- Swapping it to a *runtime* texture renders **nothing**. This held for:
  - `CreateFromByteArray` raw RGBA;
  - `CreateFromMemory` PNG;
  - always-valid + blocking preload;
  - each of these with `Decal.CheckAndRegisterToDecalSet()`.

Conclusion: map decals sample only the decal atlas, which is built when resources are compiled
(`DecalAtlasGroup`). A texture generated at runtime can't be put on a decal. Decals can still
carry *stock atlas* textures tinted at runtime. One option is a per-county mask texture baked
into the module's decal atlas, coloured live by `SetFactor1`. Its limits are per-county
granularity and the atlas size. Next to test is the fallback: a terrain-following mesh with a
runtime texture (`polmap.mesh`).

## Mesh spike result (2026-10-28, live on 1.4.8): runtime textures render on a mesh

A runtime-built grid mesh draped on `scene.GetTerrainHeight` samples (vanilla
`BoundaryWallView` recipe) renders runtime textures. The checkerboard showed with all four
sources: `CreateFromByteArray`, `CreateFromMemory` PNG, `CreateTextureFromPath` and
`LoadTextureFromPath`. So the territory wash is viable. The material is still open:
`editor_map_border` uses its slot-0 texture only as a mask under a fixed green, so true colours
need another material. The candidates are vanilla's runtime-texture materials
`agent_label_with_tableau`, `tableau_with_alpha_mask` and `formation_icon`, plus overriding the
blend mode (`polmap.mesh … <blend>`).

Tried 2026-10-28: `agent_label_with_tableau` (slot 0), `tableau_with_alpha_mask` (slots 0 and 1)
and `formation_icon` (slots 0 and 1) draw nothing on the map mesh. `editor_map_border` with
`AddAlpha` draws a teal diamond hatch: its shader adds the hatch itself, so it is ruled out.
Next: the map party banner's own material (`mm:campaign_banner_small`, tableau in slot 1).
Fallback: a dedicated material authored in the DADG module.

**Working route (2026-10-28): `vertex_color_mat`, no texture at all.** Shader `notexture`,
stock blend `AddAlpha`. Colours must be written with `Mesh.AddTriangleWithVertexColors`:
plain `AddTriangle` leaves the mesh white. Per-cell red/blue/green renders in true colour,
draped on the terrain. `GameEntity.SetAlpha` and `Mesh.SetColorAlpha` are ignored, so
translucency has to come from the vertex colour's alpha byte. Confirmed: alpha 90/255 (35%)
gives a readable wash with terrain, rivers and towns showing through.
**Build cost is quadratic in mesh size.** One mesh of 2k triangles took 72 ms, 8k took 1042 ms,
and 131k never finished (est. minutes). So the geometry must be many small meshes: one per
county, or chunks of at most about 512 triangles. Confirmed after chunking into 16×16-cell meshes
(512 triangles): a 2000 m sheet of 8k triangles builds in 72 ms (16 meshes), and one of 131k triangles in
1170 ms (256 meshes). That is linear, about 9 µs per triangle.

**Spike verdict: GO.** The display layer is coloured geometry rather than a texture:
`vertex_color_mat` with per-vertex ARGB at about 35% alpha, one or more small meshes per county.
Section 1's "runtime compose" becomes "runtime recolour": the offline bake emits triangulated
settlement-cell polygons and border strips, and a holder change rebuilds only the affected
county's meshes, so a few ms.

**Rendering cost: none noticeable.** With the 131k-triangle sheet up, moving the party over it
kept the FPS unchanged (user playtest 2026-10-29). The only cost is the build itself. It runs
synchronously on the main thread, so the game freezes while it runs. The runtime builder must
therefore build once at map load and spread later rebuilds across ticks, a few county meshes per
frame, never a full rebuild inside an event handler. Each mesh holds 512 triangles at most, so 131k triangles takes about 256 of them. This turns the design
from "compose a texture" into "build coloured geometry": the bake emits triangulated county
polygons (or a settlement-ID grid), and the runtime writes each triangle's holder colour.
Borders become thin darker strips of triangles along polygon edges.

## Spike status

`DellarteDellaGuerra.Integration/PoliticalMap/PoliticalMapSpikeCommands.cs` is throwaway code
and compiles. Console commands, on the campaign map:
- `polmap.decal [material] [heightScale]` lays a 16×16 red/blue checkerboard with a green
  centre cross over the map's borders. From it you can read whether projection works, the
  decal's extent and orientation, the channel order and the alpha. The default material is
  a copy of `decal_defender_ranged_siege`; try other decal materials if it renders wrong.
- `polmap.hide` removes it.
- `polmap.political_color <value>` calls the native function.

## Settlement grid built in game (decision 2026-10-30)

**Decision: the mod builds the settlement grid itself at campaign load. Nothing is baked or
shipped for it, and no Python is needed to play or to move a settlement.** The grid is the
same as `tools/political_map_bake.py` produces: 1120×1120 cells of 1 m, each land cell owned by
its nearest town, castle or village measured over land.

**Land mask source: the map's own `terrain.bin`, parsed in C#.** It ships with the map module,
and it is exactly what the Python bake thresholds, so the two land masks are identical. The
reader (`Infrastructure/PoliticalMap/Zgr6TerrainReader.cs`) decodes the ZGR6 HGHT section: an
LZ4 raw block of 256 nodes, each a 129×129 16-bit greyscale PNG, inflated with the BCL
`DeflateStream`. The height range and map extent come from the `<terrain>` tag of
`scene.xscene`, streamed because it sits at the end of a 20 MB file. The scene folder is
chosen the way vanilla `MapScene` chooses it: the last active module that ships
`SceneObj/Main_map/scene.xscene`. The engine options were rejected:
- `Scene.GetTerrainHeight` per cell is 1.25M native calls, which have to run on the main thread
  with the map scene loaded.
- The campaign navmesh's face terrain types are a different notion of land from the one the
  reference grid uses. Navmesh faces are coarse polygons, and rivers are their own face types.
  Matching the Python mask would still need the closing and the threshold on top.

**Algorithm** (`Domain/PoliticalMap/Grid/PoliticalMapGridBuilder.cs`, pure and unit tested),
the same as the bake step by step:
1. Bilinear height at each cell centre, compared with sea level 9.6.
2. A binary closing with the 4-neighbour cross, 2 iterations each way, so rivers under 3 m do
   not split a landmass. Off the grid counts as water, as in scipy.
3. Each settlement starts from the landmass cell nearest its position.
4. A multi-source Dijkstra over the 8-connected landmass, with diagonals weighing √2, run on a
   binary heap.
5. Cells that were bridged water go back to 0.

**Fidelity against the Python reference** (`geo_cells.bin.gz`, baked 2026-10-30 from the same
files):

| | Python | C# |
|---|---|---|
| Assigned land cells | 256,919 | 256,919 (the same cells) |
| Settlement list | 564 | 564, same order |
| Cells with the same value | | 1,253,779 / 1,254,400 = **99.95%** |
| Land cells with the same owner | | 256,298 / 256,919 = **99.76%** |

All 621 cells that differ lie on a border between two settlements, with the reference owner
within 2 cells. They are ties between equal-length paths, which scipy's heap and ours break
differently. The difference is invisible at map scale.

**Cost** (net8.0 test host on the dev machine, warm runs):

| Step | Time |
|---|---|
| Stream `scene.xscene` to the `<terrain>` tag | ~35 ms |
| Decode `terrain.bin` (LZ4 + 256 PNGs) | ~140 ms |
| Build the grid (mask, closing, seeds, Dijkstra) | ~270–295 ms |
| **Total** | **~0.45 s warm, ~0.56 s cold (first run, JIT included)** |

That is under a second, so there is **no cache**. A cache keyed on the terrain and settlement
hashes would save half a second once per session, at the price of a file in the user's Configs
folder that can go stale. It can be added later if the game's .NET Framework runtime turns out
much slower than the test host.

**Wiring:** `PoliticalMapGridCampaignBehavior` (feature project). On `OnSessionLaunched` it
reads the towns, castles and villages from `Settlement.All` on the main thread, which is cheap.
It then runs the terrain read and the build in `Task.Run`. Its `Grid` property is null until the
build is ready, and `GridTask` lets a caller await it. A build that finishes after another save
has loaded is dropped. A failure is logged, and the map stays blank. `IPoliticalMapTerrain` →
`MainMapTerrain` is registered in `DadgServiceContainer`, and the behaviour in `SubModule`.
The settlements come from the live campaign, not the XML, so the grid follows whatever
settlements another module adds or moves.

**What this retires:**
- `config/political_map/settlement_cells.bin.gz` is no longer read by anything and can be
  deleted. It is also stale: it is an older straight-line bake.
- `tools/political_map_bake.py` is no longer part of the build or the release. It is worth
  keeping as an offline preview tool, because its PNG preview of county and duchy borders is
  the quickest way to check title-tree changes without launching the game.

Both are left in place for now. Deleting them is the maintainer's call.

**Still open:**
- A live check of the build time on the game's runtime: the log line "Political map grid
  built: …" reports it.

## Overlay as built (2026-10-30)

The overlay reads the settlement grid above and draws vertex-coloured quads on a sheet draped
over the terrain. This is the mesh route from the 2026-10-28 spike, without a texture. The decal
route was re-checked on 2026-10-30 and is still a no-go: map decals sample only the compiled
atlas, so a runtime texture on a decal draws nothing. The code is in:
- `Domain/PoliticalMap/Overlay/`: the painter, demesne fold, mesher, quad packer, sheet and
  zoom, all unit tested. `Domain/PoliticalMap/PoliticalMapPalette.cs` holds the wash and the
  map's kingdom colours.
- `DellarteDellaGuerra/PoliticalMap/Rendering/`: the renderer and the per-frame drawing.
- `PoliticalMapOverlayCampaignBehavior`.

The spike commands (`PoliticalMapSpikeCommands.cs`) are kept.

**The sheet.** The grid is cut into blocks of 8 cells (`PoliticalMapMesher.MaxQuadSide`). The
sheet's height at each block corner is the highest terrain over the blocks touching that corner,
and it is interpolated bilinearly between corners (`PoliticalMapSheet`). The quad packer keeps
every quad inside one block (`PoliticalMapQuadPacker`), so no quad dips under a hill, and quads
of mixed sizes meet without cracks. Each territory is one entity of fill quads, 0.3 above the
sheet.

Quads are drawn with a copy of `vertex_color_mat` set to alpha blend `Modulate`
(`src·α + dst·(1−α)`), changed 2026-10-31. The stock material blends `AddAlpha` (`dst + src·α`,
logged live by the mesh spike above). That is additive, which caused three faults seen in game:
- black borders added nothing, so they were invisible;
- each border band showed as an empty strip, because the border mesh, lifted above the fill,
  hid the fill under it and then added black;
- fills went pastel over bright terrain, because adding colour to bright ground only lightens it.

`Modulate` is the engine's name for the standard blend, and is what the rest of this section
assumes. It has not been verified live yet; if the fills draw wrongly, the blend mode is the first
thing to look at.

**Territories and the demesne fold.** Each settlement belongs to its seat's county; a village's
seat is its bound town or castle. A duchy's demesne is a settlement whose title is a duchy, not
a county. It is folded at runtime, from the grid, into the county it shares the longest border
with: the county with the most distinct grid cells touching the demesne. Ties go to the smaller
id. On the 1471 map this gives exactly the bake's folds, which
`PoliticalMapOverlayOverRealContentTests` asserts:

| Duchy demesne | Folded into |
|---|---|
| Buckingham | Cheshire |
| Lancaster | York |
| Norfolk | Norwich |
| Somerset | Bristol |
| Wales | Gwynedd |
| Warwick | Oxford |
| York | Warwick |

**Views**, cycled in this order by G, `polmap.view next` and the map bar panel. Each panel row
shows the view's name over a one-line caption:

| View | Caption | Fill |
|---|---|---|
| Kingdoms (default) | Which kingdom controls each place right now. | The map colour of the `Settlement.MapFaction` (below). A kingdomless clan gets a map clan colour. |
| Titles (live, 2026-10-31) | Which title each holder holds over their land right now. | Each settlement's land goes to its owner clan. It is grouped under the highest-ranked title that clan holds over that land right now, for example the Duke of York's counties form "Duchy of York". Each such region is one flat colour, and touching regions differ. No stripes, no greying. |
| Owners | Which clan actually holds each castle, town and village. | Each `OwnerClan` a lighter or darker shade of its kingdom's map colour; a kingdomless clan a colour of its own. Villages take their bound settlement's colour. |
| Off | Hide the overlay. | Nothing |

A settlement with no owner has no fill. No view draws borders (removed 2026-10-31): regions are
told apart by colour alone, and the region labels name them.

The Titles view used to be the de jure map: duchies, their counties as shades, vacant counties
greyed and contested ones striped. It was replaced on 2026-10-31 by the live map, because the
"rightful holder" idea confused more than it explained. A de jure view can come back later as a
view of its own.

**Kingdom colours.** The map uses its own kingdom colours, not `Kingdom.Color`. Kingdom colours
and banners come from POC (`DellarteDellaGuerra/config/poc.config.json`, `kingdomBanner`). Those
banners give York and Lancaster near-white backgrounds (palette 127) and Scotland a blue one
(143), which would not read as four realms. `PoliticalMapPalette.KingdomColor` holds a fixed
table keyed by kingdom id, and falls back to the live colour for any other faction:

| Kingdom | Id | Map colour | Hue |
|---|---|---|---|
| House of Lancaster | `sturgia` | strong red `0xFFD41E1E` | 0 |
| House of York | `vlandia` | strong blue `0xFF1E64DC` | 218 |
| Crown of Scotland | `khuzait` | green `0xFF19B333` | 130 |
| Lordship of the Isles | `battania` | magenta `0xFFF20DB9` | 315 |

Why these (changed again 2026-10-31). The DADG scenario is two civil wars inside one war, so each
rival pair must look like enemies at a glance. The previous table put York in violet beside
Lancaster's red, and Scotland in blue beside the Isles' green; both pairs were too close. The
rule now is that rivals are at least 120 degrees of hue apart. Lancaster stays red and York
stays blue. Scotland borders England, so it takes the remaining primary, green. The Isles touch
only Scotland, so they take magenta, opposite green. Gold for Scotland was ruled out: gold
against green is only about 85 degrees.

| Pair | Hue apart (degrees) | Delta E |
|---|---|---|
| Lancaster / York (rivals) | 142 | 123 |
| Scotland / Isles (rivals) | 175 | 170 |
| Lancaster / Scotland | 130 | 130 |
| York / Scotland | 88 | 148 |
| Lancaster / Isles | 45 | 81 |
| York / Isles | 97 | 73 |

The closest pair, Lancaster and the Isles, never touch on the map. Unit tests hold:
- rivals to at least 120 degrees and Delta E 100;
- every pair to at least 40 degrees and Delta E 70;
- every shade of a rival at least Delta E 60 from every shade of the other.

The kingdom view legend shows the swatches.

The table is code only, so it applies to existing saves too. The kingdom XML and the POC banners
are left as they are.

**Map colouring (2026-10-31, CK3 style).** Banner colours are never used: POC gives most English
clans white banner backgrounds and many Scottish ones near-identical reds. Every colour comes from
`PoliticalMapColouring` (domain), and is picked by greedy graph colouring over which regions touch:
- Colours are compared by CIE76 Delta E (CIELAB), not RGB distance: 2 is just noticeable, 20 is
  clearly different. Minimum distances between touching regions:
  - clans of one kingdom: 12 (`ShadeMinDistance`);
  - kingdomless clans: 20 (`DistinctMinDistance`);
  - title regions: 40 (`RegionMinDistance`).
- `Shades(colour)` gives 6 shades of a kingdom colour, the colour itself first. Every shade keeps
  the colour's HSL hue and saturation; only the lightness moves, within 0.2 of the colour's own
  (never darker than 0.22 or paler than 0.72). So a York clan is always York blue, lighter or
  darker, and never drifts toward another kingdom's hue. They are picked by farthest-point
  sampling, so touching clans take the most different shades first. The first three shades of
  every kingdom are Delta E 17 or more apart.
- Shades of rival kingdoms stay far apart: the closest Lancaster and York shades are Delta E 78
  apart, the closest Scotland and Isles shades 111.
- Two regions touch when any of their settlements' grid cells share a side
  (`PoliticalMapSettlementAdjacency`). Regions are coloured most settlements first, then by id.
  Each keeps its last colour if it still fits, else takes the first candidate far enough from
  every touching region already coloured, else the candidate furthest from them all. The result
  does not depend on the order settlements or pairs are listed in, and one castle changing hands
  does not reshuffle the map.

Per view:
- Kingdoms: the four kingdoms keep their fixed colours above. A kingdomless clan holding land
  takes a colour from `KingdomlessPalette`: 26 colours at least 25 degrees of hue from every
  kingdom hue and Delta E 20 from every kingdom shade and from each other (re-picked for the new
  kingdom colours).
- Owners: each clan is a lighter or darker shade of its kingdom's map colour (the faction most of
  its settlements answer to), touching clans of one kingdom different shades. A clan under no
  kingdom takes a `KingdomlessPalette` colour. A faction outside the fixed table is shaded from
  its live colour (in the Kingdoms view it would take a kingdomless colour instead; no such
  kingdom exists on the 1471 map).
- Titles, the live map (`PoliticalMapPainter.TitleRegionOf`). For each settlement:
  - The owner is `OwnerClanOf`; no owner means no fill.
  - The painter walks up the de jure chain from the land's own territory and from the county it
    is folded into. It takes the highest-ranked title the owner holds right now. "Holds" means
    the title's occupant, or its holder's clan when it has no occupant.
  - Kingdoms count too: the King of England's own land reads "Kingdom of England".
  - If the owner holds no title over the land, the region is named after the land's county
    title, or after no title when the land has none. It is still the owner's region.
- The region is keyed `"titleId@clanId"` (`PoliticalMapTitleRegion`), with an empty title part
  for untitled land. Each region is one flat colour from `TitlePalette`: 32 colours, each Delta E
  22 or more from all the others. Touching regions keep 40 apart.
- Each region's candidates are the palette rotated by a stable FNV hash of its key
  (`TitleCandidates`). Regions therefore spread over the whole palette, yet a region always
  prefers the same colours. On the 1471 map this gives 104 regions with 751 touching pairs.
- No stripes and no greying: the paint's `Hatch` is always 0.

The colours depend only on the map, so they apply to existing saves and are not saved.
`PoliticalMapPainter.MapColours(view)` returns the swatch colours the overlay last drew in a view,
for the legend: the kingdoms (and kingdomless clans) in Kingdoms and Owners, nothing in Titles. A
paint without neighbours (the region labels asking for keys only) neither colours regions apart
nor changes the remembered colours.

**Titles keys for the region labels.** In the Titles view, each `PoliticalMapSettlementPaint`
carries these keys:

| Key | Holds |
|---|---|
| `CountyKey` | The territory after the demesne fold, as before. |
| `DuchyKey` | The title the land's live region is named after, or null for untitled land. |
| `RealmKey` | Null. |
| `RegionKey` | The region key, `"titleId@clanId"`. |

`PoliticalMapPainter.TitleRegionNames(regionKey, names)` gives a region's display names: the
title's raw name, with its string id (wrap it in a `TextObject`), and the holder clan's name.
`PoliticalMapTitleRegion.Parse(key)` splits a key into its holder clan and title.

**Wash and zoom.** Fills are drawn at one opacity for every view, 60% by default
(`PoliticalMapPalette.Opacity`, alpha 153). With a real alpha blend, a saturated fill at 60% over
bright terrain keeps about half its saturation. That is enough to read the colour while relief
and rivers still show; 0.75 is the next step if it looks weak in game. The opacity is a session
setting: `polmap.opacity <0..1>` changes it, and the whole map repaints within a second or two
(every paint changes, so every territory rebuilds). It is not saved. Values to try:
- `0.45`: lighter, rivers and relief show clearly, colours weaker over snow.
- `0.6` (default): colours read over snow and bright terrain, relief still visible.
- `0.75`: strong colours, the terrain mostly hidden.
- `1`: a flat paper map, painted land hides the terrain entirely. Water and unowned land stay
  unpainted.

There is no colour grade or terrain tint: only the fill alpha changes. Vertex alpha can't be
faded without a rebuild, so zoom switches the wash on and off instead of fading it
(`PoliticalMapZoom.ShowsWash`):
- Camera distance under 20: no wash. With the borders gone, nothing of the overlay shows this
  close in except the labels.
- Camera distance 20 and over: the wash.

These thresholds are a first guess.

**Saved state.** Only the chosen view is saved with the campaign (`_dadgPoliticalMapView`). A new
campaign starts on Kingdoms.

**Performance.** Nothing is rebuilt in an event handler.
- Three scene-local mesh caches retain Kingdoms and both Owners zoom levels. Once prepared,
  changing view or zoom switches visibility instead of rebuilding the map.
- Idle frames prepare the unused levels with a 2 ms upload budget per frame. A requested cold
  level gets a 6 ms budget. Initial preparation still takes time; the last completed map stays
  visible until the replacement is complete.
- Border smoothing and geometry planning run on worker threads using immutable paint snapshots.
  Terrain sampling and native mesh uploads stay on the map thread.
- Live paints are checked once a second and before activating a cached level. Colour changes
  invalidate affected territories and their smoothing neighbours. Obsolete worker results are
  discarded; all affected meshes swap together when their uploads finish.
- Label regions are cached for the same three levels and prepared on a worker. Settlement keys
  are checked before reuse, names remain live, and labels follow the completed mesh level.
  Unchanged labels retain their view models and terrain samples.
- Caches are bounded to three levels and dropped with the grid/session. This uses more memory
  in exchange for avoiding repeated geometry work on zoom.
- The overlay ticks from `OnApplicationTick`, so it also updates while the game is paused.
- Cache-ready logs separate worker planning, native upload time and the slowest map frame.
  Runtime zoom latency still needs a live check after deployment.

**UI.** A "Political Map" button sits in the top-left corner of the campaign map. It is a
UIExtenderEx prefab extension on `MapBar`, appended inside the bar's full-screen root, plus a
`MapBarVM` mixin, so it hides with the bar. It first sat on the bottom bar just right of the time
controls, but the bottom bar has no free slot: the button was squeezed between the time controls
and the right info bar, and its dull olive brush (`Popup.Done.Button.NineGrid`) read badly. It
now uses the vanilla `ButtonBrush2` (the main menu button, with hover and selected states) and
`Popup.Button.Text` at 22 px, 220 x 50. The button opens a small panel under it with one row per
view: its name, with the current one marked `>`, over its caption. All text is localizable
(`{=dadg_polmap_*}`).

**Legend.** While a view is drawn, a dark box under the button shows, top to bottom:
- a large title, "Political Map: Kingdoms" / "Political Map: Titles" / "Political Map: Owners"
  (`Popup.Button.Text`, 30 px, the button's own cream colour);
- one plain-English line saying what a colour means in that view (18 px):
  - Kingdoms: "Each colour is a kingdom: land is painted in the colour of the kingdom that
    controls it now."
  - Titles: "Each colour is a title held right now, with all the land its holder holds under
    it."
  - Owners: "Each clan's land is a lighter or darker shade of its kingdom's colour; a clan under
    no kingdom has a colour of its own."
- Kingdoms and Owners: a swatch and name for each faction controlling at least one settlement, most
  settlements first (`PoliticalMapLegend`). Kingdomless clans that hold land are listed too. Each
  swatch is the colour the map last painted that faction in the current view
  (`PoliticalMapPainter.MapColours(view)`; in Owners, the kingdom's base colour, its clans being
  shades of it), never a banner colour
  (`IPoliticalMapWorld.ClanColor`/`FactionColor` are not used). The list is empty until the view
  has been painted once, which happens within a second of switching to it;
- Owners: "Lighter and darker shades of a kingdom's colour are its different clans."
  (`dadg_polmap_legend_clan_lightness`).
- Titles shows no swatches and no notes: a colour names a held title, which the hover and the
  labels name. The old stripe note is gone; the prefab still binds `StripeNoteText`, so it stays
  as an empty, hidden stub.
- There are no borders in any view.
- the hover readout (below);
- the hint "G: next view".

The box is 440 wide. Its strings are ASCII only: Galahad is not known to carry dashes or other
typographic glyphs, and a missing glyph drops without an error.

**Why the button was blank and the legend missing (fixed 2026-10-31).** With War Sails loaded,
the map bar's view model is `NavalDLC.ViewModelCollection.Map.MapBar.NavalMapBarVM`, not
`MapBarVM`. The UIExtenderEx 3.0.3 compiled-prefab cache says so
(`Modules/Bannerlord.UIExtenderEx/CompiledPrefabs/CompiledPrefabs.zip`, entry
`MapBar/NavalDLC.ViewModelCollection.Map.MapBar.NavalMapBarVM.dll`). UIExtenderEx attaches a
mixin only to instances of exactly its target type unless the mixin says `handleDerived`
(`ViewModelComponent.RegisterViewModelMixin`, and the constructor hook checks
`DeclaringType == instance type`). So no mixin was ever created. The compiled prefab binds mixin
members by name (`DynamicMember.Get` → `ViewModel.GetPropertyValue`), got null for each, and so:
- every text came out empty, hence the empty plaque;
- every `IsVisible` came out false, hence no panel and no legend;
- the button's click did nothing.

The overlay and the hotkey were unaffected, as they live in the campaign behaviour. The fix is
`[ViewModelMixin(handleDerived: true)]` on `MapBarPoliticalMapMixin`. Any future mixin on a view
model that a DLC subclasses needs the same: `MapBarVM`, `EncyclopediaSettlementPageVM` and
`MapAnchorTrackerVM` are compiled for NavalDLC subclasses.

**Hover readout.** Five times a second, while the map screen is on top, the cursor is cast onto
the map terrain the way the map screen's own cursor is (`SceneView.TranslateMouse` and
`RayCastForClosestEntityOrTerrain`, same exclusion flags). The grid cell under it gives the
settlement, and `PoliticalMapHoverDescriber` decides what to say:
- Kingdoms: the settlement, and the faction controlling it (or "No kingdom").
- Titles: the name of the title the land's live region is named after, and "Held by <clan>" for
  that region's holder. Untitled land shows its settlement, "Held by <clan>" and "No title". Land
  with no owner shows its settlement and "No owner".
- Owners: the settlement, and "Held by <owner clan>". A village answers for its bound town's or
  castle's owner.

Water and land owned by no settlement show nothing. The readout is in the legend box, not a
floating tooltip.

**Snow.** While any view is drawn, the campaign map's snow is held back so the washes read over
the northern counties in winter. The lever is the season factor the map scene's snow follows:
`MapScreen.TickVisuals` asks `MapWeatherModel.GetSeasonTimeFactorOfCampaignTime(now, ..., false)`
every frame and passes the snow factor to `MBMapScene.SetSeasonTimeFactor`. DADG already wraps
that model (`DadgMapWeatherModel`), so the wrapper caps the snow factor at midsummer (0) on that
unsnapped call only, while the overlay is drawn on the current map scene. No Harmony patch, and
nothing to restore: the moment the view is Off, or the scene is gone, the next frame passes the
season's own factor again. Weather (rain, snow storms, the snapped callers) is untouched.
- `polmap.snow 0` (default): hide the snow while a view is drawn.
- `polmap.snow 0 -1`: also cap below midsummer, for snow that lingers on the highest peaks.
- `polmap.snow 1`: leave the snow to the season.

The snow setting is per session, not saved. The terrain atlas itself (the painted ground
texture) is not touched. Hiding it would need a material swap on the terrain, and a flat paper
map is `polmap.opacity 1` instead.

**Hotkey: G** cycles the views on the campaign map. G is free there in vanilla, where it only
drops a weapon in missions. It is the same physical key on QWERTY and AZERTY.

**Console commands:**
- `polmap.view [next|Kingdom|Titles|Owners|Off]`.
- `polmap.opacity [0..1]`: the fill opacity (above).
- `polmap.snow [0|1] [season factor]`: the snow switch (above).

**Known limits:**
- A title re-attached under a new liege is recoloured at once. However, which settlement
  belongs to which county is fixed when the session starts.
- Banners aren't drawn yet. They were cut; see build order step 4.

**Verified live:** nothing on this build yet. Everything above is covered by the build and the
unit and real-content tests only. Still to check in game:
- the three views and their captions in the panel, and the panel's width;
- the button in the top-left corner: its brush, its label, and that it opens the panel;
- the legend box: the kingdom swatches, the clan note, no stripe note, and that it clears the
  top of the screen;
- the hover readout in each view, including untitled land, land with no owner, and water;
- snow gone while a view is drawn in winter, back when the view is Off;
- the `Modulate` blend: fills saturated rather than pastel over bright terrain, and no empty
  strips where the borders used to be;
- `polmap.opacity` at 0.45, 0.6, 0.75 and 1;
- the four kingdom colours as a wash;
- the rival pairs (Lancaster red against York blue, Scotland green against Isles magenta) read
  as enemies at a glance;
- Owners: each kingdom's clans read as lighter and darker shades of its colour, touching clans
  clearly different, kingdomless clans distinct from every kingdom; the kingdom swatches and the
  clan note;
- Titles: each live region one flat colour and touching ones different; the Duke of York's land
  reads as one "Duchy of York" region and the king's own land as "Kingdom of England"; no stripes
  or greying; the caption and that the legend shows no swatches;
- that the draped sheet covers hills without poking through or cracking;
- the zoom thresholds;
- the frame rate during a view switch.

### Region labels in place of settlement nameplates (2026-10-31)

While any view but Off is drawn, the settlement nameplates are hidden and the regions are named.

**Nameplates.** A Harmony postfix on the private `SettlementNameplateVM.IsVisible(in Vec3)`
(`HideSettlementNameplatesPatch`) returns false while the flag `PoliticalMapNameplates` is set.
The labels behaviour sets the flag to `view != Off` every frame. Vanilla runs this check for every
nameplate every frame, so going back to Off restores vanilla visibility on the next frame.
- Kept on the map: tracked settlements, settlements with a siege or raid icon
  (`MapEventVisualType != 0`), and hideouts.
- Party nameplates use their own view model and aren't touched.
- Settlements stay clickable through their 3D models. The nameplate's own click and right-click
  tracking go with it. So do the party markers drawn on a settlement nameplate.
- The small banner isn't kept. That would mean editing the vanilla prefab.

**Labels.** Each label is one Gauntlet text per contiguous region of the view's key. One tier
per view, the same at every zoom (the old county/duchy zoom tiers are gone):
- Kingdom view: the faction name (kingdom, or a kingdomless clan).
- Owners view: the owner clan's name, or with the label toggle on Titles the clan's highest title
  (best rank, first by id among equals; a clan holding none keeps its name).
- Titles view: one label per live title region (the owner clan's highest title over the land,
  the same regions as the colours), named by that title, or with the toggle on Clans by the
  owner clan.
- The Kingdom view has no toggle: it always names the realms.

The title names come from `titles.config.xml`. Settlements seating no title get no label.

**Label toggle.** Under the view rows, the panel shows a row in the same `ButtonBrush2` style,
"Labels: Titles" or "Labels: Clans" over a one-line caption, while the Titles or Owners view is
on. A click flips the current view's naming; each view keeps its own choice (Titles starts on
titles, Owners on clans). The console has `polmap.labels [title|clan]` for the current view. The
choice is session only and isn't saved (`PoliticalMapLabelModes`, held by the labels behaviour).
A switch only renames: the regions are not found again.

**Title regions.** The Titles labels group settlements by the titles paint's own `RegionKey`
(`titleId@clanId`, `PoliticalMapTitleRegion`), so a label always covers exactly one flat colour.
Its title is the label in Titles mode, its holder clan in Clans mode; each falls back on the
other when it has none (a region under no title is named after its clan).

**Placement.** The regions come from `PoliticalMapRegions` (Domain, unit-tested):
- It finds 4-connected components of one key, using water and foreign keys as walls.
- A chamfer (3-4) distance transform puts each label at the pole of inaccessibility: the deepest
  cell, with ties going to the cell nearest the centroid. So an L or a crescent is labelled on its
  land.
- Fragments under a size threshold are dropped: 100 cells for a county, 150 for an owner, 400 for
  others.
- Regions are re-found on a worker thread only when a settlement's key changes. Keys are compared
  once a second and on a view switch.

**Sizing and collision.** The font follows the region's size on screen, never the length of its
name. A long name like "Earldom of Warwick and Salisbury" or "Principality of Wales" prints as
large as "Kent" in a region of the same size. It may run over neighbouring regions and the sea.
- Gauntlet can't bind a font size, so there are seven fixed steps (13 to 48). Each step is its
  own `TextWidget`.
- Each frame, every label is projected (`MBWindowManager`). Its region's inscribed radius is
  projected too, so the font follows both the region's size and the zoom.
- `PoliticalMapLabelLayout` picks the largest step up to 0.8 of that radius on screen. A region
  showing under about 16 px of radius (too small for size 13) gets no label. Fragments are still
  dropped when the regions are found.
- Labels are placed widest tier first (faction above title above owner), then largest region
  first. A label whose text box overlaps one already placed is dropped. So a long name that runs
  over its neighbours hides their labels, and a lower-priority neighbour can't cover it.
- No repeats nearby. A label is also dropped when one with the same text is already placed and
  the gap between the two boxes, edge to edge, is under a third of the screen width
  (`RepeatDistance`, 640 px on 1920). A kingdom cut into patches, or a clan with scattered fiefs,
  is named once, on its biggest patch in view. Zoomed far out a whole realm fits in that radius
  and collapses to one name. Zoomed in, a patch a third of the screen away is named again, and so
  is one whose bigger twin is off screen. This applies in every view and in both label modes, so
  in Clans mode two neighbouring regions of one clan show one name.
- The text box is estimated, not measured: 0.5 em per character wide and 0.9 em tall, plus a 4 px
  gap. These come from the game's `GUI/GauntletUI/Fonts/Galahad/Galahad.fnt` (size 32, no
  kerning). Capitals average 0.58 em, lower case 0.42 em and a space 0.25 em. Real region names
  come to 0.36 to 0.42 em per character, so 0.5 leaves room for the outline and for names heavy in
  capitals.

**Rendering.** Brush `DADG.PoliticalMap.Label` uses Galahad in parchment white with a black
outline and glow. The layer `DadgPoliticalMapLabels` is at order 85, just under the nameplate
layer (90), so party nameplates draw over it. It takes no input, and it is hidden during map
conversations.

**Files:**
- `src/DellarteDellaGuerra.Domain/PoliticalMap/Labels/`
- `src/DellarteDellaGuerra/PoliticalMap/Labels/`
- `src/DellarteDellaGuerra/PoliticalMap/Api/Campaign/PoliticalMapLabelsCampaignBehavior.cs`
- `src/DellarteDellaGuerra.Integration/PoliticalMap/HideSettlementNameplatesPatch.cs`
- `GUI/Prefabs/PoliticalMap/PoliticalMapLabels.xml`
- `GUI/Brushes/PoliticalMapLabels.xml`

**Not verified live yet.** To check in game:
- that the labels sit on their regions;
- the font sizes at each zoom, and that long names stay readable fully zoomed out (they may run
  over neighbouring regions);
- that "House of Lancaster" / "House of York" show once per realm when zoomed out in the Kingdom
  view, and once per nearby cluster zoomed in; the same for clans in Owners;
- the label toggle row: shown in Titles and Owners only, its text flipping, and the labels
  renaming at once; `polmap.labels title|clan`;
- that title-region labels match the titles colours, one name per flat colour region;
- that settlement nameplates come back with the view Off;
- that clicking settlements still works.

## Zoom continuity (2026-11-01)

Owners levels share each continuing ruler's label anchor, so zooming only changes its screen
projection. The anchor is based on the ruler's detailed land; newly revealed vassals get their
own labels. Region sizes still govern font size and collision priority, and ownership changes
invalidate the relevant cached anchors.

The ruler/direct-vassals colours are assigned first, even if the detailed level is prepared
first. A continuing ruler's own land retains exactly that shade in the detailed level;
newly revealed vassals receive distinct shades around those fixed colours. Both levels still
track live kingdom membership, including defections.

### Frontier lines and title labels (2026-11-01)

The live map draws thin solid lines between the displayed kingdoms or main owner regions.
Zooming into Owners adds dashed internal divisions within a direct vassal's territory,
including the boundary between that lord's own land and its vassals. Lines follow the same
smoothed geometry as the fills; coastlines and riverbanks are left unoutlined. Frontier
geometry is retained with the existing map-level caches and refreshed on live grouping changes.

In Owners' Titles label mode, every owner uses its highest live title below the crown,
including the ruler (for example Duchy of Lancaster rather than England or Crown lands).
An owner without a landed title uses its estate's place name instead of silently falling
back to its clan name. These are display names only: no title is granted or transferred.
Clans mode still shows clan names. Label deduplication uses region identity, so distinct
owners with the same caption are not treated as one.

### Hierarchy labels and flat colours (2026-11-01)

Owners labels are filtered by the live title hierarchy in both naming modes: the zoomed-out
level shows counts and higher; the detailed level also labels barons and untitled estates.
When labels overlap, higher titles take precedence before region area. The existing label
anchors, live ownership grouping and title/clan text toggle are retained.

Names much wider than the projected territory diameter wrap at word boundaries to two lines,
or three when two are still too wide and a third reduces the width. Font size continues to
follow the territory, and each line is centred by the existing text brush. Collision boxes
account for the longest line and the full height. Wrapping is cached until the available
character width changes; it never rebuilds map meshes.

Per-vassal lighter/darker shades are disabled for now: every owner in a kingdom uses that
kingdom's base map colour. Kingdomless clans keep distinct colours. The owner region keys
remain separate, so smooth white frontiers distinguish the estates even when fills match.
Main divisions remain solid and the revealed internal vassal divisions remain dashed.
This supersedes the earlier shading descriptions; PoC banners and heraldry are unchanged.

### Compact controls and subdivision zoom (2026-11-01)

The map UI now keeps only the Political Map and Labels buttons; the help and legend panel is removed. Owners reveals subvassals when camera distance falls below 140, and returns to the ruler/direct-vassal level above 150. This retains hysteresis while making subdivisions visible farther out.
