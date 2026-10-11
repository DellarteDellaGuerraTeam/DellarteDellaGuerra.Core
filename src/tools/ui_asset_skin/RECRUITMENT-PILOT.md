# Recruitment asset-skin pilot

Deployed to the game client on 2026-10-03. On 2026-10-04 the user confirmed
the final thicker gold frames look like the HTML reference in game and approved
this recruitment UI for commit. This is the accepted visual baseline for reuse,
not the completed all-screen migration.

## Runtime footprint

- Map: one `ui_dadg_recruitment` category, seven sprites, one 4096×1024 atlas.
- Map: `GUI/DadgRecruitmentSpriteData.xml` and `GUI/Brushes/DADGRecruitment.xml`.
- Core: `UI/Recruitment/RecruitmentSkinPrefabExtensions.cs` (eleven targeted
  attribute patches across four native recruitment prefabs).
- No copied native prefabs or global native sprite replacements.
- Native constants, commands, troop images, tooltips, navigation, scrolling,
  costs and footer controls remain. The approved right-panel `MarginTop`
  adjustment from 75 to 70 is the sole geometry change.
- No War Sails references or dependency introduced.

## Rebuild

From Core/src, render the approved HTML/CSS/SVG composition (without text or
mock troop rows) to the isolated generator module:

```powershell
rtk python tools/ui_asset_skin/render_recruitment.py --review .codex-work/ui-asset-skin/review --module .codex-work/ui-asset-skin/build/DadgRecruitment
```

Run the installed `TaleWorlds.TwoDimension.SpriteSheetGenerator.exe` with
`SourceDirectory=<absolute Core/src/.codex-work/ui-asset-skin/build>`,
`CollectionType=AllAvailableModules`, `OutputType=Engine`.
That staging parent must contain only the fake `DadgRecruitment` module.
The current generator's `SingleModule` option expects the GUI directory,
not the module root. Exit its completion prompt after generation.

**Never run Engine generation against the live Map module:** it deletes the
target's existing GauntletUI output before generating replacements.

Copy only the new atlas PNG to Map/AssetSources/GauntletUI. In the editor's
Resource Browser, open Map/Assets/GauntletUI, right-click the empty grid,
choose **Import new asset**, and select that PNG. The importer produces the
TPAC and RuntimeDataCache. Never edit those compiled files manually.

```powershell
rtk python tools/ui_asset_skin/check_recruitment.py
rtk dotnet build DellarteDellaGuerra.Integration/DellarteDellaGuerra.Integration.csproj --configuration Debug -p:Platform=x64 -p:BuildForWindows=false -p:BuildForWindowsStore=false -v:minimal
rtk proxy powershell -NoProfile -File tools/ui_asset_skin/deploy_recruitment.ps1
```

Deployment checks destination locks before copying, backs up existing files,
and verifies SHA-256 hashes. It defaults to client binaries only. Use
`-IncludeEditor` only after the editor is closed. The currently running editor
keeps its original integration DLL; it was used only for the texture import.
GUI source sprites are stored separately in `GUI/DadgRecruitmentSpriteParts`
to avoid mixing with the legacy generator inputs.

## Evidence and remaining check

- Build succeeded: zero errors (39 existing build warnings).
- All eleven XPath targets uniquely match installed native XML.
- All seven packed sprite rectangles exactly match rendered source pixels.
- Imported texture is visible in the editor; settings match existing DADG
  `ui_group1_1` (DXT5, generated mipmaps).
- Client deployment hashes and binary backups:
  `.codex-work/ui-asset-skin/deploy-backup-20261003-144854/manifest.json`.
- Native prefab and brush source hashes remain unchanged.

The user's first campaign screenshot confirms the pilot is loaded, but reveals
an olive tint over the portraits and faint troop-card bottom borders. The tint
comes from native `GameMenu.Tableau.Background.Character` / `.Party` brushes,
not the navy artwork. Recruitment-only attribute patches now use a transparent
idle overlay and neutral hover/pressed/selected highlights. The native unavailable
portrait greyscale and troop-state brushes remain intact. No atlas, footer or
geometry changes were made for this correction.

The tint correction built with zero errors (39 warnings) and was deployed to the
game client. Backup and verified deployment hashes:
`.codex-work/ui-asset-skin/deploy-backup-20261003-152344/manifest.json`.

The second user screenshot confirms the tint is removed, but bottom card edges
remain faint, including on empty slots. A follow-up candidate widens the gold
border inward from one to two pixels on troop, cart and notable cards, retaining
their dimensions. Following colour feedback, the flat brass `#B38D46` is replaced
by a bright gold inner highlight `#F5D675` and warm amber outer edge `#D19A32`.
The combined trim remains two pixels. The HTML review and staged atlas have been rebuilt.
The large troop-list and narrow selected-troop panel borders now reuse the actual
encyclopedia squared-banner frame: `SPGeneral\TownManagement\gold_frame`
(`gold_frame_9`, through `TownManagement.GovernorPopup.GoldFrame` in the existing
`SquareClanElementFramePatch`). The preview uses the existing 55×61 DADG source
crop with slices top/right/bottom/left 13/13/14/13, rendered at twice the trim scale
(border widths 26/26/28/26, margin compensation 10/10/12/10). The approximately
four-pixel gold band has frame-only brightness 1.12 and saturation 1.1. This
replaces flat CSS trim with the artwork's highlight, bevel and edge shading
without changing panel dimensions, padding, backgrounds or troop-card borders.
The approved source atlas and GUI sources were deployed on 2026-10-03 with
`deploy_recruitment.ps1 -GuiOnly -IncludeAtlasSource`. Ten files were backed up
and hash-verified; manifest: `.codex-work/ui-asset-skin/deploy-backup-20261003-222807/manifest.json`.
Sprite registration is unchanged. The existing `ui_dadg_recruitment_1_tex`
was subsequently re-imported in the editor. Its TPAC package UUID matches
`RuntimeDataCache/D7B327BC-34DB-4510-B0A8-7D7012CC613E.rdc`; both compiled files
are included with the approved source atlas in the Map commit.
Regression checks reject the former one-pixel border and pass for two complete
gold rows/columns on all four sides of each updated card; packed pixels match.
This is a sampling-resilience correction, not a confirmed diagnosis of a specific
engine filtering defect. The user has now approved the resulting in-game appearance.
The editor was not open for the source deployment. No compiled texture assets
or DLLs have been replaced for this border correction.

**In-game appearance approved by the user on 2026-10-04.**
Remaining regression coverage (not implied by visual approval): check long
notable names, recruitable/locked/empty slots, cart add/remove, scrollbars,
controller focus and Cancel/Reset/Done at normal and alternate UI scales.
Test with and without War Sails before calling compatibility verified.
The HTML is the design reference; the user's approval is the runtime visual evidence.
Other screens should reuse these PNG assets and conventions directly, with a new
HTML mockup only for a substantially different technical component.

The wider first-attempt branch/archive remains untouched. The existing Map
UI files and squared-banner overrides have not been replaced by this pilot.
