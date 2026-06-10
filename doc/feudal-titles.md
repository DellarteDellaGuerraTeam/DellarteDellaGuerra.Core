# Feudal Title System

DADG adds a CK3-style de jure feudal layer on top of Bannerlord's kingdom politics. A static
hierarchy of titles (Barony → County → Duchy → Kingdom; the domain also knows an Emperor rank)
is read from configuration, each title anchored to a settlement seat. On top of it the mod
tracks who *currently* holds each title, who has a *claim* on it, and how much *tension* each
unfulfilled claim has built up. The result for players:

- Conquering or being granted a settlement reassigns its title; the dispossessed clan gains a claim.
- Passed-over heirs gain inheritance claims when a clan leader dies.
- Claims accumulate tension daily; past a threshold the claimant petitions their suzerain.
- Settlement elections are decided by the de jure Duke/King instead of vanilla voting rules.
- Title rank grants influence, loyalty and diplomacy effects.
- Lords raising armies call their de jure vassals to levy, with relation consequences.
- The whole hierarchy can be exported as a markdown + mermaid map for debugging/cartography.

No Harmony patches are used for any of this — everything goes through official extension
points (campaign behaviours, game model replacement, `KingdomDecision` subclassing).

## Architecture

The feature follows the repo's hexagonal layout:

| Layer | Project / folder | Role |
| --- | --- | --- |
| Domain | `src/DellarteDellaGuerra.Domain/Titles/`, `src/DellarteDellaGuerra.Domain/Levy/` | Pure use cases, records and ports. No TaleWorlds dependency. |
| Infrastructure | `src/DellarteDellaGuerra.Infrastructure/Titles/` | Config parsing (`FeudalStructureParser`, `XmlFeudalStructure`), in-memory registries, file IO (`FeudalMapFileWriter`). |
| Game API | `src/DellarteDellaGuerra/Titles/Api/`, `src/DellarteDellaGuerra/Levy/Api/` | Bannerlord-facing adapters: campaign behaviours, kingdom decisions, game models, `FeudalServices`. |
| Integration | `src/DellarteDellaGuerra.Integration/` | DI wiring (`DI/DadgServiceContainer.RegisterTitleServices`, `RegisterLevyServices`), `SubModule.cs`, `Titles/FeudalStateStoreAdapter.cs`. |

Domain ports (`src/DellarteDellaGuerra.Domain/Titles/Port/`): `ITitleRepository`,
`IClaimRepository`, `ITensionRepository` (mutable state) and `IFeudalStructure` (the immutable
de jure tree). Infrastructure implements them with plain dictionaries
(`InMemoryTitleRegistry`, `InMemoryClaimRegistry`, `InMemoryTensionRegistry`) — the campaign
tick is single-threaded, so no locking is needed.

### Extension points used (instead of Harmony)

- **Game model replacement** — `SubModule.InitializeGameStarter` calls
  `campaignGameStarter.AddModel(...)` with `DadgDiplomacyModel`, `DadgClanPoliticsModel` and
  `DadgSettlementLoyaltyModel` (`src/DellarteDellaGuerra/Titles/Api/GameModels/`), each
  subclassing the vanilla default model and delegating to `base` first.
- **`CampaignBehaviorBase`** — `FeudalTitleCampaignBehavior`, `InternalConflictCampaignBehavior`,
  `FeudalTitleSwapBehavior` (`src/DellarteDellaGuerra/Titles/Api/Campaign/`) and
  `LevyCampaignBehavior` (`src/DellarteDellaGuerra/Levy/Api/`) are added via
  `campaignGameStarter.AddBehavior(...)`.
- **`KingdomDecision` subclassing** — `FeudalSettlementClaimantDecision` extends the vanilla
  `SettlementClaimantDecision`; `FeudalPetitionDecision` is a brand-new decision
  (`src/DellarteDellaGuerra/Titles/Api/Election/`).
- **Decision swap via `CampaignEvents.KingdomDecisionAdded`** — `FeudalTitleSwapBehavior`
  listens for every vanilla `SettlementClaimantDecision`, removes it from the kingdom and
  re-adds a `FeudalSettlementClaimantDecision` built from the same proposer/settlement/exclusion.

### Why `FeudalServices` is a static locator

`src/DellarteDellaGuerra/Titles/Api/FeudalServices.cs` is a static service locator populated
once in `SubModule.InitializeGameStarter` from the DI container. This is deliberate:
Bannerlord serialises `KingdomDecision` instances (and their `DecisionOutcome`s) into save
games. A decision deserialised from a save is constructed by the save system, so it cannot
receive constructor-injected services. Decisions and replaced game models therefore resolve
collaborators from `FeudalServices` and must null-check (or check
`FeudalServices.IsInitialised`), falling back to vanilla behaviour when uninitialised
(unit tests, unexpected load order). Everything else in the system uses normal constructor
injection via `DadgServiceContainer`.

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

`XmlFeudalStructure.BuildInitialTitles()` turns the configured tree into the initial `Title`
records used to seed a new campaign.

## Gameplay mechanics

### Titles and assignment

`Title` (`Domain/Titles/Model/Title.cs`) is `(Id, Name, Rank, SeatSettlementId, HolderClanId)`
with ranks `Baron < Count < Duke < King < Emperor`. When a settlement changes owner,
`FeudalTitleCampaignBehavior.OnSettlementOwnerChanged` runs `AssignTitleUseCase`: the title
whose seat is that settlement moves to the new owner clan, and the previous holder (if any)
receives a **Strong Conquest claim** on it (deduplicated by claim id
`{titleId}:{clanId}:conquest`).

### Claims

`Claim` carries a `ClaimStrength` (Weak / Strong / DeJure) and `ClaimOrigin`
(Inheritance / Conquest / Marriage / Purchase). Besides conquest claims,
`GenerateInheritanceClaimsUseCase` fires on a clan leader's death
(`FeudalTitleCampaignBehavior.OnHeroKilled`): living children, siblings and spouse who belong
to *other* clans each gain a Strong Inheritance claim on every title the deceased's clan holds.

### Tension and petitions

`InternalConflictCampaignBehavior` ticks daily per clan: each outstanding claim accumulates
tension via `AccumulateTensionUseCase` at a base rate of 1/day, scaled by claim strength
(Weak ×0.5, Strong ×1.0, DeJure ×1.5). Tension resets to zero if the claimant gains the title.
At tension ≥ 10 a `FeudalPetitionDecision` is raised in the claimant's kingdom (one pending
petition per clan at a time), asking the de jure suzerain to rule on the claim:

- **Granted** — the claim is upgraded to at least Strong and the tension resets.
- **Denied** — `SurgeTensionOnDenial` pumps a one-off ×5 tension accumulation, pushing the
  clan towards defection (see diplomacy model below).

The chooser is the suzerain clan (falling back to the kingdom's ruling clan if eliminated);
support for granting scales with claim strength (DeJure 0.6, Strong 0.35, else 0.15).

### Elections

`FeudalSettlementClaimantDecision` replaces every vanilla settlement claimant election (via
`FeudalTitleSwapBehavior`). `DetermineChooser` walks the de jure chain upward from the
settlement's title and hands the choice to the first **Duke, King or Emperor** holder it finds
— ordinary lords no longer pick the winner. `DetermineSupport` multiplies vanilla support by a
feudal-distance weight from `ComputeFeudalSupportUseCase` (suzerain ×3.0, de jure peer ×1.5,
outsider ×0.2, no title on the settlement ×1.0), and clans with claims on the settlement's
title are injected as extra candidates.

### Model effects

- `DadgClanPoliticsModel` — daily influence bonus by highest held rank (Baron +0.5, Count +1,
  Duke +2, King +3, Emperor +4, from `ComputeInfluenceTierBonusUseCase`); influence cost to
  override kingdom decisions is ×0.7 for high-tier proposers (>2) and ×1.5 for low ones (<0.5).
- `DadgDiplomacyModel` — each point of accumulated tension adds 0.5 to a clan's score to
  leave its kingdom; hourly army-member influence is multiplied by the feudal tier bonus.
- `DadgSettlementLoyaltyModel` — +1 daily loyalty when the holder also has a DeJure claim on
  the seat's title; −1 ("Contested Title") when someone *else* holds a DeJure claim on it.

### Levies

`LevyCampaignBehavior` (`src/DellarteDellaGuerra/Levy/Api/LevyCampaignBehavior.cs`): when a
lord creates an army, `IssueLevyUseCase` opens a `LevyCall` for every de jure vassal clan in
the same kingdom (holders of titles directly under the lord's titles). A vassal party joining
the army marks the call **Answered** (+2 relation lord→vassal); calls still pending after
**3 days** (`LevyExpiryDays`) are marked **Refused** by `ExpireLeviesUseCase` (−5 relation
both ways). Dispersing the army clears the lord's outstanding calls.

### Cartography (feudal map export)

`BuildFeudalMapUseCase` walks the de jure structure and overlays current holders into a
`FeudalMap` forest; `RenderFeudalMapUseCase` renders it as markdown and as a mermaid
`flowchart TD`. `FeudalMapFileWriter` (`Infrastructure/Titles/FeudalMapFileWriter.cs`) writes
both into `feudal-map.md` in the mod's log folder (`CreateInLogFolder()`). The pipeline is
currently invoked on demand (see `src/DellarteDellaGuerra.Domain.Tests/Titles/`) and is not
yet registered in the DI container or triggered by a campaign behaviour.

## Save / load

Two complementary mechanisms persist the state:

- **`SyncData` string lists** — domain records carry no `SaveableField` attributes. Instead
  `TitleStateSerialiser` (`src/DellarteDellaGuerra/Titles/Spi/`) and `LevyStateSerialiser`
  (`src/DellarteDellaGuerra/Levy/Spi/`) flatten them to pipe-delimited strings.
  `FeudalTitleCampaignBehavior.SyncData` stores them under the keys `DadgFeudalTitles`,
  `DadgFeudalClaims` and `DadgFeudalTensions`; `LevyCampaignBehavior.SyncData` uses
  `DadgLevyCalls`. On load the registries are re-initialised through `IFeudalStateStore`
  (adapted by `Integration/Titles/FeudalStateStoreAdapter.cs`); malformed lines are skipped.
  Saves with no feudal state (mod added mid-campaign) are seeded from the config instead.
- **`SaveableTypeDefiner`** — `FeudalElectionSaveDefiner`
  (`src/DellarteDellaGuerra/Titles/Api/Election/FeudalElectionSaveDefiner.cs`) registers the
  decision types under base id `2_887_350`: `FeudalSettlementClaimantDecision` (1),
  `FeudalPetitionDecision` (2), `GrantClaimOutcome` (3), `DenyClaimOutcome` (4).
  `FeudalPetitionDecision` keeps only `string` `SaveableField`s (`_titleId`,
  `_suzerainClanId`) and resolves services from `FeudalServices` after deserialisation.

Do not renumber the definer base id or the per-type ids, and do not reorder/remove fields of
the serialiser formats without a migration path — both would break existing saves.

## Extending the system

- **New tier** — `TitleRank` already contains `Emperor`, but `FeudalStructureParser.ParseRank`
  only maps `Kingdom/Duchy/County/Barony` elements. Add an `Empire` element case there, decide
  its holder attribute, and review the rank switches: `ComputeInfluenceTierBonusUseCase`
  (influence per tier) and `FeudalSettlementClaimantDecision.DetermineChooser` (which ranks may
  choose) already handle Emperor.
- **New claim origin/strength** — extend the enums in `Domain/Titles/Model/`; the serialisers
  round-trip them by name, so old saves remain loadable as long as existing names are kept.
- **New mechanic** — add a use case + port in `Domain/Titles/`, an adapter in Infrastructure if
  it needs IO, register both in `DadgServiceContainer.RegisterTitleServices`, and drive it from
  a `CampaignBehaviorBase` added in `SubModule.InitializeGameStarter`. Only expose it through
  `FeudalServices` if it must be reachable from save-deserialised objects (decisions, models);
  in that case extend `FeudalServices.Initialise`/`Reset` and keep the null-check convention.
- **New persistent state** — extend `IFeudalStateStore` and the serialisers with a new
  `SyncData` key rather than changing the format of an existing one.
- **Tuning** — thresholds live as constants next to their behaviour:
  `InternalConflictCampaignBehavior` (tension threshold 10, daily rate 1),
  `LevyCampaignBehavior` (3-day expiry, +2/−5 relations), `ComputeFeudalSupportUseCase`
  (support weights), `AccumulateTensionUseCase` (strength factors).

Tests live in `src/DellarteDellaGuerra.Domain.Tests/Titles/`,
`src/DellarteDellaGuerra.Domain.Tests/Levy/` and
`src/DellarteDellaGuerra.Infrastructure.Tests/Titles/` — domain logic is plain C#, so new
mechanics should come with unit tests that need no game runtime.
