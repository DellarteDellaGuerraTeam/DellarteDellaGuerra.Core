# Phase 2 — Succession on a holder's death

Companion to `doc/feudal-hero-claims-and-succession-design.md` §2 and `doc/feudal-hero-titles-phase-1-implementation-plan.md`.
Phase 1 made a title name a hero. Phase 2 makes that hero *replaceable* when he dies.

---

## 1. Scope

When a title holder dies, the dignity passes to his heir by male-preference primogeniture instead of
sitting on a corpse. Nothing else changes: claims, the daily evaluator, cadet clans and supporter sets all
stay where they are.

**In:**

* `HeroNode` gains `FatherId` and `Age` — enough to reach siblings and to order by seniority.
* `ExecuteSuccessionUseCase` — pure domain, resolves an heir from genealogy alone.
* `FeudalTitleCampaignBehavior.OnHeroKilled` calls it **before** re-deriving blood claims.

**Out:** everything §6 assigns to phases 3–6.

### Trigger: holder death, not leader death

Taken from design §2 verbatim, because it is a deliberate divergence from the original requirement's
wording. Once titles are hero-held, a non-leader brother can hold a barony and die too; triggering on clan-leader
death alone would silently orphan every such title. `OnHeroKilled` already fires for every hero, so the
use case is handed the victim's id and returns immediately when he held nothing.

**Ordering hazard.** Vanilla's `ChangeClanLeaderAction` also runs on death and event order is not
guaranteed. The heir is therefore resolved from `IGenealogy` alone — `GetClanLeaderId` is read *only* in
the last-resort backstop, where a stale answer is still better than an orphan.

---

## 2. Conflict found in the design: passed-over heirs cannot get claims in this phase

Design §2.1 ends with "every passed-over living child is minted a `ClaimStrength.Strong` /
`ClaimOrigin.Inheritance` claim", and §6's phase-2 row asks to verify that "the second son holds a Strong
claim". **Neither is reachable without phase 3.** Two facts in the shipped code, both verified:

1. `GenerateBloodClaimsUseCase.RemovePreviouslyDerivedClaims` deletes *every* `ClaimOrigin.Inheritance`
   claim on every title before rebuilding. It runs immediately after succession on the same event. Any
   claim succession minted would be erased microseconds later.
2. Derivation skips same-clan claimants outright — `if (hero.ClanId == holderClanId) continue;`. A
   passed-over brother stays in the clan that now holds the title, so re-derivation will not mint the claim
   either.

The passed-over brother is therefore invisible to the claim system by construction, and stays invisible
until self-exclusion moves from *clan* to *hero* — which is exactly design §2.4, phase 3's first line.

**Options considered.** Minting claims outside `Inheritance` (say a `Succession` origin) would dodge the
wholesale rebuild, but it invents a claim kind the design doesn't have and phase 3 would immediately delete.
Pulling per-hero self-exclusion forward is a one-line edit with a realm-wide blast radius: every living
kinsman of every holder gains a claim against his own clan's titles, which rewrites the claim economy and
the 42-claim integration baseline. That is phase 3's whole job and it deserves its own baseline diff.

**Taken:** phase 2 moves titles and mints nothing. Phase 3 inherits the "second son holds a Strong claim"
assertion along with the self-exclusion change that makes it true. Design §6's phase-2 verify row is
corrected accordingly — the succession half is testable now, the claim half is not.

---

## 3. Representation is on

Design §2.2 recommends it and leaves the call explicit, so: **on**. A predeceased eldest son's own children
inherit before their uncle.

The setting argues for it more strongly than the mechanic does. Representation *through a female line* is
the whole Yorkist case — Richard of York descended from Lionel of Antwerp through a daughter, which is why
his claim outranked Lancaster's. A succession system for the Wars of the Roses that cannot express that is
not modelling its own central dispute.

Cost is one recursive call: the direct-line walk becomes `HeirOfBody`, which recurses into a predeceased
child's descendants before moving to the next child.

---

## 4. Change surface

### Domain

**`Model/HeroNode.cs`** — two optional trailing parameters, so every existing construction site still
compiles:

```csharp
public record HeroNode(
    string Id, bool IsFemale, bool IsAlive, string? ClanId, IReadOnlyList<string> ChildIds,
    string? FatherId = null, float Age = 0f);
```

**`Model/SuccessionResult.cs`** *(new)* — `(string TitleId, string PreviousHolderHeroId, string? NewHolderHeroId)`,
mirroring `AssignmentResult`. Only the new holder is nullable: a title with no resolvable heir is left vacant.

**`IExecuteSuccessionUseCase.cs` / `ExecuteSuccessionUseCase.cs`** *(new)*:

```
Execute(deceasedHeroId):
    titles = every title whose HolderHeroId is the deceased      # no port change: scan GetAllTitles
    if none: return empty

    heir = HeirOfBody(deceased)                                  # sons by age desc, then daughters
        ?? Collateral(deceased)                                  # father's other children, males then females
        ?? ClanLeaderBackstop(deceased)                          # never the deceased himself

    for each title: SaveTitle(title.WithHolder(heir))

HeirOfBody(ancestor):                                            # recursive — representation
    for child in ancestor.Children, male, Age desc:
        if child.IsAlive: return child
        if HeirOfBody(child) is {} h: return h
    for child in ancestor.Children, female, Age desc:
        ... same
    return null
```

`Collateral` is `Father.ChildIds` minus the deceased, males by age then females by age, each falling through
to `HeirOfBody` when predeceased. The backstop is `GetClanLeaderId(GetClanOf(deceased))`, discarded if it
resolves to the deceased himself — vanilla may not have run `ChangeClanLeaderAction` yet, and a title left
vacant is recoverable where a title held by a dead man is not.

No new port methods. Titles held by a hero come from `GetAllTitles()`, a 97-entry scan already run once per
claim rebuild.

### Infrastructure

**`CampaignGenealogy`** — `hero.Father?.StringId` and `hero.Age`. Both exist on `Hero` in 1.4.6.

**`DadgXmlGenealogy`** (test adapter) — `FatherId` from the `father` attribute, already parsed for the
child index. **`Age` stays 0:** the DADG hero XML carries only `id`, `alive`, `faction`, `father`, `mother`
and `spouse` — there is no age or birth-day attribute. Harmless, because this adapter feeds the claim
integration test and never the succession tests, and because `OrderByDescending` is stable in LINQ, so an
all-zero age field degrades to declaration order rather than to nondeterminism.

### Application

**`FeudalTitleCampaignBehavior.OnHeroKilled`** — one line becomes two, succession first:

```csharp
_executeSuccessionUseCase.Execute(victim.StringId);
_generateBloodClaimsUseCase.Execute();
```

Constructor and `DadgServiceContainer` registration grow by one dependency.

---

## 5. Execution order and verification

```
1. HeroNode + adapters + fakes          → verify: dotnet build succeeds, 211 existing tests still green
                                                  (behaviour-neutral: new fields default)
2. ExecuteSuccessionUseCase + unit tests → verify: eldest son / no sons → eldest daughter /
                                                  no children → eldest brother / no kin → clan-leader
                                                  backstop / dead leader → vacant, not the corpse /
                                                  representation: predeceased eldest son's son beats
                                                  his uncle / holder held several titles → all move
3. Wire OnHeroKilled + DI               → verify: dotnet build; claim baseline still 42
4. Live: campaign.kill_hero on a duke   → verify: the duchy's holder is the eldest son
```

Step 4 is the only one needing the game. Steps 1–3 cover every line of new logic.

---

## 6. Risks

| Risk | Likelihood | Mitigation |
|---|---|---|
| A dead holder's clan leader is himself (event ordering) → title held by a corpse | Medium | The backstop discards a heir equal to the deceased and leaves the title vacant; the next grant or conquest re-fills it |
| Female heir marries out and carries the title into her husband's clan | Certain, by design | Accepted — design §2.3. It is how half the great English estates changed hands |
| Recursion through a cyclic genealogy | **Happened** — see §8 | Guarded. A visited set is threaded through the descent walk; the original "impossible in engine data" reasoning was wrong because the data is authored, not engine-generated |
| Age ties order arbitrarily | Low | `OrderByDescending` is stable, so ties fall back to `ChildIds` order — deterministic, if not meaningful |
| Succession fires for the 400+ heroes who hold nothing | Certain | Early return on an empty title scan; the scan is the same 97-entry pass the claim rebuild already does on that event |

---

## 7. Outcome

Steps 1–3 are done and verified; step 4 (live) is still outstanding.

* `dotnet build W:\src\DellarteDellaGuerra.sln -c Debug -m:1` → 0 errors.
* `dotnet test` → **219 passed, 0 failed** (199 domain + 20 infrastructure), up from 211 by the eight new
  `ExecuteSuccessionUseCaseTests` cases: eldest son over an elder daughter, eldest daughter when no son
  survives, eldest brother when childless, clan-head backstop, vacant rather than a dead clan head,
  representation over an uncle, every held title moving together, and no-op when the deceased held nothing.
* The claim integration baseline is **unchanged at 42** — succession alters no claim, exactly as §2 predicts.

Nothing in §2's conflict analysis changed during implementation: no claim is minted here.

Since then the suite has grown to **230 passed, 0 failed** (201 domain + 29 infrastructure): two further unit
tests — a daughter's precedence over the holder's own brother, and the cycle guard of §8 — and the content
tests of §9.

## 8. A hero listed as his own parent, and the crash it caused

`ExecuteSuccessionUseCase` originally walked the bloodlines without a visited set, on the reasoning recorded
in §6: a child is strictly younger than its parent, so `Hero.Father` cannot close a loop. That is sound for
data the engine generates at runtime. It is not sound for data a designer types.

`DellarteDellaGuerraMap/ModuleData/heroes/dadg_heroes.xml` line 197 reads:

```xml
<Hero id="dadg_lord_23_1" faction="Faction.clan_mowbray"
      father="Hero.dadg_dead_lord_23_1" mother="Hero.dadg_lord_23_1" spouse="Hero.dadg_lord_23_2"></Hero>
```

The Duke of Norfolk is given as his own mother. Any parent→child index built from both parent attributes —
which is what both the campaign adapter and the test adapter build — then lists him among his own children,
and `HeirOfBody` descends into him forever. The failure mode is the worst available: a `StackOverflowException`
cannot be caught in .NET, so this is not a bad succession, it is the game process dying the moment Norfolk
falls in battle.

Claim derivation never tripped over it because `GenerateBloodClaimsUseCase.Descendants` carries a strength
counter that strictly decreases and stops below `WeakLevel`, bounding it to two generations. Succession walks
the whole line, so it was the first code to actually traverse the cycle.

Both halves are now fixed:

1. **The walk is guarded.** A `HashSet<string>` seeded with the deceased is threaded through `ResolveHeir`,
   `HeirOfBody` and `Collateral`. It also removes the need to special-case the deceased when iterating his
   father's children. This belongs in the code regardless of the content: the bloodlines are authored, and
   one typo should not be able to kill the process. `SkipsAHeroListedAsHisOwnParentRatherThanRecursingForever`
   holds it in place against a fake, and asserts the loop is *stepped over* rather than stopped at — a hero
   declared after the cycle still inherits.
2. **The content is corrected.** The mother now reads `dadg_lord_23_3`, whom `dadg_dead_lord_23_1` already
   named as his spouse. Norfolk still leaves no issue and has no siblings, so `duchy_norfolk` and
   `county_lynn` fall vacant on his death — but now by the succession rule, not by a malformed record.

## 9. Content tests: killing every holder in the 1471 setting

`HolderDeathOverRealContentTests` runs the `OnHeroKilled` chain — succession, then claim re-derivation — over
the shipped content through the production adapters. Authored families are knottier than any fake, so these
test the rules against bloodlines nobody wrote to suit them.

| Case | Holder | Outcome |
| --- | --- | --- |
| Only daughters, and a living brother | `dadg_lord_9_1` (Neville of Middleham) | Five dignities pass to his one daughter `dadg_lord_2_4`, aged 14, **not** to his brother of 40 who heads a cadet branch of six |
| Collateral by seniority | `dadg_lord_6_1` (Beaufort) | Childless; his eldest surviving brother takes both dignities, over a sister of 52 and over a nephew whose father was the younger brother |
| Line dies out | five clan heads with neither issue nor sibling | Vacant — the backstop refuses to name the dead man it would otherwise return |
| Sweep | all 53 holders | No crash; heir is never the deceased, never dead; a man's dignities never scatter across two heirs |

The Neville case is the one worth reading twice. His daughter married into Lancaster, so his death carries
Middleham, Warwick, Rye, Caerphilly and Glamorgan **out of the Neville clan and into a rival one** — design
§2.3 firing on the greatest magnate in the setting, and very nearly what history did with Warwick's daughters.
The chain test measures the consequence: claims fall 42 → 33, losing exactly nine and gaining none. Five were
the daughter's own — a claimant does not claim what she now holds — and four were the Neville cousins'
claims on Glamorgan, which is no longer a Neville title to inherit.

Seniority in these tests is real. `age` is not on the `<Hero>` record that carries the bloodline — it sits on
the `<NPCCharacter>` template in `dadg_lords.xml`, which the adapter already reads for sex, so the two join by
hero id. An earlier draft of these tests ran with `Age` left at 0 and recorded a caveat that ordering was
untested; wiring the real ages in moved two outcomes and is what the table above now reflects.

That correction is worth keeping in view, because ordering turns out to decide real cases. With ages at 0,
Somerset's dignities went to a *nephew*: declaration order put his predeceased brother first, and
representation stood the nephew in his father's place. With real ages the predeceased brother is 28 and the
surviving one 30, so the elder brother simply takes it himself and the nephew never comes up. Representation
is still the right rule and is still unit-tested — it is just not reachable anywhere in the shipped content
once seniority is honest. Guessing at the fixture had produced a test that passed while asserting the wrong
succession.
