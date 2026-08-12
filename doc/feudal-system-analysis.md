# Feudal Title System — Clash Audit & Intra-Faction War Analysis

> **Status:** Analysis / design document. No code changes have been made as a result of this report.
> **Branch:** `claude/banner-lord-mcp-integration-79WN8`
> **Date:** 2026-06-10
> **Scope:** (1) Where the new CK3-style feudal title system clashes with existing Bannerlord
> and DADG mechanics; (2) feasibility and recommended design for *internal wars within the same
> faction* (e.g. two Counts fighting while both remain de jure vassals of the same Duke).
[feudal-hero-titles-and-houses.md](feudal-hero-titles-and-houses.md)
---

## Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [Part A — Clashes With Existing Mechanics](#2-part-a--clashes-with-existing-mechanics)
   - [Critical findings](#critical-findings)
   - [Moderate findings](#moderate-findings)
   - [Low findings](#low-findings)
   - [Refuted suspicions](#refuted-suspicions)
   - [Items to verify in-game](#items-to-verify-in-game)
3. [Part B — Intra-Faction Private Wars](#3-part-b--intra-faction-private-wars)
   - [The core obstacle](#the-core-obstacle-mapfaction)
   - [Verified facts](#verified-facts)
   - [Design options evaluated](#design-options-evaluated)
   - [Recommended design](#recommended-design-abstract-private-war)
   - [Domain & integration sketch](#domain--integration-sketch)
4. [Part C — Recommended Order of Work](#4-part-c--recommended-order-of-work)
5. [Appendix — Verification Sources](#5-appendix--verification-sources)

---

## 1. Executive Summary

The feudal title system is architecturally sound (clean hexagonal layout, zero Harmony patches),
but the audit surfaced **two outright bugs**, **one structural balance failure**, and several
player-facing rough edges. None are crashes; all have one- or few-file fixes consistent with the
zero-Harmony constraint.

On the headline feature request — two clans in the *same* kingdom waging war on each other — the
key finding is a hard engine constraint: **Bannerlord resolves all map hostility through
`MapFaction`, which for a clan inside a kingdom is the kingdom itself.** Two same-kingdom clans can
therefore never be mutually hostile on the map. Real on-map battles between them are not achievable
without Harmony. The recommended path is an **abstract, CK3-style "private war"** resolved through
official campaign actions — which actually satisfies the requirement *better* than the alternative,
because both clans literally remain kingdom members throughout.

| Area | Finding | Severity | Fix size |
|---|---|---|---|
| Levy | Refusal penalties silently dropped for almost every lord | Critical | 1 file |
| State | Cross-campaign contamination via singleton registries | Critical | small |
| Balance | Runaway defection feedback loop (uncapped tension) | Critical | medium |
| Petitions | Petition spam after denial | Moderate | small |
| Elections | Decision swap drops `capturerHero` | Moderate | small |
| Levy | Penalises vassals who cannot comply (incl. player) | Moderate | medium |
| Titles | Dead clans hold titles forever | Moderate | small |
| Internal war | Not possible on-map without Harmony → use abstract war | — | new feature |

---

## 2. Part A — Clashes With Existing Mechanics

Findings are ordered by severity. Each cites `file:line` evidence in the repo. Items that depend on
TaleWorlds internals not visible in the reference assemblies are flagged **UNVERIFIED**.

### Critical findings

#### C1 — Levy refusal penalties are silently dropped for almost every lord

**Evidence:** `src/DellarteDellaGuerra/Levy/Api/LevyCampaignBehavior.cs:110-114` +
`src/DellarteDellaGuerra.Domain/Levy/ExpireLeviesUseCase.cs:22-32`

`ExpireLeviesUseCase.Execute` iterates `GetAllLevyCalls()` **globally**, flips *every* stale
`Called` call to `Refused`, persists it, and returns the full set. `OnDailyTickClan` then filters:

```csharp
foreach (var call in refused)
{
    if (call.IssuingClanId != clan.StringId) continue;   // <-- drops everyone else's
    ...
    ApplyRefusalRelations(clan, vassalClan);
}
```

`DailyTickClanEvent` fires per clan in sequence. The **first** clan ticked each day expires *all*
clans' stale calls. By the time any other lord's tick runs, `Execute` finds nothing still `Called`
for them and returns an empty set — so their −5/−5 relation penalty never applies. The entire
deterrent value of the levy system fires only for whichever clan happens to tick first.

**Fix (zero Harmony):** give the use case an issuing-clan scope —
`Execute(string issuingClanId, float currentDay, float expiryDays)` filtering by lord — *or* move
expiry to a single `CampaignEvents.DailyTickEvent` and apply penalties for **all** returned calls in
one pass (resolve each `IssuingClanId` → clan rather than filtering against the ticking clan).

#### C2 — Cross-campaign state contamination via never-cleared singleton registries

**Evidence:** `src/DellarteDellaGuerra.Integration/DI/DadgServiceContainer.cs:123-128`
(singleton `InMemoryTitleRegistry` / `Claim` / `Tension`); container built once in
`SubModule.OnSubModuleLoad` (`SubModule.cs:80`);
`FeudalTitleCampaignBehavior.cs:71` (`OnNewGameCreated`:
`if (_stateStore.SnapshotTitles().Count > 0) return;`) and `:80-88` (`OnGameLoaded` seeds only when
empty).

The registries live for the whole game **process**. Play campaign A, exit to the main menu, start
campaign B (or load a pre-mod save): `OnNewGameCreated` / `OnGameLoaded` see leftover titles from A
and **skip seeding entirely**, so B runs with A's holders, claims and tensions. The seed branch never
clears claims/tensions from a previous session. (The Levy side is safe only because its `SyncData`
always re-`Initialise`s on load — `LevyCampaignBehavior.cs:59-60`.)

**Fix:** add `Reset()` to `IFeudalStateStore`; call it first in both `OnNewGameCreated` and
`OnGameLoaded` (and/or hook `CampaignEvents.OnGameEndEvent`). Call `FeudalServices.Reset()` on game
end for symmetry.

#### C3 — Unbounded tension + permanent claims = runaway defection feedback loop

**Evidence:** `AssignTitleUseCase.cs:43-55` (every ownership change mints a permanent Strong conquest
claim; claims are never removed — grant only *upgrades*, `FeudalPetitionDecision.cs:152-174`);
`AccumulateTensionUseCase.cs:19-36` (tension resets **only** if the claimant currently holds the
title — otherwise grows forever, no cap, no decay);
`DadgDiplomacyModel.cs:17-29` (`+0.5 × tension` per claim, uncapped, added to vanilla
`GetScoreOfClanToLeaveKingdom`);
`InternalConflictCampaignBehavior.cs:48` (petition release valve requires the suzerain to be in the
*same* kingdom — gone after defection or suzerain elimination).

The loop is structural:

```
conquest → permanent claim → daily tension (uncapped)
        → inflated leave-kingdom score → clan defects
        → new wars/conquests → more ownership changes
        → more claims for everyone → scores grow monotonically
```

Late campaign, every clan with old claims defects on cooldown, swamping vanilla kingdom stability.
(Exact magnitude versus vanilla's score scale is **UNVERIFIED**, but unbounded growth dominates any
finite scale eventually.)

**Fix:** clamp tension (e.g. at `threshold × 2`); add daily decay; remove or downgrade claims when
the title is regained/granted or after *N* years; clamp the diplomacy bonus
(`Math.Min(tensionBonus, cap)`).

### Moderate findings

#### M1 — Decision swap: no loop, but `capturerHero` is dropped (and a UI race exists)

**Evidence:** `FeudalTitleSwapBehavior.cs:21-32`

- **Infinite loop — REFUTED.** Re-adding does re-dispatch `KingdomDecisionAdded`, but the guard at
  `:21` (`is FeudalSettlementClaimantDecision` → return) terminates recursion after exactly one
  re-entry.
- **Influence double-charge — REFUTED.** `AddDecision(feudalDecision, true)` skips the cost on
  re-add. Whether `RemoveDecision` refunds an already-charged original is **UNVERIFIED**, but the
  net effect is benign because the replacement is equivalent.
- **Real bug:** `:28` passes `capturerHero: null`, discarding the capturer that vanilla
  `SettlementClaimantDecision` uses for capturer merit/candidacy. **The conqueror loses their
  election edge in every swapped election.** Fix: read the capturer from the original decision (or
  capture it in `OnSettlementOwnerChanged`, keyed by settlement).
- **Player-UI race (UNVERIFIED):** listener order across behaviors/mods is not guaranteed; if the
  election/notification listener runs before this handler, the player could briefly hold a panel
  referencing a removed decision. Mitigation: defer the swap one tick (queue + process on
  `TickEvent`/`HourlyTick`), or leave player-proposed decisions alone.

#### M2 — Petition spam after denial

**Evidence:** `InternalConflictCampaignBehavior.cs:29-31, 50-53`; `FeudalPetitionDecision.cs:176-182`

Denial *raises* tension (surge ×5) and nothing lowers it below the threshold. The only thing
blocking a new petition is the `petitionAlreadyPending` check — so the day after a denial resolves,
a fresh petition is raised. Endless petition → denial → surge cycle every decision cycle, spamming
the kingdom decision queue and (when the player is suzerain) the player's notifications.

**Fix:** on denial, reset tension to a sub-threshold value, *or* record a per-claim cooldown
timestamp checked in `TryTriggerPetition`.

#### M3 — Levies punish vassals who cannot comply (including around the player)

**Evidence:** `LevyCampaignBehavior.cs:63-74` (issues calls on every `ArmyCreated`, no exemptions),
`:104-122` (penalty path)

No checks for: vassal leader imprisoned, party besieged/destroyed, clan already in another army,
clan on the far side of the map, or clan being the player. Two player-facing problems:

- **(a)** Every army the *player* creates issues calls; AI vassals that don't independently join
  within 3 days each cost −5/−5 with the player — uncontrollable relation bleed.
- **(b)** The player as a de jure vassal silently takes −5/−5 for not joining an AI army they may
  never have been notified about (no notification path exists in this behavior).

**Fix:** skip vassals whose leader `IsPrisoner`, whose clan has no usable party or is in another
army; gate player-affecting calls behind an inquiry/notification.

#### M4 — Dead-clan title holders degrade gracefully, but into a tension dead end

**Evidence:** `AssignTitleUseCase.cs:33-40`; consumers all filter `!c.IsEliminated`
(`FeudalSettlementClaimantDecision.cs:43-46`, `InternalConflictCampaignBehavior.cs:46-48`,
`LevyCampaignBehavior.cs:163-167`, `FeudalPetitionDecision.cs:75-76`)

No crashes from stale ids — confirmed graceful. **But:** a rebel clan seizing a seat becomes the de
jure holder (ownership change ignores `ChangeOwnerOfSettlementDetail`,
`FeudalTitleCampaignBehavior.cs:95-104`), and when a holder/suzerain clan dies, claimants' tension
keeps accruing daily with the petition valve permanently shut (the C3 dead end), and the title stays
pointed at a dead clan forever.

**Fix:** handle `CampaignEvents.OnClanDestroyedEvent` to vacate titles held by eliminated clans (set
holder to null); extend the suzerain walk to skip vacant links.

#### M5 — Petition decision rendering & scale when the player is chooser — UNVERIFIED

**Evidence:** `FeudalPetitionDecision.cs:18` (`EmptyImageIdentifier : ImageIdentifier`),
`:22-33`, `:62-69, 143-147`

When the player clan is suzerain, vanilla opens the generic kingdom-decision Gauntlet screen.
Whether `ImageIdentifierVM` tolerates an unknown `ImageIdentifier` subclass is **UNVERIFIED** —
possible blank tile or crash. Also: `new TextObject($"{ProposerClan?.Name}…")` bakes resolved names
(loses localisation; literal `{`/`}` in a clan name would be parsed as TextObject tokens).
`DetermineSupport` returns 0.1–0.6 while vanilla decisions return larger merit-scaled values — a
possible scale mismatch that could make AI never spend influence on petitions.

**Fix:** use a concrete vanilla `ImageIdentifier` (e.g. the kingdom-banner identifier used by
`ClanAsDecisionOutcome`); use proper `TextObject` variables via `SetTextVariable`; override the
cancellation hook to drop petitions whose proposer left/died; benchmark support values against
`SettlementClaimantDecision`.

### Low findings

#### L1 — `CampaignBehaviourDisabler` — REFUTED

**Evidence:** `src/DellarteDellaGuerra/DisableNativeBehaviour/MissionBehaviours/CampaignBehaviourDisabler.cs:26-28`

Only `BackstoryCampaignBehavior` and `InitialChildGenerationCampaignBehavior` are removed. No
election/decision/kingdom behaviors touched. *Side note:* removing initial child generation slightly
thins the heir pool that feeds `GenerateInheritanceClaimsUseCase` — an interaction, not a clash.

#### L2 — Save/load ordering — mostly defended, residual UNVERIFIED

**Evidence:** `SubModule.cs:93-134`; `FeudalElectionSaveDefiner.cs`; `doc/feudal-titles.md:54-64`

Decision *deserialisation* needs only the type definer (reflection-discovered, independent of
`InitializeGameStarter`). Decision *methods* run from campaign ticks, after `InitializeGameStarter`
and `OnGameLoaded`; every consumer null-checks/falls back via `FeudalServices.IsInitialised`. Exact
engine ordering is **UNVERIFIED**, but worst case is "vanilla behaviour for one frame", not a crash.
The genuinely unguarded gap is C2 (stale registry *content*, not null services).

#### L3 — `OnHeroKilled` succession-order assumption — UNVERIFIED

**Evidence:** `FeudalTitleCampaignBehavior.cs:112` (`victim.Clan.Leader != victim` → return)

If vanilla `KillCharacterAction` applies clan-leader succession *before* dispatching
`HeroKilledEvent`, this check always fails for leaders and inheritance claims never generate.
**Verify in-game;** if broken, pair `CampaignEvents.OnClanLeaderChangedEvent` with the death event.

#### L4 — Levy registry growth / `Answered` semantics

**Evidence:** `IssueLevyUseCase.cs:27-34` (dedups only `Called`); `LevyCampaignBehavior.cs:95-102`
(`OnArmyDispersed` is the only cleanup)

`Answered`/`Refused` calls accumulate until the issuing clan disperses an army. Clans whose armies
are destroyed rather than dispersed (does `ArmyDispersed` fire on defeat? **UNVERIFIED**) leak
entries into `DadgLevyCalls` save data. Cheap fix: purge non-`Called` calls older than *N* days in
the expiry pass.

### Refuted suspicions

- Decision-swap infinite loop (guard terminates after one re-entry) — **M1**
- Influence double-charge on swap — **M1**
- `CampaignBehaviourDisabler` disabling needed behaviors — **L1**

### Items to verify in-game

| # | What to confirm | If it fails |
|---|---|---|
| M5 | Petition Gauntlet screen renders for a player-suzerain | Use vanilla `ImageIdentifier` |
| L3 | `HeroKilledEvent` fires before leader succession | Pair with `OnClanLeaderChangedEvent` |
| M1 | `RemoveDecision` refund behaviour | Adjust swap if double-charge observed |
| L4 | `ArmyDispersed` fires on army defeat/destruction | Add age-based purge |

---

## 3. Part B — Intra-Faction Private Wars

> **⚠ SUPERSEDED (2026-06-13).** The recommendation below — design **(b)**, *abstract private war*
> — is overruled by a later requirement that **all wars be real and represented on the campaign
> map**. The working design refines dismissed option **(a)** by splintering *both* belligerents into
> their own one-clan kingdoms (not just one against the parent), which removes (a)'s
> "escalates to the whole kingdom" objection. See **`feudal-private-wars-design.md`**. Part B is
> retained for its `MapFaction` analysis and the options trade study, which remain accurate.

> **Goal:** two Count clans, both de jure vassals of the same Duke, fighting each other while **both
> remain members of the same kingdom** — CK3-style limited private war, with the rest of the kingdom
> uninvolved.

### The core obstacle: `MapFaction`

**Bottom line:** two clans in the same kingdom can **never** be map-hostile in vanilla Bannerlord.
Real on-map battles between them are not achievable without Harmony.

Bannerlord resolves hostility through `MapFaction`. For a clan inside a kingdom, `MapFaction` is the
**kingdom**. Party AI targeting, encounter menus, and `MapEvent` side assignment all evaluate
hostility at the MapFaction level — so two clans sharing a kingdom share a MapFaction and are never
mutually hostile. This is *why* vanilla rebels and minor factions are always **kingdomless** clans,
and why every known internal-war mod splits clans out of the kingdom rather than fighting within it.

### Verified facts

Verified against `Bannerlord.ReferenceAssemblies.Core 1.3.15.110062` (decompiled locally) and the
**Diplomacy mod source** (read directly). Method *bodies* are stripped in the reference assemblies,
so behavioral claims about internals are flagged **UNVERIFIED** but rest on consistent prior
decompiles.

**a. Map hostility goes through MapFaction — CONFIRMED (mechanism partially UNVERIFIED).**
`IFaction` exposes `MapFaction`, `IsAtWarWith`, `GetStanceWith`; `FactionManager` exposes
`IsAtWarAgainstFaction(IFaction, IFaction)` (the 1.3.x name of the older `IsAtWarOverFaction`) and
keys an internal stance dictionary via `GetStanceLinkInternal`. UNVERIFIED at IL level but
consistent with all known decompiles: `Clan.MapFaction` returns `Kingdom ?? this`, and
`IsAtWarAgainstFaction` resolves both sides to `MapFaction` before consulting the stance, returning
false when both resolve to the same faction.

**b. Clan-vs-clan `DeclareWarAction` — ineffective.** The `Apply*` overloads take `IFaction`, so a
`Clan` compiles, and a clan-pair `StanceLink` is stored — but every consumer reads stances through
MapFaction, so the stance is **dead data** while both clans are kingdom members. Worse, `ApplyInternal`
fires `OnWarDeclared` events that vanilla behaviors interpret as kingdom-level wars. **Not usable.**

**c. How rebels/minor factions get hostility.** They are *independent* clans. A settlement
rebellion (`RebellionsCampaignBehavior`) spawns a clan with `Clan.IsRebelClan == true` and **no
Kingdom**, so its MapFaction is itself; `DeclareWarAction.ApplyByRebellion` then creates a normal
faction-vs-faction stance. Minor factions likewise have `IsMinorFaction == true`, no kingdom, and
hold ordinary stances. Vanilla never breaks the MapFaction rule — it routes around it by making the
hostile party a top-level faction.

**d. Prior art — Diplomacy mod (VERIFIED from source).**
`CivilWar/Actions/StartRebellionAction.cs`: destroys competing rebel factions, then
`Campaign.Current.KingdomManager.CreateKingdom(...)` (note: there is **no** `CreateKingdomAction` in
vanilla — the official path is `KingdomManager.CreateKingdom`), moves member clans with
`ChangeKingdomAction.ApplyByJoinToKingdom(clan, rebelKingdom, showNotification: false)` (manually
preserving each clan's influence), then `DeclareWarAction.ApplyByKingdomCreation(rebelKingdom,
parentKingdom)`. War end (`ConsolidateKingdomsAction.cs`): moves clans back, calls
`DestroyKingdomAction.Apply(rebelKingdom)`, and restores fiefs via
`ChangeOwnerOfSettlementAction.ApplyByDefault`. The civil-war *core* uses only official actions
(Diplomacy uses Harmony elsewhere — UI, `PatchTools/` — but **not** for this flow).

### Design options evaluated

#### (a) Temporary splinter kingdom

*Feasible, proven by Diplomacy.* APIs verified: `KingdomManager.CreateKingdom`, `ChangeKingdomAction`,
`DeclareWarAction.ApplyByKingdomCreation`, `DestroyKingdomAction`, `ChangeOwnerOfSettlementAction`.

- **What's lost:** the requirement "both remain kingdom members" is violated *literally and
  visibly* — the warring clan shows a new banner/kingdom in the encyclopedia, leaves the parent
  kingdom's armies/decisions, and the parent's *other* clans become hostile to it too (a private
  Count-vs-Count quarrel escalates to Count-vs-whole-kingdom unless you immediately broker peace
  with everyone else — which vanilla war/peace AI will fight against daily).
- **AI risk:** **high** — noisiest option, furthest from "limited private war".
- **Save safety:** good *if* the merge-back always runs, but a mid-war save permanently contains an
  extra kingdom; edge cases (sponsor clan eliminated mid-war, player captured) need handling.

#### (b) Abstract private war (CK3-style war score) — RECOMMENDED

*Fully feasible, zero vanilla interference.* Daily tick compares `Clan.TotalStrength`/party counts,
rolls simulated skirmishes/raids, accumulates a war score; outcomes applied with verified official
APIs: `ChangeOwnerOfSettlementAction.ApplyByKingDecision` (or `ApplyByDefault`) for the contested
seat, `GiveGoldAction`, `ChangeRelationAction`, troop attrition via `MobileParty.MemberRoster`.

- **What's lost:** no visible field battles; the player experiences the war through notifications,
  gold/relation/loyalty effects, garrison and roster attrition, and the final title transfer. The
  player, when a belligerent, can still act via event inquiries (commit gold/troops to swing the
  score) — the same pattern the petition decisions already use.
- **AI risk:** **none** — vanilla never sees a war.
- **Save safety:** **best of the three** — state persists like existing tension/claims
  (`SyncData` pipe-delimited strings, no new `SaveableTypeDefiner` ids); mod removable mid-campaign,
  degrades silently.

#### (c) Hybrid with real forced battles — NOT FEASIBLE

`StartBattleAction.Apply(PartyBase, PartyBase)` and `EncounterManager.StartPartyEncounter(...)` are
public and contain no war-stance parameter, so a call compiles — but (UNVERIFIED at IL level, high
confidence) the resulting `MapEvent`/encounter machinery resolves sides via MapFaction hostility;
non-hostile same-faction parties produce undefined behaviour (wrong menus, asserts, or instant
termination), and AI parties never *seek* such battles. Diplomacy does not attempt this even with
Harmony available. **The hybrid collapses into (b).**

### Recommended design: Abstract private war

Use **design (b)**, with the existing petition pipeline as the trigger and the shared Duke as
mediator. Optionally ship **(a)** later as a separate *escalation* tier ("the feud boils over into
open rebellion") rather than as the private-war mechanism itself.

This satisfies the original requirement more faithfully than (a): both clans **literally stay in the
kingdom** the entire time.

### Domain & integration sketch

Following the hexagonal layout in `doc/feudal-titles.md`:

**Domain** (`src/DellarteDellaGuerra.Domain/PrivateWars/`):

```csharp
public enum PrivateWarStatus { Active, Mediated, AttackerVictory, DefenderVictory, WhitePeace }

// WarScore in [-100, +100], attacker-positive.
public record PrivateWar(
    string Id,
    string AttackerClanId,
    string DefenderClanId,
    string ContestedTitleId,
    float  WarScore,
    int    StartDay,
    PrivateWarStatus Status);
```

- **Port** `IPrivateWarRepository` (get active war for clan, add, update, remove, `Initialise`,
  `Snapshot`) — implemented by `InMemoryPrivateWarRegistry` in Infrastructure, like the existing
  registries.
- **Use cases** (all unit-testable, no TaleWorlds dependency):
  - `DeclarePrivateWarUseCase` — preconditions: denied petition + tension ≥ war threshold, same
    kingdom, neither clan already at private war.
  - `TickPrivateWarUseCase` — pure function of both clans' strengths + RNG seed → score delta +
    `SkirmishOutcome` (casualties/gold/relation deltas for the adapter to apply).
  - `ResolvePrivateWarUseCase` — score ≥ +100 → transfer title & fulfil claim; ≤ −100 → extinguish
    claim; either way reset tension via the existing `ITensionRepository`.
  - `MediatePrivateWarUseCase` — suzerain-imposed outcome.

**Game API** (`src/DellarteDellaGuerra/Titles/Api/Campaign/`):

- `PrivateWarCampaignBehavior : CampaignBehaviorBase` — `DailyTickClanEvent` drives
  `TickPrivateWarUseCase` and applies outcomes through official actions only
  (`ChangeRelationAction`, `GiveGoldAction`, roster attrition, and on resolution
  `ChangeOwnerOfSettlementAction.ApplyByKingDecision(newHolderLeader, seatSettlement)` — which
  re-fires the existing `OnSettlementOwnerChanged` → `AssignTitleUseCase`, keeping title state
  consistent for free). `SyncData` persists wars under a new key `DadgPrivateWars` via a
  `PrivateWarStateSerialiser`, extending `IFeudalStateStore`.

**Trigger wiring:** in `FeudalPetitionDecision.cs:126` the `DenyClaimOutcome` branch already calls
`SurgeTensionOnDenial()`. After the surge, if tension ≥ a war threshold (e.g. 25), invoke
`DeclarePrivateWarUseCase` via `FeudalServices` (save-deserialised decisions use the static locator +
null-check convention). `InternalConflictCampaignBehavior` is unchanged except for skipping petition
triggers for clans already at private war.

**Suzerain mediation:** a new `FeudalMediationDecision : KingdomDecision`, registered in
`FeudalElectionSaveDefiner` with the **next** type ids (do not renumber existing ones), proposed when
a private war has run ≥ *N* days or the score stalls. Chooser is the shared de jure Duke (reuse
`IFeudalStructure.GetDeJureSuzerainTitleId`); outcomes impose attacker victory / defender victory /
white peace, each routed through `MediatePrivateWarUseCase`. This gives a player-Duke a real lever —
the best UX moment in the feature.

**UNVERIFIED items to smoke-test before committing to the design:**
1. `ChangeOwnerOfSettlementAction.ApplyByKingDecision` between two same-kingdom clans outside an
   election context behaves cleanly (expected — vanilla uses it for exactly that).
2. Clan-pair `StanceLink`s are truly inert (only matters if you ever pursue design (a)).
3. `IsAtWarAgainstFaction` MapFaction resolution, if you want a sanity-check assertion.

---

## 4. Part C — Recommended Order of Work

1. **Fix C1 (levy penalties)** — scope the expiry use case to the issuing clan. Small, mechanical,
   high impact.
2. **Fix C2 (state contamination)** — add `IFeudalStateStore.Reset()` + `FeudalServices.Reset()`;
   call on new game / load / game end.
3. **Tame C3 + M2 (balance)** — tension cap + daily decay, claim downgrade/expiry, diplomacy bonus
   clamp, and a post-denial petition cooldown.
4. **Polish M1 / M3 / M4** — restore `capturerHero` in the swap, exempt vassals who can't comply
   from levy penalties (and notify the player), vacate titles on `OnClanDestroyedEvent`.
5. **Build the private war** (Part B, design (b)) on top of the now-stable tension pipeline, with the
   Duke-mediation decision as the player-facing centrepiece.
6. **In-game verification pass** for the UNVERIFIED items (M5 petition UI, L3 succession ordering,
   L4 army dispersal on defeat).

Each step is independently committable and consistent with the zero-Harmony constraint.

---

## 5. Appendix — Verification Sources

| Source | How used | Confidence |
|---|---|---|
| `Bannerlord.ReferenceAssemblies.Core 1.3.15.110062` (decompiled locally with `ilspycmd`) | Exact API signatures for the version this repo targets; method bodies stripped to `throw null` | Signatures **verified**; behaviour **UNVERIFIED** |
| Diplomacy mod source (`DiplomacyTeam/Bannerlord.Diplomacy@dev`, read directly) | Civil-war flow (splinter kingdom, merge-back) | **Verified** |
| This repository's source | All Part A findings | **Verified** (file:line cited) |
| Training knowledge of prior Bannerlord decompiles | MapFaction hostility resolution mechanics | **UNVERIFIED** but consistent across all known decompiles |

> **Caveat:** behavioral claims about TaleWorlds internals (stance resolution, decision refund,
> event ordering, encounter side-assignment) could not be confirmed against method bodies and are
> flagged **UNVERIFIED** throughout. They should be smoke-tested in-game before relying on them.
