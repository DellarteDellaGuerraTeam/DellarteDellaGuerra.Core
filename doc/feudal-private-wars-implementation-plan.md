# Private Wars — Implementation Plan (stay-in-kingdom substrate)

> **Status:** Implementation plan. Turns `feudal-private-wars-design.md` (the locked
> **stay-in-kingdom** substrate) into a phased, codebase-grounded build order against the DADG
> hexagonal layout. Companion to `feudal-private-wars-design.md` (mechanism + patch surface),
> `feudal-private-wars-tempkingdom-cost.md` (why the substrate was chosen),
> `feudal-title-transfer-design.md` (the de jure/de facto split this reuses) and `feudal-titles.md`
> (architecture).
> **Branch:** `claude/banner-lord-mcp-integration-79WN8` · **Target engine: 1.3.1** (matches
> `src/supported-game-versions.txt` = v1.3.15). 1.4.6 deltas are noted inline but **not** built for.

---

## Table of Contents

1. [Success criteria](#1-success-criteria)
2. [Why this phase order](#2-why-this-phase-order)
3. [Architecture mapping](#3-architecture-mapping)
4. [Phase 0 — Domain (pure, no engine)](#4-phase-0--domain-pure-no-engine)
5. [Phase 1 — Registry, persistence, locator wiring](#5-phase-1--registry-persistence-locator-wiring)
6. [Phase 2 — Make a same-kingdom war RESOLVE](#6-phase-2--make-a-same-kingdom-war-resolve)
7. [Phase 3 — Score, revert/prize, state-leak patches, triggers](#7-phase-3--score-revertprize-state-leak-patches-triggers)
8. [Phase 4 — Player path](#8-phase-4--player-path)
9. [Cross-phase: the auto-end hazards](#9-cross-phase-the-auto-end-hazards)
10. [Risks carried into implementation](#10-risks-carried-into-implementation)

---

## 1. Success criteria

The verifiable goals we loop against (per `AGENTS.md` §4):

1. **AI-vs-AI:** two same-kingdom clans fight a real, on-map siege that *resolves* (does not camp
   forever), prisoners *stay held*, and one side wins at `|S| ≥ 100`; the prize transfers and all other
   captures revert — verified in a GABS smoke test.
2. **Membership untouched:** during the war both clans still vote, draw tribute, and fight K's external
   wars; **no faction object is ever created or destroyed** (§12 of the design doc).
3. **Player-path:** player clan can press a claim by force against a fellow vassal with correct
   siege/battle UI and a bespoke "end the feud" exit.
4. Pure domain logic is unit-tested in isolation **before** any engine wiring.

---

## 2. Why this phase order

The order is a **risk gradient**, not a layering convenience:

- **Phase 2 contains the single highest-risk unknown** — design §15 risk #1: does boosting
  `GetTargetScoreForFaction` actually route stock siege AI to a *same-faction* target, or does the
  MapFaction short-circuit reassert downstream? Everything in phases 3–4 is wasted if the substrate
  can't drive a war. **We prove the substrate resolves a war before building on it.**
- **Phase 0 is pure and zero-risk** — it is the decision core every later phase asserts against, and it
  is fully unit-testable with fakes, so it ships first and de-risks the domain shape.
- **Phase 4 (player path) is a clean boundary** — dormant unless the player's clan is a belligerent, so
  the AI-vs-AI war is fully functional without any of it (design §4.5 point 4).

---

## 3. Architecture mapping

Mirror the **Levy** feature (cleanest recent template): Domain use cases + ports → Infrastructure
in-memory registry → an `Api` campaign behavior + `Spi` serialiser, DI-registered in
`DadgServiceContainer`, reached from models/patches through the `FeudalServices` static locator.

```
Domain/PrivateWars/                   (pure, netstandard2.0, zero TaleWorlds)
                                       (folder/namespace is plural — the `PrivateWar`
                                        record type can't share a namespace leaf, CS0118)
  Model/  PrivateWar, PrivateWarStatus, PrivateWarOutcome, WarSide,
          PrivateWarObservations, SettlementInfo, BattleOutcome, PrizeAward,
          RevertInstruction, ResolutionPlan
  ICasusBelli, ClaimCasusBelli
  WarSideResolver                     ← side membership by suzerain walk (call-to-arms)
  MainGoalSelector                    ← highest-prosperity de jure held by defendant (frozen goal)
  PrivateWarScoreCalculator           ← the testable heart (amended §6/§8)
  DeclarePrivateWarUseCase, TickPrivateWarUseCase, ApplyBattleOutcomeUseCase, ResolvePrivateWarUseCase
  Port/  IPrivateWarRepository, IPrivateWarHostility, IFeudalHierarchy

Infrastructure/PrivateWar/
  InMemoryPrivateWarRegistry          (implements both ports; cached side-sets, O(1) AreEnemies)

DellarteDellaGuerra/PrivateWar/
  Api/Campaign/  PrivateWarCampaignBehavior   (triggers, daily tick, event-sourced score, registry maintenance)
  Api/GameModels/  Dadg* model overrides (design §4.1)
  Api/Patches/     Harmony patches (design §4.2/§4.3) — documented zero-Harmony exception
  Spi/  PrivateWarStateSerialiser     (pipe-delimited, mirrors TitleStateSerialiser)

Integration/PrivateWar/  + FeudalServices/SubModule/DadgServiceContainer edits
```

**Locator extension.** `FeudalServices` (`DellarteDellaGuerra/Titles/Api/FeudalServices.cs`) gains two
properties — `PrivateWars` (`IPrivateWarRepository`) and `PrivateWarHostility`
(`IPrivateWarHostility`) — threaded through `Initialise(...)`/`Reset()`. **Every §4 model and patch
null-guards `FeudalServices.IsInitialised` and falls through to stock**, exactly like
`DadgDiplomacyModel` does today.

**Documented convention exception.** All Harmony in `PrivateWar/Api/Patches/` is a deliberate exception
to the zero-Harmony-in-feudal-core convention, justified by design §2 (the central hostility signal
`DefaultMobilePartyAIModel.IsEnemy` is a private helper with no model seam).

**Sides, not pairs (call-to-arms amendment).** A private war has two **principals** (claimant ⚔
title-holder) but two **sides**: each principal plus its entire (sub)vassal subtree, following the
**DADG feudal title hierarchy** (not the Bannerlord kingdom). Side membership is **dynamic**: a clan's
side is resolved by walking **up** its suzerain chain and taking the **first belligerent principal it
hits** (nearest-belligerent-ancestor — this puts the attacker's subtree on the attacker's side even
when the attacker is itself the defender's vassal). `IPrivateWarHostility.AreEnemies(X, Y)` is therefore
"X and Y resolve to opposite sides of some active war." The §4 patch count is unchanged (they all
funnel through `AreEnemies`); only the registry semantics change from unordered-pair to side-set. The
`InMemoryPrivateWarRegistry` caches resolved side-sets and invalidates on feudal-hierarchy change to
keep `AreEnemies` on the O(1) hot path (design §15 risk #4). Design §9's "one war per unordered pair"
relaxes to **one war per pair of principals (per casus belli)**; a vassal may be a called participant
in several wars at once.

---

## 4. Phase 0 — Domain (pure, no engine)

> **Verify:** `dotnet test` — new unit tests pass; zero TaleWorlds references in `Domain/PrivateWars/`.

Build and unit-test the entire decision core with **fakes**, mirroring `Domain.Tests/Levy/`
(`IssueLevyUseCaseTests.cs` + `Fakes.cs`). The domain stays pure — it **receives** engine-computed
inputs (current owners, settlement types/prosperity, suzerain links) and **returns** decisions/plans.

**Record (`PrivateWar`)** — flat record like `LevyCall`:
`Id, AttackerPrincipalClanId, DefenderPrincipalClanId, CasusBelliType, TitleId,
MainGoalSettlementId (frozen), OriginalFiefOwners (settlementId→ownerClanId snapshot), BattleScore,
Score, StartDay, Status`. **Side rosters are NOT stored** — resolved dynamically from the hierarchy.
**`BattleScore`** is the one *accumulated* score term (attacker-positive, capped ±`BATTLE_SCORE_CAP`);
every other term is recomputed from current state each tick. No temp-kingdom/refcount/WarFooting state
(design §11).

**Side resolution (`WarSideResolver`)** — pure:
`ResolveSide(clanId, war, Func<string,string?> getSuzerain) → WarSide? {Attacker|Defender|null}`.
Walks up the suzerain chain; first principal hit wins; null = uninvolved. `IFeudalHierarchy` is the
port supplying `GetSuzerain(clanId)` (the registry will back it with the existing feudal structure).

**Main goal (`MainGoalSelector`)** — pure, frozen at declaration:
`Select(IEnumerable<SettlementInfo> titleDeJureSettlements, defenderClanId) →
settlementId?`. Highest **prosperity** among the claimed title's de jure settlements **held by the
defendant**; if it holds none, return null → CB cannot be pressed.
`SettlementInfo = (Id, OwnerClanId, IsTown, Prosperity)`.

**Score (`PrivateWarScoreCalculator.Compute(PrivateWarObservations obs, float daysElapsed) → float`)** —
attacker-positive, clamped ±100 (amended §6.B). State terms are recomputed each tick; `BattleScore`
arrives pre-accumulated on `obs` (copied from the record by the caller):
```
state    = (obs.AttackerHoldsMainGoal ? 50 : 0)
         + 30*obs.DefenderSideTownsHeldByAttacker   + 10*obs.DefenderSideCastlesHeldByAttacker
         − 30*obs.AttackerSideTownsHeldByDefender   − 10*obs.AttackerSideCastlesHeldByDefender
         +  5*obs.DefenderClanPrisonersHeldByAttackerSide
         −  5*obs.AttackerClanPrisonersHeldByDefenderSide
fatigue  = FATIGUE_RATE_PER_DAY * daysElapsed * (obs.AttackerHoldsMainGoal ? +1 : −1)
S        = Clamp(state + obs.AccumulatedBattleScore + fatigue, −100, +100)
```
Settlement counts span the **whole side** (principal + sub-vassals), main goal excluded from the
town/castle tallies (its own flat 50). **Prisoners** score ±5 each only for captives of the *opposing
principal's clan* (state, not an event tally — released captives stop counting). **Village raids do not
score.** Weights/`FATIGUE_RATE_PER_DAY`/`BATTLE_WEIGHT`/`BATTLE_SCORE_CAP` are named constants.

**Battle accumulation (`ApplyBattleOutcomeUseCase.Execute(war, BattleOutcome) → PrivateWar`)** — pure;
the *only* mutator of `BattleScore`. `BattleOutcome = (WarSide winner, float enemyForceDefeated, float
losingSideTotalStrength)`. Computes
`delta = BATTLE_WEIGHT * Clamp(enemyForceDefeated/losingSideTotalStrength, 0, 1)`, signed +attacker
/ −defender, and returns `war with { BattleScore = Clamp(war.BattleScore + signedDelta, −CAP, +CAP) }`.
Guards `losingSideTotalStrength <= 0` (no-op). Phase 3 wires it to `MapEvent` end + `repository.Update`.

**Revert planner (`ResolvePrivateWarUseCase`)** — pure (design §8): snapshot + current owners + outcome
→ `IReadOnlyList<RevertInstruction(settlementId, revertToClanId)>` + `PrizeAward?` + claim-change. Revert
only fiefs that moved **between this war's two sides**; skip third-party-owned; on attacker victory keep
the goal with the attacker. Eliminated-owner skips are applied at integration (engine state).

**Use cases:**
- `DeclarePrivateWarUseCase.Execute(attackerPrincipal, defenderPrincipal, casusBelli, mainGoalId,
  fiefSnapshot, startDay) → PrivateWar?` — rejects if `mainGoalId == null` (landless defendant) or a
  war already exists for this principal pair / same CB (amended §9).
- `TickPrivateWarUseCase.Execute(war, observations, currentDay) → (newScore, PrivateWarOutcome?)` —
  `AttackerVictory` at `S≥+100`, `DefenderVictory` at `S≤−100`, else none.
- `ApplyBattleOutcomeUseCase.Execute(war, battleOutcome) → PrivateWar` — accumulates `BattleScore`
  (above); called on each resolved between-sides battle, independent of the daily tick.
- `ResolvePrivateWarUseCase.Execute(war, outcome, currentOwners) → ResolutionPlan`.

**Ports:** `IPrivateWarRepository` (Get, GetByPrincipalPair, GetByDefender, GetByTitle, GetByClan,
GetAll, Add, Update, Remove, Initialise, Snapshot — the GetByDefender/GetByTitle queries back the
multi-attacker re-eval and same-CB collision in later phases), `IPrivateWarHostility` (`AreEnemies`),
`IFeudalHierarchy` (`GetSuzerain`).

**Test matrix:**

| Test | Asserts |
|---|---|
| Score: attacker holds main goal, time passes | fatigue → **+100** (termination) |
| Score: defender holds main goal | fatigue → **−100** |
| Score: settlement weights | town 30 / castle 10 / main goal 50; defender pushback subtracts; clamp ±100 |
| Score: prisoner term | +5 per opposing-principal-clan captive held; defender-held attacker-clan captive subtracts; non-principal-clan captive ignored |
| Battle: accumulation & sign | attacker win adds `+W·(defeated/totalLosing)`; defender win subtracts; clamps to ±CAP; `totalLosing≤0` no-ops |
| SideResolver: unrelated subtrees | each clan resolves to its principal's side |
| SideResolver: attacker is defender's vassal | attacker subtree → Attacker; rest → Defender; uninvolved → null |
| MainGoalSelector | highest-prosperity de jure held by defendant; **holds none → null** |
| Declare: landless defendant | rejected |
| Declare: duplicate principal pair / same CB | rejected; different opponent allowed |
| Revert: white peace | full status-quo-ante |
| Revert: attacker victory | goal stays with attacker, rest reverts |
| Revert: shared-goal A⚔C / B⚔C third-party fief | war-1 skips it (design §9) |

---

## 5. Phase 1 — Registry, persistence, locator wiring

> **Verify:** build green; declare a war via a temporary debug hook, save, reload — the pair and its
> score survive the round-trip.

- `InMemoryPrivateWarRegistry` implementing both ports. `AreEnemies(a,b)` is the **hot path** (every §4
  patch calls it per hostility check) → O(1), keyed by **unordered** clan-id pair, with a separate
  attacker/defender role lookup. Mirror `InMemoryLevyRegistry`.
- `PrivateWarStateSerialiser` — pipe-delimited, copy `TitleStateSerialiser` shape;
  `OriginalFiefOwners` serialises as a sub-delimited map.
- Extend `IFeudalStateStore` + `FeudalStateStoreAdapter` with
  `InitialisePrivateWars`/`SnapshotPrivateWars`.
- `PrivateWarCampaignBehavior.SyncData` → `dataStore.SyncData("DadgPrivateWars", ref _serialised)`.
  **No new `SaveableTypeDefiner` ids** for the record (strings only). Any new
  `KingdomDecision`/`Outcome` subclasses for the player flow take the **next free
  `FeudalElectionSaveDefiner` ids 8, 9, 10…** (1–7 are used: petition/claimant/grant/deny/attainder/
  attaint/uphold; base id `2_887_350`).
- DI: add `RegisterPrivateWarServices()` to `DadgServiceContainer` (mirror `RegisterLevyServices`);
  register `PrivateWarCampaignBehavior`. Add the two `FeudalServices` properties + thread them through
  `Initialise`/`Reset` at the `SubModule` call site.

---

## 6. Phase 2 — Make a same-kingdom war RESOLVE

> **Verify (GABS smoke test, AI-vs-AI):** declare a war between two same-K AI clans; confirm (1) the
> attacker AI besieges the goal, (2) the siege transitions to assault and resolves, (3) uninvolved K
> lords don't gather to relieve, (4) A's own army defends A's besieged fief. These are design §15 risks
> #1–#3. **If risk #1 fails, stop and switch to explicit army formation before phase 3.**

**Model overrides (design §4.1)** — register via `AddModel` in `SubModule.InitializeGameStarter`
alongside the existing `DadgDiplomacyModel`:

- `DadgTargetScoreCalculatingModel.GetTargetScoreForFaction` — **build first.** The force multiplier:
  drives attacker intent toward the goal *and* damps kingdom-relief (design §4.1, §5).
- `DadgEncounterModel` — `GetDefenderPartiesOfSettlement`/`GetNextDefenderPartyOfSettlement` so A's
  field army (not just the static garrison) defends A's fief.
- `DadgArmyManagementCalculationModel.GetMobilePartiesToCallToArmy` — keep C out of A's army.

**Core hostility Harmony patches (design §4.2)** in `PrivateWar/Api/Patches/`, each a thin
prefix/postfix consulting `FeudalServices.PrivateWarHostility.AreEnemies(party.ActualClan, …)` and
otherwise no-op:

- `DefaultMobilePartyAIModel.IsEnemy` + `CalculateStanceScore` (private helpers — the central signal,
  Harmony-only).
- `EncounterManager.StartPartyEncounter` / `HandleEncounterForMobileParty` (un-merge → battle).
- `EncounterManager.StartSettlementEncounter` (siege actually assaults — load-bearing for "real sieges").
- `MapEvent.CanPartyJoinBattle` (reinforcements pick a side).
- Feud-army disband guard (design §4.2 / §14): the cohesion/disband path reads `FactionsAtWarWith`,
  blind to the registry war → an army raised for the feud auto-disbands. Patch to keep it alive while
  the pair is active.

**DADG drive loop** in `PrivateWarCampaignBehavior`: daily tick directs the attacker toward the goal via
the `GetTargetScoreForFaction` boost; if stock siege AI doesn't bite (risk #1), fall back to explicitly
forming an army with the goal as siege target.

---

## 7. Phase 3 — Score, revert/prize, state-leak patches, triggers

> **Verify:** a full AI war runs to a scored conclusion; on attacker win the prize transfers and every
> other capture reverts; on white peace everything reverts (status quo ante); prisoners stay held across
> a save/load.

- **State-derived score** in `PrivateWarCampaignBehavior`: on each tick build `PrivateWarObservations`
  from **current world state** — settlement ownership of the contested set vs the snapshot, and current
  **captivity** (count captives of each opposing principal's clan held by the other side) — for both
  sides resolved via `WarSideResolver` (no `StanceLink` exists for a same-K pair — design §5). Copy the
  record's `BattleScore` onto `obs.AccumulatedBattleScore` and feed into `PrivateWarScoreCalculator`.
  Settlement/prisoner terms recompute (driven by `OnSettlementOwnerChanged` / captivity change); village
  raids do not score (amended §6.B).
- **Battle score** (the one accumulated term): on `MapEvent` end, if the two sides reduce to this war's
  Attacker-side parties vs Defender-side parties (via `WarSideResolver`), build a `BattleOutcome`
  (winner, enemy strength defeated, losing side's *total* strength) and call
  `ApplyBattleOutcomeUseCase` → `repository.Update`. Cross-belligerent clashes (A-vs-B while both fight
  C) resolve to the same side / `null` and are skipped — they score in no war (design §9).
- **Call-to-arms wiring:** back `IFeudalHierarchy.GetSuzerain` with the existing feudal structure;
  invalidate the registry's cached side-sets on feudal-hierarchy change so `AreEnemies` stays O(1)
  (amended §A). The §4 patches then cover every clan on both sides automatically (they all funnel
  through `AreEnemies`).
- **Multi-attacker / main-goal re-eval:** the frozen main goal is re-evaluated only when a neutral holds
  it and peaces out; same-CB collision converts attacker-1 to war attacker-2 (amended §E). Backed by the
  `IPrivateWarRepository.GetByDefender`/`GetByTitle` queries from Phase 0.
- **State-leak patches (design §4.2):** `PrisonerReleaseCampaignBehavior.ReleasePartyPrisoners` (AI
  auto-end #1 — see §9); enable `PrisonerCaptureCampaignBehavior` for the pair;
  `RecruitmentCampaignBehavior.OnBeforeSettlementEntered` entry gate;
  `AiBehaviorCampaignBehavior.IsSettlementSuitableForVisitingCondition`;
  `SettlementHelper.FindNearest*` retreat-target exclusion;
  `ChangeOwnerOfSettlementAction.ApplyInternal` re-besiege; `CrimeRatingChangeAction` phantom-war guard.
- **Registry maintenance:** `OnClanChangedKingdom` hook — the pair **persists** by default (the feud is
  clan-vs-clan); fold/resolve only if the move makes it cross-kingdom into a formal war (design §14). A
  behavior callback, not an engine patch.
- **Resolve wiring:** prize via the **already-implemented**
  `AssignTitleUseCase.Execute(seatId, attackerId, SeatTransferKind.Conquest, day)` + dignity finalize
  through the attainder path; revert via `ChangeOwnerOfSettlementAction.ApplyByDefault`. The de jure/de
  facto contested-title marking already happens on capture in `FeudalTitleCampaignBehavior` — Private
  Wars is just its real-war driver (design §7).
- **AI trigger:** hook the existing `FeudalPetitionDecision.DenyClaimOutcome` → `SurgeTensionOnDenial`
  path; a strong-claim clan over a tension threshold (with cooldown + not-already-at-private-war-with-
  this-clan check) escalates to a private war.

---

## 8. Phase 4 — Player path

> **Verify (GABS, player is a belligerent):** "Press your claim by force" is available against a fellow
> vassal; the besiege button shows; the player is on the correct battle side; the "end the feud" exit
> resolves the war.

Dormant unless the player's clan is A or C. Player-only encounter/menu layer (design §4.3):
`PlayerEncounter.SetupFields`/`DoMeetingInternal`, the besiege/sally/break-out menu conditions,
village-raid redirect; plus the player-menu model overrides (`GetEncounterMenu`, `SettlementAccessModel`,
`FindNonAttachedNpcParties…`). **Player auto-end #2:**
`PlayerCaptivityCampaignBehavior.CheckCaptivityChange` (see §9). The bespoke **"Press your claim by
force"** trigger menu and **"end the feud"** conversation (the engine's barter-peace path can't represent
a private war, so the negotiated-exit UI is ours, not a patch). Cosmetic guards (design §4.4) —
nameplate/target tint, encyclopedia enemy list — last, optionally as UIExtenderEx mixins.

---

## 9. Cross-phase: the auto-end hazards

Two **independent** mechanisms silently *terminate* the war if unpatched, because both read "no longer
at war" off the MapFaction wall and free captives (design §4.5 point 3). They split across phases by side:

- **AI — `PrisonerReleaseCampaignBehavior.ReleasePartyPrisoners`** (phase 3). Frees prisoners whose
  faction is `!IsAtWarWith` the captor on any peace/owner-change/load. Highest-severity correctness
  patch after the core hostility signal.
- **Player — `PlayerCaptivityCampaignBehavior.CheckCaptivityChange`** (phase 4). Releases the captured
  player the first tick after capture (same-K → "no more enemies").

Neither degrades the war quietly — they end it. Flagged here so they aren't lost inside their phases.

---

## 10. Risks carried into implementation

1. **Design §15 risk #1 (siege-seeking under DADG drive)** — the project's pivot point; a **phase-2
   gate**, not a late surprise. Test before building phase 3.
2. **Two auto-end mechanisms** (§9) — must both be patched or any prisoner taken evaporates.
3. **Patch hot-path cost** — `IsEnemy`/`CalculateStanceScore` and the per-tick model overrides run
   constantly; the `AreEnemies` lookup must be genuinely O(1) and no-op cheaply when no war is active
   (design §15 risk #4).
4. **Targeting 1.3.1.** Code against 1.3.1 seams. 1.4.6 deltas (design §17) are out of scope for this
   build but recorded there for a future port (~4 members to re-point, one fewer Harmony).
