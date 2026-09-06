# Phase 4 — The daily claim evaluator

Design: `feudal-hero-claims-and-succession-design.md` §3, §6 row 4; `feudal-private-wars-design.md` §7, §9,
§16, §18.C–D.
Predecessors: phase 1 (`Title.HolderHeroId`), phase 2 (`ExecuteSuccessionUseCase`), phase 3 (claims descend
from the holder and his father).

## 1. Scope

Phases 1–3 built a realm full of claims and nothing that acts on them. Phase 4 is the driver: once a day,
look at the claims, decide whether any is worth pressing, and declare a private war through the mechanism
that already exists in `src/submodules/PrivateWars/`.

Design §6 row 4 asks for the evaluator **inter-clan only**, with the player and same-kingdom gates. Verify:
run a campaign at speed, `campaign.list_private_wars` shows a plausible declaration rate, never involving
the player and never crossing a kingdom border.

Two things this phase is **not**:

- It is not intra-clan war. Phase 3 made a holder's brothers and passed-over sons claimants against their
  own house — 302 of the 399 baseline claims are in-clan — but a clan cannot be at war with itself. The
  cadet spinoff that turns an in-clan claimant into a belligerent clan is phase 6. In this phase the in-clan
  claim is a hard skip, and it removes three quarters of the candidate set.
- It is not scoring or resolving a war. `PrivateWarCampaignBehavior.OnDailyTick` in the submodule already
  scores and resolves; DADG only decides *whether to start one*.

## 2. Conflicts found in the design

### 2.1 Who is the defender — the holder, or whoever holds the seat?

`feudal-private-wars-design.md` §7 defines the defender as "the clan currently **owning the claimed title's
seat** (de facto holder)". `feudal-hero-claims-and-succession-design.md` §3 gates on the **holder's**
kingdom. Since phase 1 those are different clans whenever a title is contested.

**Resolved: the defender is the holder's clan.** A claim is a claim on a dignity, and the dignity is what
the war transfers; §7 predates the de jure/de facto split landing in code. The de facto case is not lost —
it falls out of the main-goal precondition below. A holder whose seat is under occupation holds none of the
title's de jure settlements, so no main goal can be computed and the claim cannot be pressed at all, which
is exactly what amendment D says should happen to a "de-facto-dispossessed" defendant.

### 2.2 The main goal is not the seat

`feudal-private-wars-design.md` §7 says main goal = `Title.SeatSettlementId`. Amendment §18.C supersedes it:
the highest-prosperity settlement **among the claimed title's de jure settlements currently held by the
defendant**, frozen at declaration. The submodule already implements exactly this in
`MainGoalSelector.Select` and `DeclarePrivateWarUseCase` rejects a declaration with a null goal
(`DeclarePrivateWarUseCase.cs:32`), so the precondition is enforced in code, not merely documented. What the
submodule does *not* do is work out which settlements are the title's de jure settlements — that is DADG's
data and DADG's job.

### 2.3 Per claim, or per clan-and-title?

Design §3 evaluates per claim and persists a per-claim `NextEvaluationDay`. But a war is declared clan
against clan over one title, and the submodule already refuses a second active war for the same
(attacker, defender, casus belli, title) tuple. Several heroes of one clan holding claims on one title —
common after phase 3 — would each roll the random gate independently and inflate the declaration rate for
no gameplay reason.

**Resolved: evaluate per (claimant clan, title), taking that clan's strongest claim on the title.** The
cooldown is keyed the same way. This is the war's own identity, and it collapses the duplicate rolls.

### 2.4 Strength is summed over the subtree, before supporter sets exist

Design §3's score table sums `Clan.CurrentTotalStrength` "over each side's subtree", but phase 5 is the
phase that adds supporter sets. The subtree is not speculative, though: the submodule's `WarSideResolver`
already assigns any clan to a side by walking **up** its suzerain chain to the first belligerent principal,
and DADG already feeds it that chain through `FeudalHierarchyAdapter`. A war declared today therefore drags
in vassals whether or not phase 5 has run, so summing only the two principals would systematically
misjudge every liege-vs-vassal matchup.

**Resolved: sum the subtree now**, in DADG, with the same nearest-belligerent-ancestor rule the submodule
resolves sides by. It is a pure function of a clan→strength map and a clan→suzerain lookup, so it is a
domain helper with its own tests and touches no submodule code.

## 3. The gates, in order

Cheapest first; each is a `continue`, and nothing below a failed gate is computed.

| # | Gate | Source | Why |
|---|---|---|---|
| 1 | claimant clan == holder clan | `IGenealogy.GetClanOf` | inter-clan only (§1); phase 6 owns the rest |
| 2 | either clan is the player's | `Clan.PlayerClan` | decision 3 — no UI, no consent flow |
| 3 | either kingdom null, or the two differ | `Clan.Kingdom` | decision 5 — private wars are same-`MapFaction` by construction |
| 4 | attacker already a belligerent | `IPrivateWarsApi.GetWarsByClan` | design §9, one war at a time per claimant; the defender may face several |
| 5 | cooldown not elapsed | persisted `NextEvaluationDay` | see §4 |
| 6 | no main goal | `MainGoalSelector.Select` over the title's de jure settlements | amendment §18.D — hard precondition |

Only past gate 6 is a score computed.

## 4. The cooldown, and why it is not optional

Without it, a pair sitting just under the threshold is re-evaluated 365 times a year and declares the first
time noise crosses the line — the threshold stops meaning anything. A `NextEvaluationDay` is stamped on
**every** evaluation that reaches gate 5, whether or not it declares, so a pair is looked at roughly
monthly. It is persisted under its own `SyncData` key; losing it on load would restore the churn on the
first day after every save.

The key is `{titleId}:{claimantClanId}` (§2.3). Blood claim ids are deterministic
(`{titleId}:{heroId}:blood`), but they are rebuilt wholesale on every death and dispossession, and the hero
who carries the clan's claim can change without the clan's appetite changing.

## 5. The score

Pure, in the domain, over a flat snapshot the behaviour builds:

```
ratio = attackerStrength / defenderStrength
if ratio < 1.25                        -> 0        (below appetite; design §3 "attack at ≈1.25:1 or better")
score  = appetite(claimStrength)                   (Weak / Strong / DeJure)
       + min(ratio - 1.25, 1) * ratioWeight
       + distracted ? distractionBonus : 0         (defender's kingdom at war, or already a belligerent)
       + (-relation / 100) * relationWeight        (hatred pushes, friendship restrains)
```

Constants live next to the behaviour they govern, as the claim decay levels do in
`GenerateBloodClaimsUseCase`. A Weak claim can only clear the threshold when the ratio, the distraction and
the hatred all line up; a Strong claim clears it on a good ratio alone. That asymmetry matters more than
usual here: of the 97 inter-clan claims in the 399 baseline only **3** are Strong, so in this phase the
ratio and distraction terms do nearly all the work. Phase 6 is what turns the 302 in-clan claims — 180 of
them Strong — into candidates, and the threshold will want revisiting then.

A random gate follows the threshold so that a day on which several pairs qualify does not declare all of
them at once.

## 6. Change surface

### Domain (`src/DellarteDellaGuerra.Domain/Titles/`)

- **`Model/ClaimOpportunity.cs`** — new record: `TitleId`, `AttackerClanId`, `DefenderClanId`,
  `Strength`, `AttackerStrength`, `DefenderStrength`, `DefenderDistracted`, `Relation`. Flat and
  engine-free; the behaviour fills it.
- **`EvaluatePressClaimUseCase` / `IEvaluatePressClaimUseCase`** — `float Execute(ClaimOpportunity)`, the
  §5 formula and its constants. Kept separate from `EvaluateClaimUseCase`, which stays the boolean validity
  check it is (design §3).
- **`WarSideStrength`** — static, pure: sums a clan→strength map onto two sides by walking each clan up its
  suzerain chain to the first principal it meets, cycle-guarded. Mirrors
  `Bannerlord.PrivateWars.Domain.WarSideResolver` without depending on it (that one needs a `PrivateWar`,
  which does not exist yet at declaration time).
- **`GetDeJureSettlementsUseCase` / `IGetDeJureSettlementsUseCase`** — `IReadOnlyList<string>
  Execute(string titleId)`: the title's own seat plus every descendant title's seat, by recursing
  `IFeudalStructure.GetDeJureVassalTitleIds` and reading `ITitleRepository.GetTitle(id)?.SeatSettlementId`.
  `BuildFeudalMapUseCase.BuildEntry` is the existing precedent for that walk; there is no ready-made
  flatten-to-settlements helper to reuse.

### Application (`src/DellarteDellaGuerra/Titles/Api/Campaign/`)

- **`IPrivateWarDeclaration`** — new port, beside `IFeudalStateStore` and for the same reason: the
  application project references only `DellarteDellaGuerra.Domain`, so it cannot see
  `Bannerlord.PrivateWars.API` without a new project reference, and it should not.

  ```csharp
  bool IsBelligerent(string clanId);
  string? SelectMainGoal(string defenderClanId, IReadOnlyList<string> deJureSettlementIds);
  void Declare(string attackerClanId, string defenderClanId, string titleId,
               string mainGoalSettlementId, float day);
  ```

  `SelectMainGoal` is separate from `Declare` because the goal is gate 6 — it has to be answered before the
  score and the random roll, not discovered inside a failed declaration.
- **`ClaimPressureCampaignBehavior`** — new `CampaignBehaviorBase` on `CampaignEvents.DailyTickEvent`.
  Runs the §3 gates, builds the snapshot from `Clan.CurrentTotalStrength`, `Clan.Kingdom`,
  `IFaction.IsAtWarWith` and `Hero.GetRelation`, scores, declares, and persists the cooldown map under
  `DadgClaimPressureCooldowns`. Not folded into `FeudalTitleCampaignBehavior`, which owns lifecycle and
  persistence and has none of these dependencies (design §3).
- **`Spi/ClaimCooldownSerialiser`** — new, `Serialise`/`Deserialise` for the cooldown map in the same
  pipe-delimited style as `TitleStateSerialiser`, which is left untouched: that one converts the domain
  records, and the cooldown map is not one of them.

### Integration (`src/DellarteDellaGuerra.Integration/`)

- **`PrivateWars/PrivateWarDeclarationAdapter.cs`** — implements `IPrivateWarDeclaration` over
  `IPrivateWarsApi`: `GetWarsByClan` filtered to `PrivateWarStatus.Active` for `IsBelligerent`;
  `MainGoalSelector.Select` over `SettlementInfo` built from live `Settlement` data for `SelectMainGoal`;
  `DeclareWar(..., ClaimCasusBelli.ClaimType, ...)` with the defender's fief snapshot for `Declare`. Sits
  beside the existing `FeudalHierarchyAdapter`, which is the same shape of adapter.
- **`DI/DadgServiceContainer.RegisterTitleServices`** — register the two new use cases, the adapter and the
  behaviour, in the established `AddSingleton` style.
- **`SubModule.InitializeGameStarter`** — one more `AddBehavior`, after `FeudalTitleCampaignBehavior`.
  Registration order against the submodule's own `PrivateWarCampaignBehavior` is harmless: a war declared
  this tick scores ~0 (design §3).

### Tests

- `EvaluatePressClaimUseCaseTests` — below the ratio floor scores 0; DeJure > Strong > Weak at equal
  ratios; distraction and hatred raise the score, friendship lowers it; a defender with no strength does
  not divide by zero.
- `WarSideStrengthTests` — a vassal counts for its liege; a clan under the attacker who is under the
  defender further up counts for the attacker (nearest belligerent ancestor); an uninvolved clan counts for
  neither; a suzerain cycle terminates.
- `GetDeJureSettlementsUseCaseTests` — own seat plus descendants at both levels; a vassal title with no
  record is skipped rather than throwing.

No unit tests for the behaviour itself: it is engine types end to end, which is why the gates that can be
tested were pushed into the domain and the rest is a live check.

## 7. Execution order and verification

1. Domain records, use cases and the static helper → verify: new unit tests pass, `dotnet build` clean.
2. Port + adapter → verify: build clean; the adapter is the only new file that names a submodule type.
3. Behaviour + DI + `SubModule` registration → verify: full `dotnet test` still green at the phase 3
   baseline (235) plus the new cases; the 399-claim baseline is untouched, because this phase reads claims
   and never writes them.
4. Live → verify: run a campaign at speed, `campaign.list_private_wars` shows declarations accumulating at
   a plausible rate, none involving the player's clan, none across a kingdom border.

Build and test through the `subst W:` mapping as usual, with `DADG_MODULES_ROOT` set so the real-content
tests can still find the sibling map module.

## 8. Risks

| Risk | Assessment | Mitigation |
|---|---|---|
| Declaration rate is wildly wrong on first contact | Likely — the constants are a first guess and no phase has ever produced a declaration before | The threshold, the cooldown and the random gate are three constants in one file; step 4 is a tuning loop, not a pass/fail |
| Almost nothing qualifies | Real — only 3 of 97 inter-clan claims are Strong | Expected and stated in §5. If the campaign runs dry, the honest fix is phase 6 (in-clan claimants, 180 of them Strong), not a lowered threshold |
| The subtree sum drags a whole kingdom onto one side and every ratio lands near 1:1 | Plausible where one clan sits above most of the hierarchy | `WarSideStrength` is pure and unit-tested against exactly this shape; if it proves wrong the term is one call to change |
| Cooldown map grows without bound | Bounded by (titles × clans) with entries only for pairs actually evaluated, and it is strings in a save | No action; noted so the next reader does not add a reaper |
| A war is declared over a title whose holder dies the same day | Possible; succession runs on `HeroKilledEvent`, the evaluator on `DailyTickEvent` | Harmless — the war names the title, not the holder, and the submodule scores it from settlement control |

## 9. Outcome

Built as planned, with three deviations, all of them engine-API corrections caught at compile time:

- `Clan.TotalStrength` does not exist; the snapshot uses `Clan.CurrentTotalStrength`.
- `FactionManager.GetEnemyKingdoms` does not exist. `IsDistracted` now takes the kingdom the caller has
  already resolved and asks `Kingdom.All.Any(other => other != kingdom && kingdom.IsAtWarWith(other))` —
  `IFaction.IsAtWarWith` is what the private-wars submodule uses throughout.
- `Settlement.Prosperity` does not exist; the adapter reads `Settlement.Town?.Prosperity ?? 0f`, which
  covers towns and castles and leaves villages at 0, where `MainGoalSelector` tie-breaks them by id.

One simplification came out of writing the tests: the score carried a `Math.Max(score, 0f)` clamp that can
never fire — relation is bounded to ±100, so the worst friendship takes 0.2 off a weak claim's 0.35.
Removed, with a comment in its place (AGENTS.md §2).

`MainGoalSelector` is registered in no container, so `IPrivateWarDeclaration` is registered with an
explicit factory that constructs one, rather than adding a container entry for a submodule type nothing
else in DADG uses.

**Verified:** the solution builds clean (0 errors; 10 warnings, all pre-existing and outside this change).
`dotnet test` is green at **251** — 222 domain + 29 infrastructure, the phase 3 baseline of 235 plus the 16
new cases. The 399-claim baseline is untouched, as expected: this phase only reads claims.

**Not verified:** step 4, the live campaign check. It needs a running game, and the declaration rate is the
one thing no unit test can judge — expect the threshold, the cooldown and the random gate to need tuning on
first contact.
