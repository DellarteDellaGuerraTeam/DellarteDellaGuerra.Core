# Phase 1 — hero-held titles

Implementation plan for phase 1 of the sequencing table in
[`feudal-hero-claims-and-succession-design.md`](feudal-hero-claims-and-succession-design.md) §6.

> **Scope.** `Title.HolderHeroId` **replaces** `HolderClanId`; `IGenealogy.GetClanOf`; the registry and
> `GetSuzerainUseCase` derive the clan; the serialiser's field 5 is repurposed and backfilled on load;
> `titles.config.xml` initial holders are resolved to clan leaders at bootstrap.
>
> **Intent: no behaviour change.** Every query answers exactly what it answered before. This phase changes
> the *representation* of a title holder, never the *policy* of who holds one.

---

## 0. The invariant that makes this safe

Phase 1 preserves one property, and everything below depends on it:

> **Every title holder is his clan's leader.**

All four write paths maintain it:

| Path | Holder written |
|---|---|
| Bootstrap (`BuildInitialTitles`) | `GetClanLeaderId(config's holderClanId)` |
| Old-save backfill (`OnGameLoaded`) | `clan.Leader.StringId` |
| Conquest (`AssignTitleUseCase`) | occupation only — holder untouched |
| Grant / transfer (`AssignTitleUseCase`) | `GetClanLeaderId(newOwner.Clan)` |

Because the invariant holds, `GetClanOf(HolderHeroId)` round-trips back to the same clan the old field
stored, and `GetClanLeaderId(GetClanOf(HolderHeroId))` round-trips back to the same hero. That identity is
what makes phase 1 behaviour-neutral — and phase 2 (succession) deliberately breaks it, which is exactly
why the representation has to change first.

**Grants are the one place this is a conscious simplification.** `OnSettlementOwnerChanged` hands us a
`Hero newOwner`, and the more accurate thing would be to make *that hero* the holder. Phase 1 resolves
through his clan's leader instead, so the invariant survives. Switching grants to the literal `newOwner` is
a one-line follow-up for phase 2, once non-leader holders are normal.

---

## 1. What is deliberately **not** in this phase

| Deferred | Why |
|---|---|
| `ITitleRepository.GetTitlesByHero` | §1's cost table lists it "for succession". Nothing reads it until phase 2 — adding it now is speculative |
| `HeroNode.FatherId` / `Age` | §6 puts them in phase 2 with `ExecuteSuccessionUseCase`, which is their only consumer |
| Hero-accurate claim derivation | Phase 3. `GenerateBloodClaimsUseCase` is translated mechanically here, not rewritten |
| `FeudalMapEntry.HolderHeroId` | The view model keeps its clan id, so `RenderFeudalMapUseCase` and `FeudalTitleNodeVM` are untouched. `BuildFeudalMapUseCase` is the resolution boundary |
| `EncyclopediaHeroPageMixin` showing the hero's *own* titles | It is labelled "Clan Titles:" and is honest as-is while the invariant holds. Phase 2/3 |
| `config/titles.config.xml` | Keeps its `holderClanId` attribute. No content migration, per §1 |

---

## 2. A simplification worth taking: `IsContested` stays on the record

§1 of the design doc says `Title.IsContested` "moves off the record and into whichever use case asks",
because `OccupantClanId != HolderClanId` no longer type-checks. Reading the write paths says otherwise.

`AssignTitleUseCase` **already** clears `OccupantClanId` to `null` the instant the occupant equals the
holder's clan:

* `ExecuteConquest`, holder-retakes branch → `WithOccupant(null, null)`
* `ExecuteTransfer`, holder-changes branch → `WithHolder(x).WithOccupant(null, null)`
* `ExecuteTransfer`, same-holder branch → `WithOccupant(null, null)`

So `OccupantClanId is not null` is already **equivalent** to today's expression for every reachable state —
the `!= HolderClanId` term never fires. Reduce it:

```csharp
public bool IsContested => OccupantClanId is not null;
```

This keeps five call sites (`FeudalTitleCampaignBehavior.OnMakePeace`, `BuildFeudalMapUseCase`, both
encyclopedia mixins, `AssignTitleUseCaseTests`) completely untouched, and keeps genealogy out of a pure
domain record.

**The one residual case, stated honestly:** if a holder hero changes clan (marriage, and later the cadet
spinoff) *into* the clan currently occupying his seat, the flag stays `true` until the next
`AssignTitleUseCase` call self-heals it. The consequence is a stale "(occupied by X)" suffix on an
encyclopedia label. That is acceptable; the alternative costs an `IGenealogy` dependency in five display
paths to fix a cosmetic edge case.

**Doc correction:** §1's "moves off the record" line should be amended when this lands.

---

## 3. Change surface

`HolderClanId` / `holderClanId` currently has **106 matches**. After this phase the surviving ones are all
legitimate: `config/titles.config.xml`'s attribute, `FeudalStructureParser`, `FeudalTitleNode`,
`AssignmentResult`, `FeudalMapEntry`, and PrivateWars' `PrizeAward.NewHolderClanId`.

### Stage A — ports and records *(Domain — will not compile alone; A–E are one atomic change)*

**A1. `Domain/Titles/Model/Title.cs`**
```diff
-    string? HolderClanId,
+    string? HolderHeroId,
     string? OccupantClanId = null,
     float? ContestedSinceDay = null)
 {
-    public Title WithHolder(string? holderClanId) => this with { HolderClanId = holderClanId };
+    public Title WithHolder(string? holderHeroId) => this with { HolderHeroId = holderHeroId };
     ...
-    public bool IsContested => OccupantClanId is not null && OccupantClanId != HolderClanId;
+    public bool IsContested => OccupantClanId is not null;
 }
```
Update the record's summary: the holder is a **hero**, whose clan is derived through
`IGenealogy.GetClanOf`; `OccupantClanId` stays a clan because de facto occupation derives from
`Settlement.OwnerClan`, which has no hero equivalent.

**A2. `Domain/Titles/Port/IGenealogy.cs`** — add one method:
```csharp
/// <summary>
/// The hero's current clan. Derived on every call, never cached: heroes change clans through
/// marriage and (later) the cadet spinoff, and a stored clan id would go stale silently.
/// </summary>
string? GetClanOf(string heroId);
```
Also drop the now-false clause from `GetClanLeaderId`'s comment — "The clan's current head~~, treated as
the living holder of its titles~~."

**A3. `Domain/Titles/Port/GenealogyExtensions.cs`** *(new, ~8 lines)* — four call sites need
"the holder clan of a possibly-null title", so extract it once rather than duplicating a null-dance:
```csharp
public static class GenealogyExtensions
{
    public static string? GetHolderClanOf(this IGenealogy genealogy, Title? title) =>
        title?.HolderHeroId is { } heroId ? genealogy.GetClanOf(heroId) : null;
}
```
Must be `public` — `InMemoryTitleRegistry` (Infrastructure) uses it too.

**A4. `Domain/Titles/Port/ITitleRepository.cs`** — **unchanged.** `GetTitlesByClan` keeps its signature;
only its implementation derives.

### Stage B — domain use cases

**B1. `GetSuzerainUseCase.cs`** — inject `IGenealogy`; line 26:
```diff
-    string? holderClanId = _titleRepository.GetTitle(parentTitleId)?.HolderClanId;
+    string? holderClanId = _genealogy.GetHolderClanOf(_titleRepository.GetTitle(parentTitleId));
```
Public shape stays `clanId -> clanId?`, so `FeudalHierarchyAdapter` and every party/settlement caller are
untouched. The existing "skip a parent held by the same clan and keep climbing" loop is what makes one clan
holding a duchy *and* a county beneath it work — no change needed.

**B2. `GetDirectVassalsUseCase.cs`** — inject `IGenealogy`;
`.Select(t => t.HolderClanId)` → `.Select(t => _genealogy.GetHolderClanOf(t))`.

**B3. `EvaluateClaimUseCase.cs`** — inject `IGenealogy`; the null-check on `title` folds into the helper:
```csharp
string? holderClanId = _genealogy.GetHolderClanOf(_titleRepository.GetTitle(claim.TitleId));
return holderClanId != null && holderClanId != claim.ClaimantClanId;
```

**B4. `AssignTitleUseCase.cs`** — the real work (17 sites). Inject `IGenealogy`.
* `Execute`: the conquest guard becomes `title.HolderHeroId is not null`.
* `ExecuteConquest`: resolve `string? holderClanId = _genealogy.GetHolderClanOf(title);` **once** at the
  top; it replaces both comparisons and feeds all three `AssignmentResult` arguments.
* `ExecuteTransfer`: `string? previousHolderClanId = _genealogy.GetHolderClanOf(title);` and the write
  becomes
  ```csharp
  _titleRepository.SaveTitle(title
      .WithHolder(newHolderClanId is null ? null : _genealogy.GetClanLeaderId(newHolderClanId))
      .WithOccupant(null, null));
  ```
* `Execute`'s signature, the log lines, the conquest-claim id (`{titleId}:{clanId}:conquest`) and
  `AssignmentResult` **all keep clan ids** — a result is a report rebuilt per call, so it cannot go stale.

**B5. `GenerateBloodClaimsUseCase.cs`** — already has `IGenealogy`. Mechanical translation only: resolve
once per title at the top of `Execute()` and thread the map through, rather than calling the port four
times per title.
```csharp
var holderClanByTitleId = titles.ToDictionary(
    title => title.Id, title => _genealogy.GetHolderClanOf(title));
```
`PrincipalTitleByClan` takes that map as a second parameter and stays static. The loop's four reads of
`title.HolderClanId` become `holderClanByTitleId[title.Id]`. **The claim set must not change** — under §0's
invariant, `GetClanLeaderId(GetClanOf(HolderHeroId))` returns the same anchor hero as before.

**B6. `BuildFeudalMapUseCase.cs`** — inject `IGenealogy`; `title?.HolderClanId` →
`_genealogy.GetHolderClanOf(title)`. `FeudalMapEntry` unchanged.

**Unchanged in Domain:** `AssignmentResult`, `FeudalMapEntry`, `RenderFeudalMapUseCase`, all PrivateWars.

### Stage C — Infrastructure

**C1. `InMemoryTitleRegistry.cs`** — constructor takes `IGenealogy`; one line changes:
```diff
-    return _titlesByTitleId.Values.Where(title => title.HolderClanId == clanId).ToList();
+    return _titlesByTitleId.Values.Where(title => _genealogy.GetHolderClanOf(title) == clanId).ToList();
```
The seat index and `Initialise`/`Snapshot` are untouched.

**C2. `CampaignGenealogy.cs`** — implement the new port method:
```csharp
public string? GetClanOf(string heroId)
    => MBObjectManager.Instance?.GetObject<Hero>(heroId)?.Clan?.StringId;
```
One dictionary lookup, two field reads, **zero allocation** — versus `GetHero`, which allocates a
`HeroNode` *and* a `List<string>`. This is the method that makes deriving cheap enough to do on the
`AreEnemies` path.

**C3. `XmlFeudalStructure.cs`** — `BuildInitialTitles()` takes the genealogy as a *parameter* rather than a
constructor field, so `FeudalStructureConfigReader` and the DI registration for the structure itself stay
untouched:
```csharp
public IReadOnlyList<Title> BuildInitialTitles(IGenealogy genealogy)
{
    ...
    string? holderHeroId = node.InitialHolderClanId is { } clanId
        ? genealogy.GetClanLeaderId(clanId)
        : null;
    titles.Add(new Title(node.TitleId, node.Name, node.Rank, node.SeatSettlementId, holderHeroId));
}
```
Two call sites only: the DI lambda and `InitialBloodClaimsIntegrationTests`. Update the doc comment to
"each held by the leader of its configured initial holder clan, if any."

**C4. `FeudalStructureParser.cs`, `Model/FeudalTitleNode.cs`** — **unchanged.** `holderClanId` stays the
authoring vocabulary; a clan is the right thing for a content author to name.

### Stage D — Application and Integration

**D1. `Titles/Spi/TitleStateSerialiser.cs`** — property rename only (`title.HolderClanId` →
`title.HolderHeroId` on serialise; field 4 on deserialise). **Field counts stay 5-or-7.**

**D2. `Titles/Api/Campaign/FeudalTitleCampaignBehavior.cs`** — **unchanged.** An earlier revision of this
plan backfilled old saves here, probing each persisted field-5 id through `MBObjectManager` to tell a
surviving clan id from a hero id and swapping in the clan's leader. That was dropped on the call that save
compatibility is not wanted for this phase: the feudal system is still pre-release, so a save written
against the clan-held format is simply not supported. Removing it also removes the only production path in
this phase with no automated coverage.

Field 5 of a pre-phase-1 save therefore loads as a clan id in a hero-id slot, and those titles resolve to no
living holder. That is the accepted cost — start a new campaign.

**D3. `Titles/Api/FeudalTitleKingdoms.cs`** — resolve through the hero:
```diff
-    string? holderClanId = (current is not null ? titles.GetTitle(current)?.HolderClanId : null)
-                           ?? titles.GetTitle(titleId)?.HolderClanId;
-    return FindClan(holderClanId)?.Kingdom;
+    string? holderHeroId = (current is not null ? titles.GetTitle(current)?.HolderHeroId : null)
+                           ?? titles.GetTitle(titleId)?.HolderHeroId;
+    return HolderClan(holderHeroId)?.Kingdom;
```
```csharp
private static Clan? HolderClan(string? heroId) =>
    heroId is not null && MBObjectManager.Instance?.GetObject<Hero>(heroId)?.Clan is { IsEliminated: false } clan
        ? clan
        : null;
```
`FindClan` has **exactly one caller** — line 33, in this file. This change orphans it, so remove it
(AGENTS.md §3: clean up the orphans *your* change creates).

**D4. `Integration/DI/DadgServiceContainer.cs`** — **one line**, the bootstrap lambda:
```diff
-        () => sp.GetRequiredService<XmlFeudalStructure>().BuildInitialTitles()));
+        () => sp.GetRequiredService<XmlFeudalStructure>()
+                .BuildInitialTitles(sp.GetRequiredService<IGenealogy>())));
```
Every other new constructor dependency (`IGenealogy` into five use cases and the registry) resolves
automatically — `IGenealogy` is already registered, and MS.DI resolves lazily so its position in the file
does not matter.

**Unchanged in Integration:** `FeudalStateStoreAdapter`, `IFeudalStateStore`, `FeudalTitleNodeVM`, both
encyclopedia mixins, `FeudalHierarchyAdapter`.

### Stage E — tests

**E1. `Domain.Tests/Titles/Fakes.cs`**
* `FakeGenealogy` — add `GetClanOf(heroId)` (`_heroes` lookup → `ClanId`).
* `FakeTitleRepository` — needs a genealogy for `GetTitlesByClan`. Give it an initialised property rather
  than a constructor parameter, so the ~12 existing `new FakeTitleRepository(t1, t2)` sites still compile
  and only the suzerain/vassal tests set it:
  ```csharp
  public FakeGenealogy Genealogy { get; init; } = new();
  public IReadOnlyList<Title> GetTitlesByClan(string clanId) =>
      _titles.Values.Where(title => Genealogy.GetHolderClanOf(title) == clanId).ToList();
  ```

**E2. `AssignTitleUseCaseTests.cs`** (12 sites) — the largest churn. Titles are constructed with a **hero
id**, and the fake genealogy maps that hero → clan and that clan → leader. Assertions on
`AssignmentResult.PreviousHolderClanId` / `NewHolderClanId` and `title.IsContested` stay as they are.

**E3. `BuildFeudalMapUseCaseTests.cs`** (4 sites) — use-case constructor gains a genealogy; entries still
assert on `HolderClanId`.

**E4. `GenerateBloodClaimsUseCaseTests.cs`** (3 sites) — holder becomes a hero id; the existing
`FakeGenealogy.WithLeader` calls already supply the clan↔leader mapping.

**E5. `Infrastructure.Tests`** — needs a minimal local `IGenealogy` fake (`DadgXmlGenealogy` is
content-backed and too heavy for unit tests):
* `InMemoryTitleRegistryTests` — registry constructor; `YorkDuchy` takes a hero id.
* `XmlFeudalStructureTests` — `BuildInitialTitles(genealogy)`; `kingdom.HolderClanId == "clan_lancaster"`
  becomes `HolderHeroId == <that clan's leader>`; the `barony.HolderClanId is null` assertion becomes
  `HolderHeroId is null` and still holds (no `holderClanId` in the sample XML).
* `DadgXmlGenealogy` — add `GetClanOf` (it already parses `faction` into `HeroNode.ClanId`).

**E6. `InitialBloodClaimsIntegrationTests.cs`** — the behaviour-neutrality proof. Wire
`new InMemoryTitleRegistry(genealogy)` and `BuildInitialTitles(genealogy)`; rewrite the five
`HolderClanId` reads as `genealogy.GetClanOf(title.HolderHeroId!)`. **Every number was expected to stay the
same:** 97 titles, 53 distinct holder clans, 442 heroes, 45 claims (5 Strong / 40 Weak), 30 claimant clans,
24 titles claimed, and the full `ExpectedClaims` string. A content defect this phase exposed moved four of
them, and fixing that content moved one more — see §7 for both, and for the re-baselined figures. The existing `Assert.All(... leaderId is not empty ...)` becomes the statement that
bootstrap resolution is total over real content — it already passes today, which is what proves no title
goes vacant when holders are resolved from `config/titles.config.xml` through the clan leader.

**Unchanged:** `FeudalStructureParserTests` (parser untouched — its six matches are on
`InitialHolderClanId`/`holderClanId`, both of which survive) and `PrivateWars/ResolveWarUseCaseTests`
(its match is `PrizeAward.NewHolderClanId`, which stays clan-keyed).

---

## 4. Execution order and verification

Stages A–E are **one atomic change** — renaming a record field breaks the build until the last call site is
updated, so there is no green intermediate state. Author in A→E order (ports, then domain, then
infrastructure, then application, then tests) and verify at the boundaries below.

```
1. Stages A–E authored in order        → verify: dotnet build succeeds; zero `HolderClanId` references
                                                  outside the six legitimate survivors (§3)
2. dotnet test                          → verify: all green, and InitialBloodClaimsIntegrationTests'
                                                  numbers are byte-identical (97 / 53 / 442 / 45 / 5 / 40)
                                                  — re-baselined to 97 / 53 / 442 / 42 / 1 / 41 after a
                                                  content fix, see §7
3. Build the module; start a NEW game   → verify: feudal map shows the same holders as before the change
4. Save from that session, reload it    → verify: holders identical
5. Trigger a siege capture and a fief grant
                                        → verify: conquest sets OccupantClanId and leaves the holder;
                                                  the grant moves the holder to the new owner's leader
```

Saves written before this phase are **not** supported (see D2) — step 3 starts a new campaign deliberately.
With the backfill gone, every line of production code this phase touches is covered by steps 1–2; the
runtime steps confirm wiring, not logic.

---

## 5. Risk register

| Risk | Likelihood | Mitigation |
|---|---|---|
| A configured `holderClanId` names a clan with no leader → title silently vacant | Low | `InitialBloodClaimsIntegrationTests` already asserts a non-empty leader for all 53 holder clans on real content; keep that assertion |
| A save written before this phase loads its clan ids into `HolderHeroId` and every title reads as vacant | Certain | Accepted, not mitigated — save compatibility is explicitly out of scope for this phase (§3 D2). Start a new campaign |
| `GetTitlesByClan` gets hot enough for the extra port hop to matter | Low | It is already an O(titles) scan over 97 entries; `GetClanOf` is allocation-free. If it ever bites, index on `heroId` — an index on the *authoritative* key, which cannot go stale |
| Claim set shifts despite the "no behaviour change" intent | Medium | Step 2's `ExpectedClaims` string is an exact-match assertion over real 1471 content — any drift fails the build |

---

## 6. Doc corrections this phase produces

* §1: "`Title.IsContested` … moves off the record and into whichever use case asks" — superseded by §2
  above; it stays on the record as `OccupantClanId is not null`.
* §6, phase 1 row: `HeroNode.FatherId`/`Age` are correctly listed under phase 2; an earlier reading placed
  them here. Nothing in phase 1 reads them.

---

## 7. Outcome: §0's invariant was false in the shipped content — since corrected

Stages A–E landed; the solution builds clean and `Domain.Tests` passes 191/191. The one failure is the
neutrality proof itself, `InitialBloodClaimsIntegrationTests.Real1471ContentProducesTheExpectedInitialClaims`,
and it is a true positive.

**The invariant.** §0 assumed every title holder is his clan's leader, so that `GetClanOf(HolderHeroId)`
round-trips to the old stored clan. Cross-checking all 91 clans in `dadg_clans.xml` against
`dadg_heroes.xml`, exactly one breaks it:

| clan | `owner` | that hero's `faction` |
|---|---|---|
| `clan_vernon` | `dadg_lord_20_1` | **`clan_talbot`** |

The naming convention makes the cause plain: `dadg_dead_lord_20_1/2` are `clan_vernon`, but all five
*living* clan-20 heroes (`dadg_lord_20_1 … 20_5`) carry `faction="Faction.clan_talbot"`. `clan_talbot` is
separately and correctly owned by `dadg_lord_19_1`. `clan_vernon` is therefore left owning a Talbot and
holding zero living members — a content defect, not a modelling one.

**Measured blast radius.** Diffing the full projection before and after, the change loses exactly four
claims and alters nothing else; the other 41 are byte-identical:

```
LOST  barony_beeston|dadg_lord_20_1|clan_talbot|Strong
LOST  barony_beeston|dadg_lord_20_3|clan_talbot|Strong
LOST  barony_beeston|dadg_lord_20_4|clan_talbot|Strong
LOST  barony_beeston|dadg_lord_20_5|clan_talbot|Strong
```

Totals move 45 → 41 claims, 5 → 1 Strong, 53 → 52 distinct holder clans (Beeston's holder folds from
`clan_vernon` into `clan_talbot`, which already held titles).

**Why the new behaviour is the correct one.** `barony_beeston` is held by `clan_vernon`, whose leader is
`dadg_lord_20_1`. The first lost line is thus a **Strong claim by the holder against the title he already
holds**. The old code compared claimant *clan* to holder *clan*; the data inconsistency made those differ,
so the holder claimed from himself. Hero-level identity makes that impossible to express. Phase 1 did not
introduce a regression here — it removed one the clan-level representation could not see.

**Predicted against corrected data.** Re-running with `dadg_lord_20_1 … 20_5` reassigned to `clan_vernon`
(a scratch copy at the time) restored 53 holder clans and dropped the same four claims, adding one further
claim that follows from `dadg_lord_20_2` changing clans. No dataset reproduces the *old* expectation,
because that expectation encodes the inconsistency: its lines require those heroes to be `clan_talbot`
*and* Beeston's holder not to be.

**Resolution taken.** The content was corrected at source: all five living clan-20 heroes now carry
`faction="Faction.clan_vernon"`, so `clan_vernon` owns its own members. Re-running the 91-clan cross-check
against the shipped `dadg_clans.xml`/`dadg_heroes.xml` reports **zero violations** — §0's invariant now
holds in the data as well as in the design.

`ExpectedClaims` was re-baselined against the corrected content and matches the prediction above exactly:
**97 titles, 53 holder clans, 442 heroes, 42 claims (1 Strong / 41 Weak), 31 claimant clans, 23 claimed
titles.** Against the pre-phase-1 baseline of 45 that is four `barony_beeston` Strong claims removed — the
claimants are the holder's own clansmen now, so they would be absent under *any* self-consistent dataset —
and `county_hallamshire|dadg_lord_20_2|clan_vernon|Weak` gained, which follows from the content fix rather
than from this phase. `barony_beeston` stays claim-free. The `<remarks>` block that documented the
inconsistency was removed with it.

Net effect of the phase on real content: the sole behaviour change is the loss of a Strong claim by a
holder against a title he already holds — a bug the clan-level representation could not express.
