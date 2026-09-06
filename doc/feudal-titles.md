# Feudal Title System

DADG adds a CK3-style de jure feudal layer on top of Bannerlord's kingdom politics. A static
hierarchy of titles (Barony → County → Duchy → Kingdom; the domain also knows an Emperor rank)
is read from configuration, each title anchored to a settlement seat. On top of it the mod
tracks which *hero* currently holds each title, which clan is *occupying* its seat when the two
diverge, and who has a *claim* on it. The result for players:

- Conquering a seat occupies it without moving the dignity; a grant, gift or barter transfers the
  dignity outright and the dispossessed clan gains a claim.
- Making peace regularises a standing occupation into a transfer (uti possidetis).
- When a title-holder dies his dignities pass by male-preference primogeniture — see
  [`feudal-succession.md`](feudal-succession.md).
- Every living relative in reach of a holder or the holder's father carries an inheritance claim,
  including the holder's own brothers and passed-over sons.
- The de jure hierarchy is browsable in-game through the encyclopedia and a dedicated screen.

No Harmony patches are used for any of this — everything goes through official extension points
(a campaign behaviour, plus UI mixins and prefab extensions for the encyclopedia pages).

## Architecture

The feature follows the repo's hexagonal layout:

| Layer | Project / folder | Role |
| --- | --- | --- |
| Domain | `src/DellarteDellaGuerra.Domain/Titles/` | Pure use cases, records and ports. No TaleWorlds dependency. |
| Infrastructure | `src/DellarteDellaGuerra.Infrastructure/Titles/` | Config parsing (`FeudalStructureParser`, `XmlFeudalStructure`), in-memory registries, the live-campaign genealogy adapter, file IO (`FeudalMapFileWriter`). |
| Game API | `src/DellarteDellaGuerra/Titles/Api/` | Bannerlord-facing adapters: `FeudalTitleCampaignBehavior`, `FeudalServices`, `FeudalTitleKingdoms`. |
| Integration | `src/DellarteDellaGuerra.Integration/` | DI wiring (`DI/DadgServiceContainer.RegisterTitleServices`), `SubModule.cs`, `Titles/FeudalStateStoreAdapter.cs`, the feudal UI under `Titles/UI/`. |

Domain ports (`src/DellarteDellaGuerra.Domain/Titles/Port/`): `ITitleRepository` and
`IClaimRepository` (mutable state), `IFeudalStructure` (the immutable de jure tree) and
`IGenealogy` (read-only access to the bloodlines that claim derivation and succession walk).
Infrastructure implements the first two with plain dictionaries (`InMemoryTitleRegistry`,
`InMemoryClaimRegistry`) — the campaign tick is single-threaded, so no locking is needed — and
`IGenealogy` with `CampaignGenealogy`, which reads `Hero` straight out of `MBObjectManager`.

### Extension points used (instead of Harmony)

- **`CampaignBehaviorBase`** — `FeudalTitleCampaignBehavior`
  (`src/DellarteDellaGuerra/Titles/Api/Campaign/`) is added via
  `campaignGameStarter.AddBehavior(...)` in `SubModule.InitializeGameStarter`. It is the only
  campaign behaviour the feudal system registers.
- **UI mixins and prefab extensions** — `EncyclopediaClanPageMixin`, `EncyclopediaHeroPageMixin`
  and the matching `PrefabExtensions` (`src/DellarteDellaGuerra.Integration/Titles/UI/`) add
  feudal information to the vanilla encyclopedia pages.

### Why `FeudalServices` is a static locator

`src/DellarteDellaGuerra/Titles/Api/FeudalServices.cs` is a static service locator populated
once in `SubModule.InitializeGameStarter` from the DI container, and
`src/DellarteDellaGuerra.Integration/Titles/UI/FeudalUiServices.cs` is its counterpart for the UI.
Both exist for the same reason: consumers the *game* constructs rather than the DI container —
static helpers such as `FeudalTitleKingdoms`, and the encyclopedia mixins and screens the UI
machinery instantiates — cannot receive constructor-injected services. Every consumer must
null-check (or check `IsInitialised`) and fall back to vanilla behaviour when the locator has not
been initialised (unit tests, unexpected load order). Everything else in the system uses normal
constructor injection via `DadgServiceContainer`.

## Configuration: `config/titles.config.xml`

`FeudalStructureConfigReader` (Infrastructure) loads `titles.config.xml` from the mod's config
folder and `FeudalStructureParser` parses it into a forest of `FeudalTitleNode`s, which backs
`XmlFeudalStructure : IFeudalStructure`. A missing or invalid file logs an error and yields an
empty hierarchy (the feature degrades to vanilla behaviour).

```xml
<Feudalism>
  <Kingdom id="kingdom_england" name="England" seat="town_london" kingClanId="clan_lancaster">
    <Duchy id="duchy_york" name="Duchy of York" seat="town_york" holderClanId="clan_york">
      <County id="county_richmond" name="County of Richmond" seat="town_richmond" holderClanId="clan_neville">
        <Barony id="barony_middleham" name="Barony of Middleham" seat="castle_middleham"/>
      </County>
    </Duchy>
    <!-- Counties may also sit directly under the crown -->
    <County id="county_kent" name="County of Kent" seat="town_canterbury" holderClanId="clan_woodville"/>
  </Kingdom>
</Feudalism>
```

Rules enforced by the parser (`FeudalStructureParser.cs`):

- Element name determines rank: `Kingdom`→King, `Duchy`→Duke, `County`→Count, `Barony`→Baron.
  Any other element name throws.
- `id` is required and must be unique across all titles.
- `seat` (a settlement `StringId`) is optional but must be unique when present.
- The initial holder attribute is `kingClanId` on `Kingdom` and `holderClanId` on every other
  rank; both are optional (the title starts vacant).
- `name` defaults to the `id` when omitted.

`XmlFeudalStructure.BuildInitialTitles(IGenealogy)` turns the configured tree into the initial
`Title` records used to seed a new campaign. The config names a *clan*, but a title is held by a
hero, so each configured clan is resolved to its head at seed time.

## Gameplay mechanics

### Titles and assignment

`Title` (`Domain/Titles/Model/Title.cs`) is
`(Id, Name, Rank, SeatSettlementId, HolderHeroId, OccupantClanId, ContestedSinceDay)` with ranks
`Baron < Count < Duke < King < Emperor`. `HolderHeroId` is the de jure holder — a hero, whose clan
is derived through `IGenealogy.GetClanOf` rather than stored. `OccupantClanId` is the de facto
holder of the seat when the two diverge, and stays a clan because occupation derives from
`Settlement.OwnerClan`, which has no hero equivalent.

`FeudalTitleCampaignBehavior.OnSettlementOwnerChanged` classifies the engine's transfer detail into
a `SeatTransferKind` and runs `AssignTitleUseCase`:

- **Conquest** (`BySiege`, `ByRebellion`, and `ByKingDecision` when the new owner is outside the
  title's own kingdom) — a *held* dignity does not move. The title is marked contested
  (`OccupantClanId`, `ContestedSinceDay`) and the holder keeps it. A vacant title has no holder to
  dispossess, so conquest transfers it directly.
- **Grant** (`ByGift`, `ByBarter`, and `ByKingDecision` within the title's own kingdom) — the
  dignity transfers to the new owner clan's head, any contest is cleared, and the previous holder's
  clan gains a **Strong Conquest claim** (deduplicated by claim id `{titleId}:{clanId}:conquest`).

`OnMakePeace` closes the loop: when the title's kingdom makes peace with the faction occupying the
seat, the occupation is regularised as a grant.

### Succession

Handled by `ExecuteSuccessionUseCase` on `HeroKilledEvent`, before claims are re-derived.
Male-preference primogeniture with representation: the holder's own line first, then his siblings
through his father, then his clan head as a backstop, then vacancy. Full reference:
[`feudal-succession.md`](feudal-succession.md).

### Claims

`Claim` carries a `ClaimStrength` (Weak / Strong / DeJure), a `ClaimOrigin`
(Inheritance / Conquest / Marriage / Purchase), a claimant clan and — for blood claims — a claimant
hero. Conquest claims name no hero and stay clan-level.

`GenerateBloodClaimsUseCase` rebuilds every `Inheritance` claim in the realm from scratch whenever
the bloodlines or the holders move: new campaign, save load, settlement owner change, peace
cession, and any hero death. Each title is anchored at **its holder and the holder's father**, the
same two walks succession makes, so every claimant is a hero succession could have picked.
Strength decays down each generation from an anchor:

- a holder's son takes a Strong claim, his daughter a Weak one;
- a Strong claimant's children take a Weak claim, son or daughter alike;
- a Weak claim passes to nobody.

So a claim reaches at most two generations from an anchor, and a female link costs a generation
without cutting the line outright. Everyone but the holder himself qualifies, his own kin included
— `EvaluateClaimUseCase` bars only the holder, comparing heroes for a blood claim and falling back
to a clan comparison for a conquest claim.

### Cartography (feudal map export)

`BuildFeudalMapUseCase` walks the de jure structure and overlays current holders into a
`FeudalMap` forest. It is registered in the DI container and drives the in-game hierarchy screen
(`Integration/Titles/UI/FeudalHierarchyScreen.cs`) through `FeudalUiServices`.
`RenderFeudalMapUseCase` (markdown + a mermaid `flowchart TD`) and `FeudalMapFileWriter`
(`Infrastructure/Titles/FeudalMapFileWriter.cs`, writing `feudal-map.md` into the mod's log folder)
exist but are exercised only from tests — neither is registered or triggered by a behaviour.

## Save / load

Domain records carry no `SaveableField` attributes. Instead `TitleStateSerialiser`
(`src/DellarteDellaGuerra/Titles/Spi/`) flattens them to pipe-delimited strings and
`FeudalTitleCampaignBehavior.SyncData` stores them under the keys `DadgFeudalTitles` and
`DadgFeudalClaims`. On load the registries are re-initialised through `IFeudalStateStore` (adapted
by `Integration/Titles/FeudalStateStoreAdapter.cs`); malformed lines are skipped. Saves with no
feudal state (mod added mid-campaign) are seeded from the config instead.

Blood claims are the exception to restoring: they are a pure function of the bloodlines and the
holders, so `OnGameLoaded` always rebuilds them rather than trusting what the save carried. A save
written before the system existed carries none, and one written under older rules carries claims
the current rules would not grant.

Do not reorder or remove fields of the serialiser formats without a migration path — that would
break existing saves.

## Extending the system

- **New tier** — `TitleRank` already contains `Emperor`, but `FeudalStructureParser.ParseRank`
  only maps `Kingdom/Duchy/County/Barony` elements. Add an `Empire` element case there and decide
  its holder attribute; `FeudalTitleKingdoms` already treats Emperor as a crown rank.
- **New claim origin/strength** — extend the enums in `Domain/Titles/Model/`; the serialisers
  round-trip them by name, so old saves remain loadable as long as existing names are kept.
- **New mechanic** — add a use case + port in `Domain/Titles/`, an adapter in Infrastructure if
  it needs IO, register both in `DadgServiceContainer.RegisterTitleServices`, and drive it from
  `FeudalTitleCampaignBehavior` (or a new `CampaignBehaviorBase` added in
  `SubModule.InitializeGameStarter`). Only expose it through `FeudalServices` / `FeudalUiServices`
  if it must be reachable from objects the game constructs, and keep the null-check convention.
- **New persistent state** — extend `IFeudalStateStore` and `TitleStateSerialiser` with a new
  `SyncData` key rather than changing the format of an existing one.
- **Tuning** — the surviving constants live next to their behaviour: the claim decay levels in
  `GenerateBloodClaimsUseCase`, the transfer-kind mapping in
  `FeudalTitleCampaignBehavior.OnSettlementOwnerChanged`.

Tests live in `src/DellarteDellaGuerra.Domain.Tests/Titles/` and
`src/DellarteDellaGuerra.Infrastructure.Tests/Titles/` — domain logic is plain C#, so new
mechanics should come with unit tests that need no game runtime. The infrastructure tests
additionally run the real 1471 content, which needs the sibling `DellarteDellaGuerraMap` module
(located by walking up for a `Modules` directory, or by setting `DADG_MODULES_ROOT`).
