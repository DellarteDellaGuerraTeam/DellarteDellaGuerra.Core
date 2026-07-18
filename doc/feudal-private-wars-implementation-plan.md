# Private Wars — Implementation Plan (stay-in-kingdom substrate)

> **Status:** Implementation plan. Turns `feudal-private-wars-design.md` (the locked
> **stay-in-kingdom** substrate) into a phased, codebase-grounded build order against the DADG
> hexagonal layout. Companion to `feudal-private-wars-design.md` (mechanism + patch surface),
> `feudal-private-wars-tempkingdom-cost.md` (why the substrate was chosen),
> `feudal-title-transfer-design.md` (the de jure/de facto split this reuses) and `feudal-titles.md`
> (architecture).
> **Branch:** `feature/add-internal-faction-wars` · **Target engine: 1.4.6**. The original plan was
> implemented on 1.3.15; §11 is the approved port/hardening amendment.

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
11. [1.4.6 port and hardening amendment](#11-146-port-and-hardening-amendment-2026-07-18)

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
> attacker AI besieges the goal, (2) the siege resolves through the designed AI capture path, (3) uninvolved K
> lords don't gather to relieve, (4) A's own army defends A's besieged fief. These are design §15 risks
> #1–#3. **If risk #1 fails, stop and switch to explicit army formation before phase 3.**

**Model overrides (design §4.1)** — register via `AddModel` in `SubModule.InitializeGameStarter`
alongside the existing `DadgDiplomacyModel`:

- `DadgTargetScoreCalculatingModel.GetTargetScoreForFaction` — **build first.** The force multiplier:
  drives attacker intent toward the goal *and* damps kingdom-relief (design §4.1, §5).
- `DadgEncounterModel` — `GetDefenderPartiesOfSettlement`/`GetNextDefenderPartyOfSettlement` so A's
  field army (not just the static garrison) defends A's fief.
- `DadgArmyManagementCalculationModel.CanLordCreateArmy` (1.4.6) — preserve vanilla eligibility for
  ordinary kingdom armies, filter every opposing private-war-side party from the returned candidates,
  and fail the vanilla formation result if the filtered list no longer satisfies its contract.

**Core hostility Harmony patches (design §4.2)** in `PrivateWar/Api/Patches/`, each a thin
prefix/postfix consulting `FeudalServices.PrivateWarHostility.AreEnemies(party.ActualClan, …)` and
otherwise no-op:

- `DefaultMobilePartyAIModel.IsEnemy` + `CalculateStanceScore` (private helpers — the central signal,
  Harmony-only).
- `EncounterManager.StartPartyEncounter` / `HandleEncounterForMobileParty` (un-merge → battle).
- `EncounterManager.StartSettlementEncounter` (siege actually assaults — load-bearing for "real sieges").
- `MapEvent.CanPartyJoinBattle` (reinforcements pick a side).
- `Kingdom.CreateArmy` (after vanilla winner selection, replace members only for a matching active
  private-war besieger/leader/frozen goal; abort if fresh policy validation fails).
- No feud-specific disband patch on 1.4.6: the old `NoActiveWar` dispersion path is gone. Preserve
  normal cohesion, starvation, and objective-completion dispersal and verify them in runtime tests.

**DADG drive loop** in `PrivateWarCampaignBehavior`: the AI-hourly listener injects the frozen goal as
a scored `BesiegeSettlement` candidate for both sides whenever the opposing side holds it. On 1.4.6,
the candidate sets `AIBehaviorData.WillGatherArmy = true` only when the private-war army policy
authorizes the party. It does **not** call `PartyThinkParams.SetArmyMembers(...)`: Bannerlord's event
listeners run LIFO, later stock score producers can overwrite the single shared list, and the private
listener cannot know the final winner at its execution point.

A narrowly filtered Harmony prefix on public
`Kingdom.CreateArmy(Hero, Settlement, ArmyTypes, MBReadOnlyList<MobileParty>)` runs after vanilla has
selected the strict winner and passed its random formation gate. Only when the type is `Besieger` and
the leader/target match an active private-war side and frozen goal does the prefix revalidate the
policy and replace `partiesToCallToArmy` with the authorized same-side list. If revalidation fails it
suppresses that private creation; ordinary and unrelated calls are unchanged:

- at most one private-war army per side for this goal;
- an eligible principal-clan party has exclusive leader priority;
- if no principal-clan party is eligible, an eligible party from another participating clan may lead;
- candidates are eligible parties whose clans currently resolve to the same war side, never the
  opposing side or an uninvolved same-kingdom clan;
- a fallback-led army is not replaced when a principal party later becomes eligible.

The private-war army candidate still competes in the normal score vote so defence, captivity,
starvation, and stronger objectives can win. Targeted tests must prove losing/tied private candidates
do not reach the filtered creation path, matching winners receive only their current authorized side,
invalidated plans abort, and ordinary `Kingdom.CreateArmy` calls retain their original member list.

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
  `DadgVolunteerModel.MaximumIndexHeroCanRecruitFromHero` plus the
  `RecruitmentCampaignBehavior.OnBeforeSettlementEntered` entry gate;
  renamed 1.4.6 `AiVisitSettlementBehavior.IsSettlementSuitableForVisitingCondition`;
  `ChangeOwnerOfSettlementAction.ApplyInternal` re-besiege. The audited crime action already guards
  same-faction declarations. Retreat/disband and army-camp selection remain an explicit v1 limitation:
  their exact 1.4.6 selectors are private multi-stage methods, and `SettlementHelper` is not patched
  globally because offensive patrol and unrelated gathering callers share it.
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
`PlayerCaptivityCampaignBehavior.CheckCaptivityChange` (see §9), patched only at the
"no more enemies" predicate so ransom and time-based escape continue to run. The bespoke **"Press your
claim by force"** trigger menu and **"end the feud"** conversation (the engine's barter-peace path can't
represent a private war, so the negotiated-exit UI is ours, not a patch). Cosmetic guards (design §4.4)
— nameplate/target tint and encyclopedia enemy list — last, using UIExtenderEx mixins. On 1.4.6 the
settlement relation mapping must explicitly use Neutral=0, SameFaction=1, Enemy=2, Ally=3 rather than
casting the domain enum by ordinal.

---

## 9. Cross-phase: the auto-end hazards

Two **independent** mechanisms silently *terminate* the war if unpatched, because both read "no longer
at war" off the MapFaction wall and free captives (design §4.5 point 3). They split across phases by side:

- **AI — `PrisonerReleaseCampaignBehavior.ReleasePartyPrisoners`** (phase 3). Frees prisoners whose
  faction is `!IsAtWarWith` the captor on any peace/owner-change/load. Highest-severity correctness
  patch after the core hostility signal.
- **Player — `PlayerCaptivityCampaignBehavior.CheckCaptivityChange`** (phase 4). Releases the captured
  player the first tick after capture (same-K → "no more enemies"). The current whole-method prefix is
  invalid because it also prevents ransom/time escape; replace only the faction-war predicate.

Neither degrades the war quietly — they end it. Flagged here so they aren't lost inside their phases.

---

## 10. Risks carried into implementation

1. **Design §15 risk #1 (siege-seeking under DADG drive)** — the project's pivot point; a **phase-2
   gate**, not a late surprise. Test before building phase 3.
2. **Two auto-end mechanisms** (§9) — must both be patched or any prisoner taken evaporates.
3. **Patch hot-path cost** — `IsEnemy`/`CalculateStanceScore` and the per-tick model overrides run
   constantly; the `AreEnemies` lookup must be genuinely O(1) and no-op cheaply when no war is active
   (design §15 risk #4).
4. **1.4.6 siege retention.** `BesiegerCamp.CheckBesiegerPartiesAndMakeThemLeave` can eject an
   unattached party whose selected behavior drifted away from Besiege/Escort/Assault. Verify the
   score-driven objective keeps the private-war army attached and its leader besieging.
5. **1.4.6 synthetic capture.** `ApplyBySiege` now destroys the garrison party. Verify there is no
   double destruction/stale reference and that losing lords become prisoners before the goal can be
   reclaimed.
6. **Shared army-decision cache.** `PartyThinkParams` stores one army-member list for the winning
   behavior. Never overwrite it for a private-war objective that did not win the score vote.

---

## 11. 1.4.6 port and hardening amendment (2026-07-18)

This amendment supersedes 1.3-only member names and turns the compatibility audit into implementation
gates. It does not broaden the feature beyond the design's existing private-war contract.

### 11.1 Integration baseline

- Merge `migrate-to-1.4` into `feature/add-internal-faction-wars`; do not rebase the feature's long
  history.
- Resolve top-level `SubModule.xml` with 1.4.6 Native/Sandbox versions while retaining
  `Bannerlord.UIExtenderEx`.
- Accept deletion of the obsolete `.mcp.json`, retain the APM configuration, pin Cannons to `f02ba26`
  and ExpandedTemplate to `f1d93f4`, and remove stale 1.3 submodule branch hints.
- Build once after the merge to establish the 1.4 compile baseline before feature repairs.

### 11.2 Patch-seam decisions

| Surface | 1.4.6 decision |
| --- | --- |
| Army creation and candidate list | Public model for ordinary eligibility/filtering and scored behavior for the goal. Use one narrowly filtered `Kingdom.CreateArmy` prefix for final private member substitution because 1.4 has no post-scoring/pre-creation event and the shared member cache is unsafe under LIFO listener order. |
| AI `IsEnemy` / stance score | Keep Harmony; private helpers precede the public attack/avoid hooks. |
| Static encounter start and non-virtual battle/siege side gates | Keep targeted Harmony. |
| Besiege/continue/attack-army menu conditions | Prefer private-war-only registered menu options if public consequences reproduce vanilla exactly; otherwise retain the postfixes. |
| Village hostile/raid flow | Keep Harmony; the required start-hostile-action consequence is private. |
| Player captivity | Replace whole-method prefix with a predicate-level patch. |
| AI prisoner retention | Keep a tightly filtered veto; no pre-release model/event exists. |
| Sally-out strength | Prefer a predicate-level transpiler over a copied full-method prefix if exact IL matching is reliable. |
| Party/settlement nameplates | UIExtenderEx, never Harmony; explicit 1.4 relation mapping. |

Every retained patch gets an automated target-resolution assertion against the 1.4 assemblies. Every
transpiler asserts its exact replacement count and fails visibly when the IL shape changes.

### 11.3 Design/code gaps included in the hardening pass

- Add the clean `EncounterModel.FindNonAttachedNpcPartiesWhoWillJoinPlayerEncounter` override so
  nearby participants are enumerated before `MapEvent.CanPartyJoinBattle` assigns them.
- Recruitment restrictions use the clean volunteer model plus the public recruitment-entry callback;
  AI hostile-fief visitation uses the renamed 1.4.6 private predicate. Hostile-fief disband/army-camp
  filtering is explicitly deferred from v1 because the exact callers are private multi-stage selectors
  and a global `SettlementHelper` patch would affect offensive patrol/gathering behavior. No player-crime
  patch is carried: 1.4.6 `ChangeCrimeRatingAction.ApplyInternal` checks
  `Hero.MainHero.MapFaction != faction` before relation loss and `DeclareWarAction`.
- Preserve the 1.4 naval/port branches in all patched methods. Private-war naval support is verified
  where the feature permits it; unsupported naval cases must fall through to vanilla unchanged.

### 11.4 Verification matrix

Automated:

- existing private-war domain tests;
- army leader priority, fallback, one-per-side, same-side membership, and losing-score cases;
- Harmony target/overload resolution and transpiler replacement counts;
- serializer round-trip, legacy 11-field load, malformed record handling, and registry restoration;
- explicit settlement-nameplate relation values and color fallback.

Runtime under 1.4.6:

- AI-vs-AI field battle with correct reinforcements;
- attacker army capture and defender army reclaim of the frozen goal;
- siege retention, sally-out strength, garrison destruction, prisoner capture/release, and normal army
  dispersal;
- player field/army conversations, village raid, siege assault, captivity, ransom, escape, and peace;
- party/settlement nameplates and immediate hostility refresh;
- load a real 1.3.15 mid-war save in 1.4.6 and continue through resolution.

---

## 12. SDD execution tasks

The following tasks are the implementation order for the 1.4.6 port. Each production-code task uses
red-green-refactor, records the focused RED and GREEN commands, commits independently, and passes a
separate spec-and-quality review before the next task begins. The final branch receives an additional
whole-diff review.

### Global constraints

- Belligerents remain in their kingdom; hostility is pair-scoped and must never create a synthetic
  same-kingdom `DeclareWarAction`.
- Each side may assign at most one army to the frozen private-war goal. An eligible principal-clan
  party has exclusive leadership priority; another participating clan may lead only when no
  principal-clan party is eligible. Do not replace an existing fallback leader mid-army.
- Private-war army membership is limited to eligible parties on the same resolved side. Opposing and
  uninvolved same-kingdom parties are excluded.
- Use public 1.4.6 models, scored behavior APIs, campaign events, and UIExtenderEx seams where they can
  express the rule. Retained Harmony patches must be pair-filtered, target-verified, and no broader
  than the hard-coded predicate they replace.
- Do not restore the removed 1.3 `NoActiveWar` army-dispersion patch. Preserve vanilla cohesion,
  starvation, objective-completion, naval, and port behavior unless the private-war rule explicitly
  requires a change.
- Synthetic siege capture must take losing lords prisoner and transfer ownership once without using
  destroyed garrison references.
- Persistence must accept current 12-field and legacy 11-field records, tolerate malformed records,
  and restore the runtime hostility registry without losing the goal-last-taken fallback day.
- Do not run the known-hanging `DellarteDellaGuerra.Integration.Tests` command. Use focused tests and
  `dotnet build DellarteDellaGuerra.sln --configuration Release -v:minimal`.

### Task 1: Integrate the Bannerlord 1.4.6 baseline

Merge `migrate-to-1.4` into this feature branch without rebasing the feature history. Resolve the
known metadata and submodule overlaps exactly as specified in §11.1, retaining UIExtenderEx while
adopting the migration branch's 1.4.6 references and jousting compatibility fix. Remove obsolete 1.3
submodule branch hints and accept deletion of the obsolete `.mcp.json`.

This is a branch/configuration integration task rather than a behavior change, so TDD does not apply.
Verify the resolved metadata and gitlinks, run the existing private-war domain suite, then run one
Release solution build to capture the remaining 1.4 feature-specific failures for Task 4.

### Task 2: Harden private-war save migration

Write failing persistence tests for current 12-field round-trip, legacy 11-field fallback,
non-numeric optional goal-last-taken data, malformed-record isolation, and hostility-registry
restoration. Fix `PrivateWarStateSerialiser` and the campaign save/load adapter minimally so malformed
optional data cannot overwrite the fallback and a bad record cannot poison valid records.

Focused verification: the new persistence tests plus existing private-war domain tests.

### Task 3: Define and test private-war army policy

Introduce the smallest testable policy needed to decide whether a party may lead the private-war
army and which parties may join it. Start with failing tests for principal-clan priority, participating-
clan fallback, one-army-per-side, no mid-army replacement, same-side membership, opposite/uninvolved
exclusion, ineligible parties, and the frozen-goal association. Keep Bannerlord objects at the adapter
edge where practical; do not add speculative configuration.

Focused verification: the new policy tests and all private-war domain tests.

### Task 4: Port and connect 1.4.6 army decision making

Port `DadgArmyManagementCalculationModel` from the removed
`GetMobilePartiesToCallToArmy(MobileParty)` method to
`CanLordCreateArmy(MobileParty, out MBList<MobileParty>)`. Preserve vanilla eligibility gates, filter
ordinary kingdom armies away from private-war enemies, and allow a private-war-only army when the
Task 3 policy authorizes it.

Change the hourly private-war goal injection so an authorized `BesiegeSettlement` candidate sets
`WillGatherArmy` but never writes the shared `PartyThinkParams` member cache. Add the narrowly filtered
`Kingdom.CreateArmy` prefix described above so the already-selected matching private-war creation gets
a freshly revalidated same-side list. Write failing adapter/patch tests for leader priority/fallback,
ordinary-army filtering, private-war-only formation, matching type/leader/frozen-goal filters,
revalidation failure, unchanged ordinary calls, and the absence of a private-listener
`SetArmyMembers` call.

Focused verification: the army adapter tests, private-war domain tests, and the affected application
project build against 1.4.6.

### Task 5: Fill clean model and campaign-event gaps

Write failing tests and implement the missing clean reinforcement override in `DadgEncounterModel`.
Then cover the other pre-existing requirements from §11.3: private-enemy recruitment restrictions,
AI hostile-fief visitation, hostile-fief retreat/camp filtering, and the player-crime guard. Prefer
public model or campaign-event seams; if one requirement has no clean seam, add only the narrowly
filtered patch documented by the source audit. Unrelated and cross-kingdom behavior must remain
vanilla.

Focused verification: tests for each gate with private enemy, same-side participant, uninvolved
same-kingdom clan, and normal cross-kingdom enemy cases.

### Task 6: Harden Harmony targets and narrow copied behavior

Add automated 1.4.6 target/overload resolution for every retained private-war and heraldry patch.
Any transpiler must assert its exact replacement count and fail visibly on IL drift. Using failing
behavior tests where practical:

- replace the whole `PlayerCaptivityCampaignBehavior.CheckCaptivityChange` prefix with interception
  of only the no-more-enemies decision, preserving ransom and time escape;
- keep the prisoner-release veto pair-filtered;
- narrow the sally-out change to the same-faction strength predicate if an exact safe replacement is
  available;
- replace the three simple encounter-menu condition patches with private-war-only registered options
  only if the public consequences reproduce vanilla exactly; otherwise keep verified postfixes;
- retain village-flow Harmony and preserve 1.4 naval/port fall-through.

Focused verification: target-resolution tests, replacement-count tests, captivity branch tests, and
the affected application build.

### Task 7: Make siege retention and synthetic capture 1.4-safe

Write failing tests around the synthetic capture sequence before changing it. Ensure losing lords are
captured, ownership transfers exactly once, the 1.4 `ApplyBySiege` garrison destruction does not cause
a second destruction or stale reference, and the frozen goal can later be reclaimed. Verify that the
scored `BesiegeSettlement`/army attachment path survives
`BesiegerCamp.CheckBesiegerPartiesAndMakeThemLeave`; patch only if the public behavior path cannot
retain a valid private-war besieger.

Focused verification: capture-sequence tests, siege-side tests, affected build, then a targeted
AI-vs-AI attacker-capture/defender-reclaim runtime scenario.

### Task 8: Correct 1.4 nameplates and immediate refresh

Write failing tests proving settlement relation values are mapped explicitly to 1.4.6
`Neutral`/`SameFaction`/`Enemy`/`Ally` semantics rather than cast from the domain enum ordinal. Cover
party and settlement private-enemy colors, same-side/uninvolved fallback, missing-color fallback, and
refresh after war start/end. Retain UIExtenderEx; do not introduce Harmony for nameplates.

Focused verification: color/mapping tests, prefab selector resolution, and an in-game start/end
refresh check.

### Task 9: Complete compatibility and runtime verification

Run all permitted targeted automated suites and the Release solution build. Resolve any failures with
a reproducing test first. Under Bannerlord 1.4.6, execute the §11.4 runtime matrix, including AI army
formation, siege retention, captivity/ransom/escape, prisoner release, nameplates, and a real 1.3.15
mid-war save migration. Record unsupported naval private-war cases explicitly; otherwise verify that
naval and port branches fall through to vanilla.

Commit only fixes supported by a failing automated test or a reproducible runtime scenario. This task
is complete only when results and any deliberate deferrals are recorded in the existing manual test
guide or this implementation plan.
