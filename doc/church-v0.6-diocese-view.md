# Church v0.6 — Dioceses & the Church Hierarchy View

Sixth slice of the DADG religious system. The Church gets its regional structure — three
dioceses (sees) grouping the 16 church settlements — and an in-game view of it, reusing the
feudal duchy/county hierarchy screen from the `feature/add-internal-faction-wars` branch.

**Port strategy (decided after investigating the source branch):** the "duchy/county view" is
a standalone Gauntlet screen (`FeudalHierarchyScreen`) rendering an XML-defined de-jure tree as
an indented flat list — **no campaign-map overlay exists on that branch**. The view chain is
well isolated (~12 small files) but its commits carry unrelated feudal-war machinery, so we
**copy-adapt files** from the source worktree
(`src\.worktrees\add-internal-faction-wars`, commit `dd3e918`) rather than cherry-pick.
Nothing else travels: no Harmony, no save data, no custom sprites/brushes (all vanilla), no
UIExtenderEx (see §4), no `ITitleRepository`/campaign-behavior state — holders resolve live.

**Out of scope:** dioceses drawn/tinted on the campaign map (the source branch's nameplate-color
mixin pattern could do this later), an Archbishop/primate above the sees, diocese gameplay
effects (tithe rollups, bishop authority — v0.7+ candidates), encyclopedia integration
(UIExtenderEx dependency).

## 1. Dioceses in config (extend, don't duplicate)

`config/dadg.church_settlements.xml` becomes hierarchical — the existing 16 entries (with their
`kind` and v0.5's `shrine` attributes unchanged) nest under three `<Diocese>` elements instead
of being duplicated into a second structure file:

```xml
<ChurchSettlements>
  <Diocese id="see_ely" name="See of Ely">
    <ChurchSettlement id="village_Ely_Cathedral" kind="Cathedral" />
    <ChurchSettlement id="village_Walsingham_Abbey" kind="Abbey" shrine="true" />
    <ChurchSettlement id="village_Battle_Abbey" kind="Abbey" />
    <ChurchSettlement id="village_Evesham_Abbey" kind="Abbey" />
  </Diocese>
  <Diocese id="see_llandaff" name="See of Llandaff">
    <ChurchSettlement id="village_Llandaff_Cathedral" kind="Cathedral" />
    <ChurchSettlement id="village_Tintern_Abbey" kind="Abbey" />
    <ChurchSettlement id="village_Whitland_Abbey" kind="Abbey" />
    <ChurchSettlement id="village_Malmesbury_Abbey" kind="Abbey" />
    <ChurchSettlement id="village_Buckfast_Abbey" kind="Abbey" />
  </Diocese>
  <Diocese id="see_st_asaph" name="See of St Asaph">
    <ChurchSettlement id="village_St_Asaph_Cathedral" kind="Cathedral" />
    <ChurchSettlement id="village_Hexham_Abbey" kind="Abbey" />
    <ChurchSettlement id="village_Byland_Abbey" kind="Abbey" />
    <ChurchSettlement id="village_Rievaulx_Abbey" kind="Abbey" />
    <ChurchSettlement id="village_Lanercost_Priory" kind="Priory" />
    <ChurchSettlement id="village_Lindisfarne_Priory" kind="Priory" />
    <ChurchSettlement id="village_Finchale_Priory" kind="Priory" />
  </Diocese>
</ChurchSettlements>
```

- Assignment is **nearest-see on the DADG map**, not historical diocesan boundaries (St Asaph
  covering Northumbria is a map-pragmatic fiction: it is the northernmost of the three
  cathedrals we have). Ely takes the East/South-East (4), Llandaff Wales + the South-West (5),
  St Asaph the North (7).
- A diocese's **seat is derived**: the single `Cathedral`-kind member (validate exactly one per
  diocese at parse time; violation → warn + treat file as invalid, features inactive, matching
  the provider's existing degrade-gracefully contract).
- Diocese `name` stays a plain string in XML (like the feudal `titles.config.xml`); wrap for
  display without a localization id for now.
- **Format break is deliberate**: the provider parses only the nested format; the config file
  is updated in the same commit. No dual-format support.

## 2. Domain & provider

Extend the existing port rather than porting `IFeudalStructure`:

- `src\DellarteDellaGuerra.Domain\Church\Port\` — `ChurchDioceseData` DTO
  (`Id`, `Name`, list of member `ChurchSettlementData`); `IChurchSettlementsProvider` gains
  `GetDioceses()` (existing `GetChurchSettlements()` unchanged — flat view stays the same for
  all current consumers).
- `src\DellarteDellaGuerra.Domain\Church\Hierarchy\` — adapted from the source branch's
  `FeudalMap`/`FeudalMapEntry`/`BuildFeudalMapUseCase`:
  - `ChurchMapEntry` (node: id, name, `ChurchNodeRank { See, Cathedral, Abbey, Priory }`,
    settlement id, children) and `ChurchMap` (root list).
  - `BuildChurchMapUseCase` + `IBuildChurchMapUseCase` — builds the two-level tree from
    `IChurchSettlementsProvider.GetDioceses()`: one `See` root per diocese (settlement id =
    derived cathedral seat), children = the non-cathedral members ordered Abbeys before
    Priories. Earns use-case status per the clean-architecture rules: takes a port, is
    independently testable. `BuildChurchMapUseCaseTests` with a fake provider (tree shape,
    seat derivation, empty-config → empty map).
- `src\DellarteDellaGuerra.Infrastructure\Church\ChurchSettlementsXmlProvider.cs` — parse the
  nested format (dioceses + members), implement `GetDioceses()`, keep all existing warning
  behavior; new validations: unique diocese ids, exactly one cathedral per diocese, no
  settlement in two dioceses.

## 3. The screen (copy-adapt from the source worktree)

Source files under `src\.worktrees\add-internal-faction-wars\src\DellarteDellaGuerra.Integration\Titles\UI\`
and `GUI\Prefabs\FeudalTitles\`; targets:

- `src\DellarteDellaGuerra.Integration\Church\UI\ChurchHierarchyScreen.cs` — from
  `FeudalHierarchyScreen`: `ScreenBase` + `GauntletLayer("ChurchHierarchyScreen", 100)` +
  `LoadMovie("ChurchHierarchyScreen", vm)`, Esc/Close pops. Keep the source's hotkey category
  registration (`GenericPanelGameKeyCategory`).
- `ChurchHierarchyScreenVM.cs` — from `FeudalHierarchyScreenVM`: build once on open via the
  use case, flatten depth-first into `Nodes`.
- `ChurchNodeVM.cs` — from `FeudalTitleNodeVM`: `IndentMargin = depth * 45f`; row text =
  rank + name; detail line resolved live from `Campaign.Current`: seat/settlement name via
  `Settlement.Find`, plus the clergy holder — the settlement's living preacher notable with the
  v0.1 title (*Bishop Eadred of Ely Cathedral*, *Abbot Godwin of Battle Abbey*); vacant →
  *"(vacant)"* (fresh `{=id}`). No occupant/claims logic.
- `ChurchUiServices.cs` — from `FeudalUiServices`, slimmed to what the VMs need
  (`IBuildChurchMapUseCase`, `ChurchSettlements` for titles). Static locator is the accepted
  exception to the DI rule because Gauntlet instantiates VMs outside the container (same
  pattern, same justification as the source branch); initialised in `DadgSubModule`'s game
  start alongside existing wiring.
- `GUI\Prefabs\Church\ChurchHierarchyScreen.xml` (repo root `GUI\`, same convention as the
  source branch) — copy of `FeudalHierarchyScreen.xml` with the movie renamed and feudal
  strings swapped. All brushes/sprites are vanilla; nothing else ships.

## 4. Entry point — game menu, not encyclopedia

The source branch opens its screen from an encyclopedia-page button injected with UIExtenderEx.
Our branch has no UIExtenderEx and the screen doesn't need it, so **skip the dependency**:

- New `src\DellarteDellaGuerra.Integration\Church\UI\ChurchHierarchyMenuBehavior.cs`
  (lives in Integration because it pushes the screen type): `AddGameMenuOption` on the
  `"village"` menu, condition `ChurchSettlements.IsChurchSettlement(currentSettlement)`,
  text *"Survey the Church in England"* (fresh `{=id}`), consequence
  `ScreenManager.PushScreen(new ChurchHierarchyScreen())` guarded by
  `ScreenManager.TopScreen is not ChurchHierarchyScreen`. Leave-type icon: `Submenu`/default —
  match an existing vanilla option's style. Registered in `DadgSubModule` beside the church
  behaviors.
- No hotkey, no console command, no encyclopedia. If the config yields an empty map, the menu
  option is hidden (condition also checks the use case produces roots — cheap, cached).

## 5. Wiring

- `DadgServiceContainer`: register `BuildChurchMapUseCase` as `IBuildChurchMapUseCase`
  (provider is already registered).
- `DadgSubModule`: `ChurchUiServices.Initialise(...)` in campaign game start;
  add `ChurchHierarchyMenuBehavior`.
- `DellarteDellaGuerra.Integration.csproj`: add whatever TaleWorlds assembly references the
  screen needs that the project lacks (`TaleWorlds.ScreenSystem`, `TaleWorlds.GauntletUI`,
  `TaleWorlds.Engine.GauntletUI` — mirror the source branch's csproj delta, verify against it).
- No save data, no Harmony, no new config knobs (structure is data, not balance).

## 6. Verification

1. Unit: `BuildChurchMapUseCaseTests` + existing suite green (30+ after v0.5); solution builds
   Debug, 0 errors (subst `W:`, `-m:1` on the MSB4018 race).
2. Static: nested XML validates (16 settlements, 3 dioceses, one cathedral each);
   diff the Gauntlet prefab against the source to confirm only names/strings changed.
3. Live playtest (deferred with v0.1–v0.5's): at a church village the menu option appears
   (absent elsewhere); the screen shows 3 sees with indented members, live bishop/abbot names,
   *(vacant)* after a clergy death; Esc and Close both pop cleanly back to the menu.

## 7. Risks

- **GUI prefab deployment (the known silent-failure point)**: the source branch keeps `GUI\` at
  repo root with **no csproj copy target**; the game reads `<module root>\GUI\Prefabs\**`
  directly, which works when the repo is checked out at the module root (normal deployment) but
  means the screen cannot render from this nested worktree, and `LoadMovie` on a missing movie
  is a blank screen or crash. Mitigation: verify at playtest on a module-root checkout; if the
  build's module-packaging step (the one behind the MSB4018 CopyFolder quirk) needs a `GUI`
  entry, add it then.
- **Config format break**: v0.1–v0.5 code reads the settlements file through one provider, so
  the nested format lands atomically — but any user-edited flat file becomes invalid (features
  deactivate with a warning, the established failure mode).
- **Static service locator**: `ChurchUiServices` reintroduces a static seam we otherwise avoid;
  contained to UI, mirrors the source branch, and dies if we later adopt a VM-injection route.
- **GauntletLayer ctor signature** (`(name, 100)`) is the 1.4-era shape on both branches;
  re-verify against the shipped 1.4.7 (War Sails) assemblies during implementation — v0.5
  established that the live game is 1.4.7, not 1.4.6, and several 1.4.6 APIs are gone
  (see doc/church-v0.5-living-church.md §10).
