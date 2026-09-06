# Feudal Succession

What happens to a dignity when its holder dies, as the code currently behaves.

The design rationale — the options weighed, the alternatives rejected — is in
`feudal-hero-claims-and-succession-design.md` §2. That is a decision record written before the rule was
built, and it is now behind the code in four ways; see §7 below. This document describes the rule as it
runs.

**Where it lives**

| Concern | File |
| --- | --- |
| The rule | `src/DellarteDellaGuerra.Domain/Titles/ExecuteSuccessionUseCase.cs` |
| The trigger | `src/DellarteDellaGuerra/Titles/Api/Campaign/FeudalTitleCampaignBehavior.cs` |
| The bloodlines it reads | `Domain/Titles/Port/IGenealogy.cs`, `Infrastructure/Titles/CampaignGenealogy.cs` |
| What it reads them into | `Domain/Titles/Model/HeroNode.cs` |
| What it reports | `Domain/Titles/Model/SuccessionResult.cs` |

## 1. Trigger

`CampaignEvents.HeroKilledEvent` → `FeudalTitleCampaignBehavior.OnHeroKilled`, which is two calls in a
fixed order:

```csharp
_executeSuccessionUseCase.Execute(victim.StringId);
_generateBloodClaimsUseCase.Execute();
```

**Any** hero's death fires this, not just a clan leader's. Since phase 1 a title names a hero
(`Title.HolderHeroId`), so a non-leader younger brother can hold a barony and die holding it; triggering on
leader death alone would silently orphan every such title. `Execute` returns immediately when the dead hero
held nothing, which is the overwhelming majority of deaths.

The order matters. Succession runs first so that the re-derivation behind it sees the new holders instead of
deriving a realm's worth of claims from a corpse.

## 2. The rule — male-preference primogeniture

`ResolveHeir` tries three sources and takes the first hero it finds:

```
HeirOfBody(deceased)  ??  Collateral(deceased)  ??  ClanLeaderBackstop(deceased)
```

### 2.1 Heir of body — the deceased's own line

`BySeniority` orders a parent's children **sons eldest-first, then daughters eldest-first**. The walk takes
them in that order: a living child is the heir; a dead child is replaced by his own descendants
(representation), depth-first, before the walk moves on to the next child.

Because every son precedes every daughter in that ordering, **a daughter inherits only when no son's line
survives at all** — not merely when the eldest son is dead. A predeceased son's daughter therefore inherits
ahead of the deceased's own daughter.

Representation applies to daughters as well: a predeceased daughter's children stand in her place. That is
deliberate rather than incidental — descent through a female link is the entire Yorkist legal claim in this
setting.

### 2.2 Collateral — the deceased's brothers and sisters

Reached through `FatherId`, because a hero's siblings are his father's other children: brothers
eldest-first, then sisters, each predeceased sibling represented by his own line exactly as a child is.

**The walk goes up exactly one level.** There is no grandfather step, so uncles, aunts and cousins are never
heirs, and a hero with no recorded father has no collateral heirs at all.

### 2.3 Clan-head backstop

The head of the clan the deceased belonged to (`IGenealogy.GetClanLeaderId`) — and only if that hero is
alive.

`Clan.Leader` is never consulted earlier in the chain. Vanilla's `ChangeClanLeaderAction` fires on the same
death and the order of the two is not guaranteed, so reading it to find an heir would be a race; as a last
resort a stale answer still beats an orphaned title. If the recorded head is himself dead — likely, since
the engine may not have processed its own handover yet — the title is left **vacant** instead.

## 3. What happens to the titles

- **Every title the deceased held passes to the same heir.** No splitting by rank, no partition.
- **Vacancy is a legitimate outcome.** `Title.HolderHeroId` becomes null and
  `SuccessionResult.NewHolderHeroId` is null. The next grant or conquest re-fills it.
- **Only the dignity moves.** `Title.WithHolder` leaves `OccupantClanId` and `ContestedSinceDay` alone, so a
  title whose seat is under enemy occupation stays contested across the succession.
- **The heir's clan is not checked.** Holder clans are derived through `IGenealogy.GetClanOf`, so a daughter
  who inherits and later marries carries the dignity into her husband's house — `MarriageAction` moves the
  bride's clan and the derived holder follows automatically. Period-accurate, and intended.
- **Succession mints no claims.** It moves holders and nothing else. Passed-over kin get their claims from
  the re-derivation that runs immediately after it (§5).

## 4. The cycle guard

`ResolveHeir` carries a `HashSet<string> visited` seeded with the deceased. This is not defensive
programming for its own sake. The bloodlines are authored XML, not engine-generated data, and
`DellarteDellaGuerraMap/ModuleData/heroes/dadg_heroes.xml` contained a hero listed as his own father. The
unguarded walk recursed until the process died, and a `StackOverflowException` cannot be caught in .NET — so
that was a crash, not a bad succession.

Seeding the set with the deceased also does useful work: iterating his father's children in `Collateral`
skips him without a special case.

Claim derivation needs no equivalent guard — its strength counter bounds it to two generations whatever the
data says.

## 5. Claims after a succession

`GenerateBloodClaimsUseCase` then rebuilds **every** `ClaimOrigin.Inheritance` claim in the realm from
scratch, anchoring each title at the holder and the holder's father — deliberately the same two walks as
§2.1 and §2.2, so that every claimant is a hero succession could have picked. The dead hero's own claims
vanish because he is no longer alive.

The consequence worth knowing: the passed-over second son holds a **Strong** claim on the dignity his elder
brother has just inherited, and he is in the same clan. Same-clan claims are no longer filtered out —
`EvaluateClaimUseCase` bars only the holder himself — which is what lets a house go to war with itself.

## 6. Behaviour pinned by tests

`src/DellarteDellaGuerra.Domain.Tests/Titles/ExecuteSuccessionUseCaseTests.cs` — the eldest son over an
elder daughter; the eldest daughter when no son survives; a daughter over the holder's own brother; the
eldest brother when the holder is childless; the clan head when no kin survives; vacant rather than a dead
clan head; a predeceased son's child ahead of his uncle; every held title moving to one heir; a no-op when
the deceased held nothing; and a hero listed as his own parent, skipped rather than recursed into.

`src/DellarteDellaGuerra.Infrastructure.Tests/Titles/HolderDeathOverRealContentTests.cs` runs the same rule
over the shipped 1471 content:

- Warwick's five dignities pass to his only daughter rather than to his brother.
- Somerset, childless, is succeeded by his eldest surviving brother — the same hero claim derivation names
  as the Strong claimant on `duchy_somerset`.
- Five holders whose lines die out leave their titles vacant.
- Every holder in the setting can be killed without orphaning or misplacing a dignity.
- Warwick's death takes the realm from 399 claims to 339: sixty go, none appear, and all sixty belong to his
  five titles.

## 7. Where the design doc differs

`feudal-hero-claims-and-succession-design.md` §2.1's pseudocode predates the implementation and differs from
it in four places:

1. it gives daughters no representation branch — the code represents any predeceased child;
2. its backstop returns the clan leader unconditionally — the code refuses a dead one and leaves the title
   vacant;
3. it has no cycle guard — added later, recorded in `feudal-hero-titles-phase-2-implementation-plan.md` §8;
4. it says succession mints Strong claims for passed-over children — it does not; derivation does.

Read §2 of the design doc for *why* the rule is shaped this way. Read this document, or the code, for what
it does.
