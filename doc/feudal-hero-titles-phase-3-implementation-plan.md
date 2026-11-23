# Phase 3 — Hero-accurate claim derivation

Design: `feudal-hero-claims-and-succession-design.md` §2.3, §2.4, §6 row 3.
Predecessors: phase 1 (`Title.HolderHeroId`), phase 2 (`ExecuteSuccessionUseCase`).

## 1. Scope

Design §6 asks for four things in one phase:

1. hero-accurate claim derivation,
2. drop `PrincipalTitleByClan`,
3. per-hero self-exclusion,
4. daughters get claims.

They are one change, not four. All four fall out of a single correction: **claim derivation still thinks a
title belongs to a clan.** Phase 1 made titles hero-held everywhere except here.

Verify (design §6): brothers *and* sisters of a title-holder appear in claim queries; a passed-over second
son holds a Strong claim on the title his elder brother inherited.

## 2. The organising principle: claimants are the people succession could have picked

Phase 2 resolves an heir by walking the bloodlines in a fixed order — down from the deceased
(`HeirOfBody`), then up one level to his father and down again (`Collateral`). Phase 3 makes claim
derivation walk *the same two paths from the living holder*:

| Succession (phase 2) | Claim derivation (phase 3) | Reach |
|---|---|---|
| `HeirOfBody(deceased)` — down from the holder | anchor at `title.HolderHeroId` | sons Strong, daughters Weak, grandchildren Weak |
| `Collateral(deceased)` — up to `FatherId`, then down | anchor at `holder.FatherId` | brothers Strong, sisters Weak, nephews/nieces Weak |
| `ClanLeaderBackstop` | *no claim analogue* | a backstop is not a bloodline |

So the set of claimants on a title becomes the set of heroes who could inherit it. That is the property
worth having, and it is why the two anchors are the holder and his father and **not** the grandfather:
`Collateral` goes up exactly one level, so derivation goes up exactly one level. Anchoring the grandfather
would give the holder's uncles Strong claims that succession would never honour.

Nothing new is invented. The decay rule is unchanged — a holder's son takes Strong, a daughter takes Weak,
their children take Weak, and a Weak claim passes to nobody — it is only applied from honest anchors.

### What this deletes

Both existing anchor mechanisms go:

- **`GetClanLeaderId` as the living anchor.** After phase 1 a barony can be held by a hero who is not his
  clan's head, and the claims on it currently descend from the head instead of from the holder. This is a
  correctness bug that phase 1 introduced and phase 3 pays off; the port method stays, because
  `ExecuteSuccessionUseCase` still needs it for the backstop.
- **`PrincipalTitleByClan` + `GetDeceasedClanMemberIds`.** These exist because "the campaign data records
  no per-hero title history", so every dead clan member anchored the clan's highest-rank dignity as a proxy
  for *previous holders*. The proxy was always crude: one long-dead ancestor scattered claims across a
  portfolio, and the highest-rank tiebreak was arbitrary. The father anchor replaces it with the thing it
  was approximating — the previous generation of the actual holding line — and does so per title.
  `GetDeceasedClanMemberIds` then has no callers and leaves `IGenealogy`.

This is design §2.4's "a net simplification of existing code, not an addition", and it is: one anchor
helper, one grouping pass, and one port method removed.

## 3. Per-hero self-exclusion

Two places compare a claimant's **clan** to the holder's clan and must compare **heroes** instead.

`GenerateBloodClaimsUseCase`:

```csharp
if (hero.ClanId == holderClanId) continue;   // becomes
if (hero.Id == title.HolderHeroId) continue;
```

`EvaluateClaimUseCase` filters the same way at the point of use, so changing only derivation would mint
in-clan claims that every consumer then discards. It becomes a hero comparison **when the claim names a
hero**, falling back to the clan comparison when it does not — `Claim.ClaimantHeroId` is null for
`ClaimOrigin.Conquest`, which is genuinely clan-level.

### This is the phase's real blast radius

Same-clan claims stop being filtered, so a holder's own brothers and passed-over sons acquire claims
against the house they belong to. That is not a side effect — it is the requirement ("a passed-over second
son holds a Strong claim on the title his elder brother inherited") and the precondition for design §4,
the intra-clan war and cadet spinoff. Until now `GenerateBloodClaimsUseCase`'s remark that "a claim on a
title one's own clan already holds is not actionable" was true; from this phase it is false, and the remark
goes with it.

The initial-claim baseline will grow substantially and its diff must be read line by line, not just
counted.

## 4. Change surface

### Domain

- `GenerateBloodClaimsUseCase` — `Anchors(holderClanId, includeDeceased)` becomes `Anchors(title)`
  returning the holder and his father; delete `PrincipalTitleByClan`; hero-level self-exclusion; the
  holder-clan lookup survives only to stamp `Claim.ClaimantClanId`.
- `EvaluateClaimUseCase` — hero comparison with a clan fallback.
- `Port/IGenealogy` — remove `GetDeceasedClanMemberIds`; correct the doc comments that still call the clan
  head "the living holder of its titles".

### Infrastructure and tests

- `CampaignGenealogy`, `DadgXmlGenealogy`, `IdentityGenealogy`, `Fakes` — drop the removed port method.
- `GenerateBloodClaimsUseCaseTests` — `AnchorsTheClansDeadOnItsPrincipalTitleOnly` is deleted (the
  behaviour is gone); `IgnoresDescendantsStillInTheHoldingClan` inverts into a test that in-clan kin now
  *do* hold claims, with only the holder excluded; add the brother, the sister and the passed-over-second-
  son cases.
- `EvaluateClaimUseCaseTests` — add a same-clan-different-hero case and keep a conquest claim on the clan
  path.
- `InitialBloodClaimsIntegrationTests` — new baseline, reviewed by hand.
- `HolderDeathOverRealContentTests` — the claim-movement assertion on Warwick's death is restated against
  the new numbers.

## 5. Risks

| Risk | Assessment | Mitigation |
|---|---|---|
| Claim volume explodes and the daily evaluator (phase 4) drowns | Likely — every holder gains a brother-and-son cohort | Measure the new baseline before phase 4 sets its threshold; the evaluator's gates are phase 4's problem, not derivation's |
| Removing the deceased anchors silently drops legitimate long-line claims | Real; some 1471 claims descend from the dead | The father anchor recovers the near ones. Any claim lost is a claim two-plus generations from a holder, which the decay rule already scores as Weak-at-best |
| Cyclic bloodline sends the walk round forever | Guarded already — `Descendants` decays a level each hop and stops below Weak, which is why phase 2's crash never touched it | No change; noted so the next reader does not add a redundant visited set |
| In-clan claims break a consumer that assumes cross-clan | `EvaluateClaimUseCase` is the only filter today | Changed in this phase; `GetSuzerainUseCase` and the feudal map do not read claims |

## 6. Outcome

Done and verified.

* `dotnet build W:\src\DellarteDellaGuerra.sln -c Debug -m:1` → 0 errors.
* `dotnet test` → **235 passed, 0 failed** (206 domain + 29 infrastructure), up from 230.

### What changed in the unit tests

`AnchorsTheClansDeadOnItsPrincipalTitleOnly` is gone with the behaviour it named.
`IgnoresDescendantsStillInTheHoldingClan` became `GivesKinStillInTheHoldingClanAClaim` — the same fixture,
the opposite expectation. Four cases were added on a fixture that gives the holder a father:
`GivesThePassedOverSecondSonAStrongClaimOnHisBrothersTitle`, `GivesTheHoldersSisterAWeakClaim`,
`GrantsTheHolderNoClaimOnHisOwnTitle` and `StopsAtTheHoldersFatherRatherThanReachingHisUncles` — the last
pins the §2 decision not to anchor the grandfather. `EvaluateClaimUseCase` gained the two cases that
separate the hero path from the clan path.

### The new baseline

| | Before | After |
|---|---|---|
| Claims | 42 | **399** |
| Strong | 1 | **183** |
| Weak | 41 | **216** |
| Titles with claimants | 23 | **89** |
| Claimant clans | 31 | **53** |

A 9.5× growth, and the shape of it is right rather than merely large. Three examples read straight off the
baseline:

* **`barony_middleham`** — Warwick's brother `dadg_lord_9_2` holds a Strong claim; six more of his father's
  children hold Weak ones; the five claimants in other houses keep the Weak claims they already had; Warwick
  himself is absent. That is the phase's headline requirement, on real content.
* **`duchy_york`** — three of the house's own take Strong claims on the duchy and two more Weak, alongside
  the pre-existing de la Pole and Holland claims. The Yorkist succession dispute is now expressible.
* **`duchy_somerset`** — the Strong claimant is `dadg_lord_6_2`, who is exactly the hero phase 2's
  `ChildlessTheDukeIsSucceededByHisEldestSurvivingBrother` proves would inherit. Derivation and succession
  agree on the same man, which is the invariant §2 set out to buy.

### Warwick's death, restated

`TheClaimsAreRederivedFromTheNewHolderOnceTheSuccessionHasRun` now runs 399 → 339: **sixty claims go, none
appear, and no title outside Warwick's five is touched.** All five are left with no claimants at all, which
is correct rather than surprising — the heiress was a claimant and is now the holder, she has neither
children nor a sibling, and the aunts and cousins who reached those titles through Warwick's *father* now
stand two levels above the new holder, outside the one-level reach the collateral rule allows. The old
assertion listed nine exact keys; listing sixty would be noise, so the test asserts the count, that every
lost claim belongs to one of the five titles, and that the five end up empty.

### For phase 4

399 is the number the daily evaluator has to gate, not 42. Most of the growth is Weak in-clan claims, so
the evaluator's threshold work is now load-bearing in a way the §5 risk table anticipated.
