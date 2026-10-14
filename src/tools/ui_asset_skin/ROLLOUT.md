# DADG UI asset rollout

Accepted recruitment baseline (user's in-game visual approval, 2026-10-04):
Core `46f0258`, Map `18074ad`. Keep this checkpoint distinct from subsequent batches.

## Method

- Reuse approved PNG art: blue outer surfaces, darker blue contents, navy cards,
  approximately 4px beveled content frames, 2px gold card outlines.
- Work by shared sprite families, not copied screen layouts. No new HTML mockup
  unless the component has materially different technical behaviour.
- Keep native sheet dimensions, names, coordinates, slice metadata, alpha masks,
  widget geometry and bindings. Only small targeted brush/tint corrections when
  an actual consumer needs them; never add hundreds of prefab overrides.
- Keep finished encyclopedia art, squared banners, portraits, functional icons,
  semantic state colours, and illustrations. No automatic whole-atlas recolour.
- Check changed/unchanged pixels, then import with the editor; never write TPAC/RDC.
- The existing inventory has 22 candidate sheets, not a verified 21-sheet task or
  22 mandatory repaints. Naval-specific candidates are optional and separate.

## First source batch: shared-surfaces-01

Three sprite regions in `ui_group1_1`, plus their existing loose source PNGs:

| Sprite | Reuse / limits |
| --- | --- |
| `StdAssets\Popup\order_popup_canvas` | Crafting hero/order/template, clan card selection and heir selection. Separate frame and gradient sprites remain for the next frame-family pass. |
| `General\CharacterCreation\character_creation_panel` | Creation stages, banner editor, face/hair editor, save/load, education and character developer. The decorative inset stays at its original location; no controls move. |
| `General\InitialMenu\main_menu_texture` | Main-menu backing; original alpha silhouette and consumer opacity retained. |

Twenty direct XML consumers were identified locally; this is dependency evidence,
not twenty verified finished screens. Dynamic consumers and overlays still need
runtime review. Nothing imports or requires NavalDLC for this batch.

Source deployment completed 2026-10-04 (four PNG files, hash-verified). Backup:
`.codex-work/ui-asset-skin/shared-backup-20261004-001652/manifest.json`.
The user subsequently imported this sheet; save/load screenshots show the shared
outer blue/gold panel working. Other consumers are not yet runtime-verified.
This batch remains separate from the committed recruitment checkpoint.

```powershell
rtk python tools/ui_asset_skin/test_shared_surfaces.py
rtk python tools/ui_asset_skin/build_shared_surfaces.py
rtk proxy powershell -NoProfile -File tools/ui_asset_skin/deploy_shared_surfaces.ps1
```

Build inputs: existing Map PNGs and clean native PNG exports from the earlier
TPAC extraction at `.codex-work/ui-migration/compiled/Native/ui_group1_1.png`.
Missing exports must be extracted from the installed native package first, not
replaced with a painted DADG atlas. Approved reference and native-contract hashes
are pinned, so an update requires reviewing the contract before rebuilding.

Build output: `.codex-work/ui-asset-skin/shared-surfaces-01/`, including an exact
PNG comparison and JSON manifest. No browser, new sprite registration, C# build
or repacking is needed. Deploy only copies verified PNGs and backs up destinations;
the editor must subsequently re-import `ui_group1_1_tex`. Do not claim this batch
runtime-verified until actual screens are checked.

## Following batches

### Source batches deployed 2026-10-04 after save/load feedback

| Sheet / editor texture | Exact scope | Editor action |
| --- | --- | --- |
| `ui_saveload_1_tex` | Four sprites: normal/selected save rows, campaign-group header, button canvas. Two colour-only brushes neutralize the header tint and colour the scrollbar gold. | Import new `ui_saveload_1.png` source into Map; there was no Map texture for this sheet. |
| `ui_armymanagement_1_tex` | Only the paint-marked `ArmyManagement\panel_tabbar`. Existing blue/gold cards, rows and headers are byte-for-byte preserved. No direct XML consumer for this tab strip was found; this is source cleanup, not a claim of visible screen coverage. | Re-import existing Map texture. |
| `ui_quest_1_tex` | Five sprites: two headers, group toggle, quest row, shaped gold progress surround. Four small brushes remove warm colour/hue transforms, retaining native state brightness and storyline saturation differences. | Import new `ui_quest_1.png` source into Map. |

Sources are in Map `AssetSources/GauntletUI`; companion XMLs are
`GUI/Brushes/DADGSaveLoadSurface.xml` and `DADGQuestSurface.xml`.
Ten sprite rectangles total, three PNGs and two brush XMLs deployed and hash-verified.
No prefab, sprite registration, C#, DLL, TPAC or RDC changes by the source deployer.
User confirmed editor imports complete; all three Map texture packages now exist.
Brushes need a UI/editor reload to be picked up.
Do not treat these batches as runtime-verified or as completed whole screens.

Build manifests/comparisons are under `.codex-work/ui-asset-skin/` in
`saveload-surfaces-02`, `army-surfaces-03`, and `quest-surfaces-01`.
Deployment journals/backups: `shared-backup-20261004-140223`,
`shared-backup-20261004-140231`, `shared-backup-20261004-140237`.
New destinations have no prior file; the journal records a null backup.

Every changed sprite preserves its native alpha and dimensions. All other pixels
are checked unchanged. Save availability/corruption styling, banner heraldry,
quest illustration, progress fill colours, masks and icons are untouched.
These batches only reference SandBox assets; no NavalDLC dependency is introduced.

```powershell
rtk python tools/ui_asset_skin/test_shared_surfaces.py
rtk python tools/ui_asset_skin/test_surface_brushes.py
rtk python tools/ui_asset_skin/build_shared_surfaces.py --recipe tools/ui_asset_skin/saveload-surfaces.json --output .codex-work/ui-asset-skin/saveload-surfaces-02
rtk python tools/ui_asset_skin/build_shared_surfaces.py --recipe tools/ui_asset_skin/army-surfaces.json --output .codex-work/ui-asset-skin/army-surfaces-03
rtk python tools/ui_asset_skin/build_shared_surfaces.py --recipe tools/ui_asset_skin/quest-surfaces.json --output .codex-work/ui-asset-skin/quest-surfaces-01
```

Rebuild a batch before redeploying it: manifests pin the destination state and
the deployer deliberately rejects a stale manifest rather than overwrite changes.

### Campaign source batch deployed 2026-10-04

Two implementation subagents audited separate families; a third performed read-only
QA for sprite shadowing, brush inheritance and interaction-state preservation.
All 59 selected sprite names have no competing Map SpriteData registration.

| New Map texture | Sprite regions | Scope |
| --- | ---: | --- |
| `ui_inventory_1_tex` | 2 | Left/right structural headers. Warning/item rows, equipment/filter/sort controls remain native. |
| `ui_partyscreen_1_tex` | 13 | Left/right headers, collapsers and troop rows, including distinct hover/pressed/selected textures. Health, progress, icons and extension controls remain unchanged. |
| `ui_clan_1_tex` | 21 | Management/finance rows, column headers and progress surround. Existing shaped family header preserved after rejecting an unsuitable recolour. |
| `ui_kingdom_1_tex` | 23 | Ordinary list/column headers and rows, progress surrounds. Parchment decision cards, active-policy/diplomacy colours and heraldry preserved. |

These are **first imports**, not re-imports. Source PNGs are deployed to Map
`AssetSources/GauntletUI`, with matching stems minus `_tex`. No corresponding Map
source or compiled texture existed before this batch. Native sheets are unchanged.
Import into DellarteDellaGuerraMap and name the textures exactly as listed above.
On the next continuation, all four compiled Map texture packages were present.
This confirms package presence, not visual approval of those screens.

Two small Map brush files accompany the sources: `DADGClanSurface.xml` (two brushes)
and `DADGKingdomSurface.xml` (three brushes). Only palette attributes change.
The third Kingdom override explicitly preserves the war row's inherited native
default/selected tints when the ordinary row's warm tint is removed. Sounds,
geometry, disabled desaturation and interaction brightness remain unchanged.
Inventory/party need no brush overrides. Reload UI/editor to pick up new brushes.

Six source/XML files deployed and hash-verified. Journals (null backups indicate
new destinations): `shared-backup-20261004-150746`, `shared-backup-20261004-150805`,
`shared-backup-20261004-150825`, `shared-backup-20261004-150844` under
`.codex-work/ui-asset-skin/`.

Staging/report/comparison folders: `inventory-surfaces-04`, `party-surfaces-04`,
`clan-surfaces-01`, `kingdom-surfaces-01`. Numbered PNG comparison pages make larger
batches inspectable without an HTML mockup. All pages were visually reviewed.
All four builds validate native alpha, dimensions, untouched pixels and registration.
16 targeted tests passed (five rendering/selection, six previous palette-contract,
three inventory/party state/consumer, two clan/kingdom exact brush-contract tests).
Protected-sprite selections and duplicate sprite selections now fail before build.

No C#, prefab/layout, sprite registration, TPAC or RuntimeDataCache edits were made.
This is a structural-art pass, **not complete screen coverage or runtime approval**.
These assets/brushes rely only on SandBox; DLC-on/off runtime checks remain pending.
Next: editor imports, then spot-check inventory headers, party interaction states,
clan finance/members and kingdom diplomacy/selection before broadening coverage.

### Settlement overlay source batch deployed 2026-10-12

The settlement pass continues the inventory/party palette without replacing the
settlement illustration or copying the native layout. It changes exactly three
structural sprites across the shared Native sheets:

- `ui_group1_2_tex`: the settlement information header and the
  party/notable-portrait header, recoloured navy with a restrained gold edge.
  The portrait header uses its alpha silhouette so the large rounded left corner
  receives the same gold treatment as its straight edges, then reuses the native
  highlight geometry to gold the two inset corner contours without flattening them.
  Both curved masks use a half-pixel feather to avoid stair-stepping at runtime scale,
  clipped to the visible silhouette so the rounded transparent cutout stays colour-free.
  The portrait header alone rebuilds its alpha from the solid silhouette with the
  same feather, removing the native long shadow tail outside the rounded corner.
- `ui_group1_1_tex`: the existing curved party/notable group frame, recoloured
  gold while preserving its native shape and alpha.

Two exact palette-only selector overrides change the party category from orange
to blue and the character/notable category from olive to gold. The portrait-card
brushes remain separate: party cards retain their translucent blue tint, while
character/notable slots now use an opaque dark DADG navy fill instead of the
native translucent olive/grey-brown appearance. Their native default, hovered,
pressed, disabled and selected factors remain byte-for-byte equivalent. The
portrait click, encyclopedia, tooltip and quest-marker bindings are unchanged.

The left settlement action list already uses the native navy `GameMenu.Button`.
It is deliberately not overridden: `ExecuteAction`, enabled/disabled handling,
text-state selection, hints and navigation remain native. Settlement artwork,
stat icons, toggles, marble decoration, banners and the unselected atlas pixels
are explicitly outside the batch.

Four source files were deployed and hash-verified in Map: two atlas PNGs and two
brush XMLs. Backups: `shared-backup-20261012-141708`,
`shared-backup-20261012-141721`, `shared-backup-20261012-224612`, and
`shared-backup-20261013-170534`. The rounded-cutout correction was deployed from
the same batch with backups `shared-backup-20261013-181756` and
`shared-backup-20261013-182953`. The source comparisons are staged under
`.codex-work/ui-asset-skin/settlement-header-surfaces-01` and
`settlement-frame-surfaces-01`. All 43 Bannerlord-free UI asset tests pass.

No prefab, C#, registration, TPAC or RuntimeDataCache file was changed. Import
`ui_group1_2_tex` for the first time and re-import `ui_group1_1_tex` in the Map
editor before runtime review. Then spot-check town/castle/village variants,
portrait hover/selection, the party/notable toggle and action disabled states.
The later opaque portrait-fill refinement is brush-only and requires no atlas
re-import; reload the UI/editor after deploying `DADGGameMenuTableau.xml`.

### Inventory interaction-surface completion 2026-10-14

`inventory-surfaces-05` extends the earlier two-header inventory pass with 42
atlas-only regions in `ui_inventory_1`: the four left/right item-row canvases,
all 32 left/right sort-header states, and the six equipment-mode states. The
ordinary surfaces are navy, selected sort and mode states are gold, and item
rows use a restrained one-pixel gold underline rather than boxed gold frames.

The native inventory prefab and brush XML remain unchanged. Hover, pressed,
selected, unavailable and character-cannot-use semantics still come from their
original Gauntlet state mappings; in particular, the warning-red tuple tint is
preserved. Filters, equipment slots, item icons and the existing full-screen
`InventoryBackground` are outside this slice. All selected sprites preserve
their native dimensions and alpha, and every unselected atlas pixel is checked
byte-identical.

Deploy only Map `AssetSources/GauntletUI/ui_inventory_1.png`, then re-import
**`ui_inventory_1_tex`** in the editor. No new panel, prefab, UIExtender patch,
sprite registration, C#, TPAC or RuntimeDataCache change is part of this batch.
The source PNG was deployed and hash-verified on 2026-10-14. Staging and
comparisons: `.codex-work/ui-asset-skin/inventory-surfaces-05`; rollback backup
and journal: `.codex-work/ui-asset-skin/shared-backup-20261014-215934`.

### Crafting source batch deployed 2026-10-04

`crafting-surfaces-01` changes 25 regions in `ui_crafting_1`: two sculpted side
panels, the crafted-item popup, refinement header, smelting/refining row states,
order-mode controls, ten frame-only layers and the action button's two canvases.
Native relief, dimensions, alpha and every unselected pixel are preserved.
No brush XML, prefab, C#, registration or compiled-package changes were needed.
Weapon/material artwork, locked/disabled states and progress fills remain native.

Deployed source: Map `AssetSources/GauntletUI/ui_crafting_1.png`.
First import into DellarteDellaGuerraMap as **`ui_crafting_1_tex`**.
Journal: `.codex-work/ui-asset-skin/shared-backup-20261004-152614/manifest.json`;
this new source has no prior file, recorded as a null backup. Copy hash verified.

All five before/after PNG pages were visually reviewed. Independent QA decoded
the native/staged atlases and confirmed unchanged alpha/unselected RGBA, distinct
interaction states, no competing Map sprite registrations and no nine-slice
aliases for the selected sprites. Nineteen targeted tests passed (five rendering
checks and fourteen brush/scope checks). This is source validation, not runtime
approval; crafting/refining/smelting and order-mode states need an editor/game
spot check after import. Only SandBox assets are referenced, with no DLC dependency.

### Save/load header contrast correction 2026-10-04

User feedback: campaign expanders visually merged with the dark save rows.
`saveload-header-contrast-01` changes only `SaveLoad\\load_collapser` to a brighter
royal-blue palette. Its gold border, native alpha and geometry remain unchanged;
all other atlas pixels, including normal/selected save rows, are byte-identical.
Independent QA measured interior mean RGB changing from (12,38,63) to (27,78,137).
All 22 targeted tests pass. No brush or layout changes.

Deployed and committed Map source `AssetSources/GauntletUI/ui_saveload_1.png`.
**Re-import `ui_saveload_1_tex`**; its committed TPAC/cache still represent the
previous import until the user imports this correction. Backup/journal:
`.codex-work/ui-asset-skin/shared-backup-20261004-153800/manifest.json`.
This correction is layered after the original save/load recipe; when rebuilding
from scratch, apply `saveload-surfaces.json` first, then this contrast recipe.

### Slice commits 2026-10-04

| Slice | Core recipe/tooling | Map assets |
| --- | --- | --- |
| Shared surfaces and guarded pipeline | `bb99a24` | `6684269` |
| Save/load | `0729af3` | `750c888` |
| Army unfinished tab strip | `82f52e4` | `1bd44b7` |
| Quest | `1eabc4d` | `6a31d0b` |
| Inventory/party | `a19f42f` | `434f9e1` |
| Clan/kingdom | `ee813e6` | `eca6455` |
| Crafting | `54d1eee` | `1bf59ab` (source only; first import pending) |
| Save/load header contrast | `03964cf` | `3f6f153` (source only; re-import pending) |

Imported slices include their TPAC and positively associated RDC, matched using
the package UUID. Unrelated sail/ship/settlement changes and caches were excluded.
Core commits contain tools, recipes and contract checks only, not new runtime UI
code or a deployment DLL. No commits were pushed.

### Approved blue-ledger save/load treatment 2026-10-04

The user approved the HTML study after identifying the full gold rectangles
around ordinary saves as the main visual problem. `saveload-ledger.json` supersedes
the bright campaign-header experiment and replaces three sprite regions:

- Campaign heading: ink navy, one-pixel muted-gold underline, no gold box.
- Ordinary save: lighter slate blue, one-pixel blue-grey bottom separator.
- Selected save: richer blue with a two-pixel gold outline.

Native relief, silhouettes and alpha are preserved. No fonts, heraldry, prefab
layout, brush XML or native interaction/invalid-save transforms change. This is
the source implementation of the HTML palette direction, not a pixel-identical
browser renderer; Gauntlet still applies its native state transforms.

All 27 targeted Python tests passed; the generated three-row before/after sheet
was visually inspected. Staging: `.codex-work/ui-asset-skin/saveload-ledger-01`.
Apply this recipe after `saveload-surfaces.json` when rebuilding from scratch;
the intermediate `saveload-header-contrast.json` is no longer needed.
The existing neutral `DADGSaveLoadSurface.xml` is still required from the first pass.

Independent QA confirmed the three-sprite scope, exact native alpha and unchanged
unrelated RGBA/XML contracts. Deployed and hash-verified the single Map source PNG.
Backup: `.codex-work/ui-asset-skin/shared-backup-20261004-214906/manifest.json`.
The user's prior imported TPAC/cache and existing brush XML were hash-verified
unchanged and are excluded from this source-only slice. **Re-import
`ui_saveload_1_tex`** in the Map editor; in-game validation remains pending.

### Atlas exclusions confirmed 2026-10-04

- `ui_town_management_1`: all 52 sprites are building illustrations or their
  thumbnails, not structural panels. Preserve the artwork and do not introduce
  a Map override. Town-management structural sprites belong to shared sheets.
- `ui_options_1`: nine of eleven sprites are calibration/controller/network art
  or semantic memory fills. The two structural controls are an already gold
  11x27 slider handle and a muted 321x27 memory outline. No new 4096x2048 Map
  override solely for this small trim. Existing interaction factors are retained.
  Future settings work should target shared controls and account for Map's three
  existing dropdown-arrow registrations.
- `ui_launcher_1`: no in-game Native/SandBox consumer found. Mixed launcher
  branding, news and multiplayer artwork are outside this campaign/settings
  pass. Preserve the existing Map atlas.

None of these three sheets requires an import for this pass. This is deliberate
scope reduction, not a claim that every town/settings surface is already skinned.

Read-only town audit identifies the next shared-sheet candidates, all with prefix
`SPGeneral\\TownManagement\\`:

| Sheet | Structural candidates |
| --- | --- |
| `ui_group1_1` | `governor_popup`, `reserve_popup`, `title_divider`, `vertical_divider` |
| `ui_group1_2` | `horizontal_divider`, `large_project_canvas`; review selected project-ring/glow states together |
| `ui_group1_3` | `project_ring`, `reserve_slider_frame`, `level_plate` |

Preserve Map's registered `gold_frame`, hammer icons and existing
`DADG.GoldFrame.xml`. Future palette-only checks should cover
`TownManagement.GovernorPopup.Governor.Background` and the two town scrollbar
brushes' brown colour/hue transforms. The main shell inherits `Encyclopedia.Frame`
and shared popup/title art, so coordinate that change across consumers rather
than copying a town layout. These are audited candidates, not deployed changes.

### Remaining families

1. Shared gold frames, title bars, selection borders and buttons, preserving
   nine-slice thickness and normal/hover/disabled/selected semantics. Review
   native colour/stone overlays before treating a texture replacement as complete.
2. Campaign families: inventory/party, clan/kingdom, quests/encyclopedia gaps,
   town management, crafting and army management; retain already finished art.
3. Menus/options/save-load gaps and loading-art priority (separate runtime concern).
4. Optional War Sails port/naval surfaces. Verify both DLC-enabled and DLC-disabled
   configurations. No multiplayer-only screen work.

Each batch should have a short explicit manifest, source/alpha/unaffected-region
checks, source backups and a precise editor re-import list. Runtime spot checks
must cover scaling, navigation and shared-brush side effects; recruitment approval
does not establish those for every consumer.

### Banner Editor shared-control source batch

`banner-editor-surfaces-01` keeps the native Banner Editor prefab and changes four
shared `ui_group1_2` primitives only: the normal and selected `FaceGen.Card`
surfaces, the standard slider trough, and its carved frame. Cards are navy; the
selected card retains a gold surround and glow with a navy interior. The slider
keeps its native gold fill and steel handle, while its trough becomes navy and
its frame becomes muted gold. Heraldry, colour swatches, selection art, scroll
gradient, commands, navigation and geometry are protected.

`DADGBannerEditorSurface.xml` preserves the native card and scrollbar brush
structure, sounds and state set. It only reduces the very strong native card
hover/selected brightness for the new palette and changes the scrollbar handle
from brown to gold. The primitives are also used by character creation and
campaign options; those consumers must be included in the runtime spot check.

Deploy the generated `ui_group1_2.png` and brush XML to Map, then re-import
`ui_group1_2_tex` in the editor and reload the UI. No prefab, registration, C#,
TPAC or RuntimeDataCache file is authored by this batch. All 47 Bannerlord-free
UI asset tests pass before deployment.

The two source files were deployed and hash-verified on 2026-10-14. Backup and
journal: `.codex-work/ui-asset-skin/shared-backup-20261014-213307/manifest.json`.
The existing compiled package is deliberately excluded; re-import
`ui_group1_2_tex` before runtime review.
