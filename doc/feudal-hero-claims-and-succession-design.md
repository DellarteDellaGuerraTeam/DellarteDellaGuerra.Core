# Hero-Level Claims, Succession, and Intra-Clan Wars — Design Analysis

Status: analysis only. No code written. 2026-09-05, branch `feature/add-internal-faction-wars-v2`.
**Revision 2** — all five open decisions resolved; `Title` now *drops* `HolderClanId` rather than caching it (§1).

Answers four requirements:

1. Claims held by **characters**, not clans.
2. On a clan leader's death, the **eldest son inherits all the titles**.
3. A **daily evaluation** picking opportune moments to press a claim (strength comparison, ally/enemy state).
4. **Brothers within one clan** can press claims against each other, recruiting allies and traitorous
   vassals, with a two-sided strength comparison; the claimant is **spun off into a derived clan** so the
   clan-keyed private-war substrate keeps working.

Engine facts below were verified against the 1.4.6 decompiled source (game runs v1.4.8 build 119303).

---

## 0. Executive summary

The migration is smaller than it looks, because two of the three prerequisites already landed.

| | State |
|---|---|
| `Claim.ClaimantHeroId` | **Already exists** (optional, denormalised alongside `ClaimantClanId`) |
| De jure / de facto title split | **Already landed** (`HolderClanId` vs `OccupantClanId` + `ContestedSinceDay`) |
| `Title.HolderHeroId` | Missing — **replaces** `HolderClanId`, which is derived from here on |
| Succession on death | Missing — `OnHeroKilled` only re-derives blood claims |
| Daily declaration driver | Missing — nothing in DADG calls `IPrivateWarsApi.DeclareWar` except a debug cheat |
| Per-war side overrides ("traitorous vassals") | Missing — `WarSideResolver` derives sides *purely* from the suzerain chain |

Two findings dominate the cost estimate:

- **Requirement 4's claim half is a one-line change.** `GenerateBloodClaimsUseCase` currently skips any
  claimant in the holder's own clan (`if (hero.ClanId == title.HolderClanId) continue;`). Once titles carry
  a holder *hero*, that becomes `if (hero.Id == title.HolderHeroId) continue;` — you cannot claim what you
  personally hold — and brother-vs-brother claims exist immediately.
- **"Traitorous vassals" is the one thing the current model genuinely cannot express.** `WarSideResolver`
  walks up the suzerain chain to the nearest belligerent ancestor; there is no way to say "vassal V fights
  for the claimant against its own liege". This needs a small, honest addition to the PrivateWars submodule
  (§4.4), not a workaround in DADG.

---

## 1. Titles name a hero; the clan is derived

Three separable scopes hide behind "character-basis". Only the first two are required.

**(a) Claims are hero-owned.** Nearly free — `Claim.ClaimantHeroId` already exists; what is missing is that
nothing *reads* it, and `InMemoryClaimRegistry.GetClaimsFor` still filters on `ClaimantClanId`.

**(b) Titles are hero-held.** The real change, and **not optional**:

- "when a clan leader dies the eldest son inherits all the titles" is a no-op under clan-held titles — the
  clan still holds them, and vanilla's `ChangeClanLeaderAction` already moves `Clan.Leader`.
- "especially if one of their brother **has the title**" states hero-held titles outright.

Per decision 2, this applies at **every rank** — no split rule between baronies and duchies.

**(c) Settlements, elections, influence, `MapFaction` become hero-anchored.** Out of scope. Bannerlord is
clan-anchored here and fighting it is the expensive path. The de jure/de facto split already models the
mismatch: a hero holds the *dignity*, a clan *occupies* the seat.

### Decision: `HolderClanId` is deleted, not kept as a cache

Revision 1 proposed keeping `HolderClanId` as a denormalised cache of the holder hero's clan, mirroring what
`Claim` does with `ClaimantHeroId` / `ClaimantClanId`. That recommendation does not survive contact with the
code, and it is withdrawn.

**Why deriving is correct, not merely acceptable.** This design makes hero→clan changes a *first-class,
frequent* operation. The cadet spinoff sets `claimant.Clan = cadet`; reabsorption on defeat sets it back;
and vanilla's `MarriageAction` already moves heroes between clans with no knowledge of DADG at all. A stored
`HolderClanId` must be re-synced on every one of those paths. Miss one and `AreEnemies` silently resolves
the wrong side — a private war just quietly stops working, with no exception and no log line. A derived clan
**cannot** be wrong.

The marriage case is decisive. With decision 1 (daughters inherit when there are no sons), a title-holding
daughter who marries carries her title into her husband's house. Derived, that is automatic and
historically correct — it is how the Nevilles got Warwick. Stored, it is a stale pointer, and the title
wrongly stays with her birth clan. §2.3 covers the consequences, which are a feature.

**The performance objection dissolves under inspection.** `CampaignGenealogy.GetHero` allocates a `HeroNode`
*and* a fresh `List<string>` of child ids on every call, so deriving through the port *as it stands today*
really would hurt on the `AreEnemies` path. That is a defect of the port's shape, not of deriving. One
method fixes it:

```csharp
// IGenealogy — one dictionary lookup, two field reads, zero allocation.
string? GetClanOf(string heroId);

// CampaignGenealogy
=> MBObjectManager.Instance?.GetObject<Hero>(heroId)?.Clan?.StringId;
```

`GetTitlesByClan` is *already* an O(titles) linear scan (`_titlesByTitleId.Values.Where(...)`). Deriving
multiplies its constant, not its order. If it ever bites, `InMemoryTitleRegistry` can hold a
`Dictionary<heroId, List<titleId>>` index — an index on the **authoritative** key, which by construction
cannot go stale the way a cached clan id can.

### The record

```csharp
public record Title(
    string Id, string Name, TitleRank Rank, string SeatSettlementId,
    string? HolderHeroId,                 // was HolderClanId
    string? OccupantClanId = null,
    float? ContestedSinceDay = null);
```

**`OccupantClanId` stays a clan, deliberately.** De facto occupation derives from `Settlement.OwnerClan`,
which is clan-level in the engine and has no hero equivalent. The asymmetry is the point: a dignity is
personal, an occupation is territorial. The one genuinely awkward consequence: `Title.IsContested` becomes
`OccupantClanId is not null && OccupantClanId != GetClanOf(HolderHeroId)`, which needs genealogy and so
moves off the record and into whichever use case asks.

### What this costs, honestly

| Site | Change |
|---|---|
| `IGenealogy` | New `GetClanOf(heroId)`; `HeroNode` gains `FatherId` and `Age` (§2) |
| `ITitleRepository` | `GetTitlesByClan` stays (still needed by `GetSuzerainUseCase`); **add** `GetTitlesByHero` for succession |
| `InMemoryTitleRegistry` | Takes `IGenealogy`; `GetTitlesByClan` filters on `GetClanOf(t.HolderHeroId)`. Today it has no campaign dependency at all — but `Infrastructure` already references the TaleWorlds assemblies, and `Infrastructure.Tests` already has a `DadgXmlGenealogy` double, so it stays testable |
| `GetSuzerainUseCase` | Takes `IGenealogy`; line 26 becomes `GetClanOf(GetTitle(parentTitleId)?.HolderHeroId)`. Its public shape stays `clanId -> clanId?` — parties and settlements are clan-keyed, so `FeudalHierarchyAdapter` is untouched |
| `AssignTitleUseCase` | The real work. Conquest is genuinely clan-level, so a conquering clan's **leader** becomes the new holder. `AssignmentResult` keeps clan ids — it is a report, rebuilt each call, so it cannot go stale |
| `TitleStateSerialiser` | Field 5 changes *meaning* rather than a new field appearing — see §5 |
| `FeudalStructureParser` | `config/titles.config.xml` keeps its `holderClanId` attribute; resolve clan → leader at bootstrap. Authoring initial holders by clan is the right ergonomics. **No content migration** |
| `FeudalTitleKingdoms`, `BuildFeudalMapUseCase`, `RenderFeudalMapUseCase` | Display paths; resolve through genealogy. `FeudalMapEntry` may carry both — it is a view model rebuilt each render, so no staleness risk |

Net: the record **shrinks** instead of growing, and an entire class of bug disappears.

**A regression that does not happen:** with hero-held titles, one clan can hold a duchy and a county beneath
it (two brothers, pre-spinoff). `GetSuzerainUseCase` already handles this — its walk skips any parent title
whose holder resolves to the same clan and keeps climbing.

**Dead holders still resolve.** Bannerlord keeps `Hero.Clan` populated after death —
`CampaignGenealogy.GetDeceasedClanMemberIds` relies on exactly that
(`DeadOrDisabledHeroes.Where(h => h.Clan == clan)`). So a title whose holder has died but whose succession
has not yet run still derives a clan and does not vanish. That is what makes phase 1 safe to ship before
phase 2.

---

## 2. Succession — male-preference primogeniture (decision 1)

### Trigger

`FeudalTitleCampaignBehavior.OnHeroKilled` today is one line (`_generateBloodClaimsUseCase.Execute()`).
Succession runs **before** that re-derivation, so passed-over heirs get claims against the new holder.

**Divergence from the literal wording, flagged deliberately:** the requirement says *clan leader*, but once
titles are hero-held a non-leader brother can hold a barony and die too. Trigger on **title-holder death**,
which subsumes clan-leader death. The literal version would silently orphan every title held by a
non-leader.

**Ordering hazard:** vanilla's `ChangeClanLeaderAction` also runs on death, and event order is not
guaranteed. Resolve heirs purely from genealogy and never read `Clan.Leader` during succession, except as
the last-resort backstop.

### 2.1 The ordering

```
Heir(deceased):
    # 1. Direct line — sons by age, eldest first
    for child in deceased.Children, male, ordered by Age desc:
        if child.IsAlive:                        return child
        # [representation] elif HasLivingDescendant(child): return Heir(child)

    # 2. Direct line — daughters by age, eldest first
    for child in deceased.Children, female, ordered by Age desc:
        if child.IsAlive:                        return child

    # 3. Collateral — the deceased's brothers, then sisters, eldest first
    for sibling in deceased.Father.Children except deceased, male first, by Age desc:
        if sibling.IsAlive:                      return sibling
        # [representation] else:                 return Heir(sibling)

    # 4. Backstop — never orphan a title
    return leader of the clan the deceased belonged to
```

Male-preference means a daughter inherits only when **no** living son exists. Siblings are reachable as
`Father.ChildIds`, so `HeroNode` needs `FatherId` and `Age` and nothing more:

```csharp
public record HeroNode(
    string Id, bool IsFemale, bool IsAlive, string? ClanId, IReadOnlyList<string> ChildIds,
    string? FatherId = null, float Age = 0f);
```

`Age` as a float keeps the domain free of `CampaignTime`. All the underlying fields exist on `Hero` in
1.4.6 — `Father`, `Mother`, `Siblings`, `BirthDay`, `Age`. `IsFemale` is already on `HeroNode`, so the
daughter branch needs no new port surface at all.

Every passed-over living child is minted a `ClaimStrength.Strong` / `ClaimOrigin.Inheritance` claim. That
clause is what wires requirement 2 into requirement 4: **succession is where brother-against-brother claims
are born.**

### 2.2 Representation — one `else` branch, recommended

Representation is the rule that a predeceased eldest son's own son inherits before his uncle. It is
bracketed above because it was not part of the decision, and turning it on is literally two `else` branches
in the ordering function — no structural change.

Recommend turning it on. It is not a flourish here: representation *through a female line* is the entire
Yorkist legal claim — Richard of York descended from Lionel of Antwerp through his daughter, which is why
his claim outranked Lancaster's. Without it, the mod's succession rules cannot express the setting's own
central dispute. But it is a call to make explicitly, and the ordering function is the only thing affected.

### 2.3 Consequence of daughters inheriting: titles marry out

Bannerlord's `MarriageAction` moves a bride into her husband's clan. With derived holder clans, a woman who
inherits a title and then marries **carries that title into her husband's house**, automatically. Her birth
clan loses it; her husband's clan gains a dignity and, through `GetSuzerainUseCase`, possibly a new place in
the hierarchy.

This is correct and period-accurate — it is how half the great English estates changed hands. Recommend
accepting it. The alternative (pinning a female holder's title to her birth clan) is a deliberate
ahistorical restriction and needs the stored-clan denormalisation back, with all its staleness.

Two interactions worth knowing:

- Combined with decision 5 (intra-kingdom only), a title that marries into another kingdom's clan puts that
  title permanently **out of reach** of its original family's claims. Historically apt, but it means some
  claims become dead letters. They cost nothing — the daily evaluator skips them on the kingdom gate.
- `GenerateBloodClaimsUseCase` should now mint claims for daughters as well, ranked below sons, since they
  are genuinely in the line of succession.

### 2.4 A hack this deletes

`GenerateBloodClaimsUseCase` currently uses `PrincipalTitleByClan` — the highest-rank title per clan — with
the comment "The campaign data records no per-hero title history". Once `HolderHeroId` exists that is no
longer true, and the approximation can be replaced by a real per-hero lookup. A net simplification of
existing code, not an addition. The same applies to `IGenealogy`'s doc comments, which currently assert that
the clan head is "the living holder of its titles" — no longer the case.

---

## 3. Daily claim evaluation

### Where it goes

`PrivateWarCampaignBehavior.OnDailyTick` already exists in the submodule, but it scores and resolves
*existing* wars and must stay ignorant of titles and claims — PrivateWars is generic; DADG owns the feudal
semantics. So: a **new DADG-side** `ClaimPressureCampaignBehavior` on `CampaignEvents.DailyTickEvent`. Not
folded into `FeudalTitleCampaignBehavior`, which is already the lifecycle/persistence owner and has none of
the required dependencies.

**Note:** the design doc's original AI trigger hung off the petition-deny path (`FeudalPetitionDecision` /
`SurgeTensionOnDenial`), which has since been deleted from the codebase along with levies and tension
(commits `bc12347`, `589add9`, `74f65b0`). That trigger design is orphaned; this replaces it.

### Shape

Keep the scoring pure. The behaviour builds a snapshot, the domain scores it, the behaviour acts.

```
daily:
  for each claim not already backing an active private war:
      if claimant or holder is the player     -> skip     (decision 3)
      if either kingdom is null               -> skip     (decision 5)
      if claimant.Kingdom != holder.Kingdom    -> skip     (decision 5)
      if no main goal exists                  -> skip     (hard precondition, design §18.D)
      opportunity = snapshot(claim)                       (infrastructure adapter)
      score       = EvaluatePressClaimUseCase(opportunity)(pure)
      if score >= threshold and cooldown elapsed and random gate passes:
          declare
```

### Gates and terms, all backed by verified APIs

| Term | Source | Effect |
|---|---|---|
| Player not involved | `Clan.PlayerClan` on either side | **hard gate** (decision 3) |
| Same kingdom | `Clan.Kingdom` on both sides, non-null | **hard gate** (decision 5) |
| Main goal exists | defender holds a de jure settlement of the title | **hard precondition** (design §18.D) |
| Claimant free | claimant not already a belligerent | **hard gate** (design §9: one war per unordered clan pair) |
| Strength ratio | `Clan.CurrentTotalStrength` summed over each side's subtree | primary score; attack at ≈1.25:1 or better |
| Defender distracted | defender's kingdom at war, or defender already a private-war belligerent | large bonus — this is the "state of allies and enemies" |
| Claim strength | `DeJure` > `Strong` > `Weak` | scales appetite |
| Relation | `Hero.GetRelation(claimant, holder)` | hatred pushes |

The main-goal precondition constrains the *defender*: a landless title-holder cannot be attacked at all. A
newly spun-off landless claimant is fine as an attacker, but a brother who holds a dignity with no seat
cannot be warred against.

`EvaluateClaimUseCase` today is three lines (`holder != null && holder != claimant`). It is the natural seat
for this, though a separate `EvaluatePressClaimUseCase` keeps the existing boolean validity check distinct
from the new appetite score.

**Cooldown is essential.** Without a per-claim `NextEvaluationDay`, a claim that scores just under threshold
is re-evaluated 365 times a year and fires the instant noise crosses the line. Persist it, or accept
re-evaluation churn.

**Ordering caveat:** both behaviours listen on `DailyTickEvent`; registration order in
`SubModule.InitializeGameStarter` decides who runs first. Harmless (a war declared this tick scores ~0), but
worth knowing.

### On the kingdom gate (decision 5)

Private wars are same-`MapFaction` by construction — hostility is manufactured *inside* one kingdom, with no
`StanceLink` and no temporary faction. Cross-kingdom claim pressing would need real `StanceLink` wars, which
is a different mechanism rather than a bigger version of this one. The gate is therefore a two-line check,
not a compromise: it states what the substrate already enforces.

### On excluding the player (decision 3)

Excluding the player as *claimant* is what avoids the work — no UI, no consent flow, no notifications.
Excluding the player as *target* is the conservative default taken here, but it is worth knowing it is
nearly free to enable later: the submodule's patch layer already handles a player belligerent end-to-end
(`PlayerCanAttackPrivateWarRivalPatch`, `PlayerIsEnemyTagPatch`, `PlayerEncounterSetupFieldsPatch`,
`DadgPlayerCaptivityCampaignBehavior`, `PrivateWarEncounterMenuOptions`). Flipping the *target* gate is one
condition; flipping the *claimant* gate is a feature.

---

## 4. Intra-clan wars and the cadet spinoff

### 4.1 The claim (one line)

`if (hero.ClanId == title.HolderClanId) continue;` becomes `if (hero.Id == title.HolderHeroId) continue;` —
you cannot claim what you personally hold. Brothers and sisters of a title-holder become valid claimants
immediately.

### 4.2 Why the spinoff is genuinely required

The instinct behind the requirement is right, and the code confirms it. `IPrivateWarsApi.DeclareWar(attackerClanId,
defenderClanId, …)` is clan-keyed; `PrivateWar` stores `AttackerPrincipalClanId` /
`DefenderPrincipalClanId`; `WarSideResolver` resolves both principals by clan id; `AreEnemies` short-circuits
on identical ids. Two brothers in one clan cannot be put on opposite sides of anything.

Every one of the ~25 `AreEnemies` call sites enters from a `Clan` — `MobileParty.ActualClan`,
`Settlement.OwnerClan`, `Hero.MainHero.Clan`, `prisoner.Clan`. That is a fact about Bannerlord, not about
DADG's design: parties and fiefs belong to clans. Making PrivateWars hero-aware would mean touching the
record, the resolver, the registry, the serialiser, every model override and every Harmony patch, and it
would still hit a clan at the boundary. **The spinoff avoids all of it.**

### 4.3 The spinoff recipe (verified)

Vanilla already does exactly this: `Clan.CreateSettlementRebelClan` spins a runtime clan off during a
settlement rebellion. That is the precedent to copy, and it is on the well-trodden path.

```csharp
var cadet = Clan.CreateClan(parent.StringId + "_cadet");   // auto-uniquified via FindNextUniqueStringId
cadet.ChangeClanName(name, informalName);
cadet.Culture = parent.Culture;
cadet.Banner  = Banner.CreateOneColoredBannerWithOneIcon(/* derived from parent */);
cadet.Color = parent.Color; cadet.Color2 = parent.Color2;
cadet.Tier  = Math.Max(1, parent.Tier - 1);
cadet.SetInitialHomeSettlement(seat ?? parent.HomeSettlement);

claimant.Clan = cadet;                                      // public setter
foreach (var child in claimant.Children) child.Clan = cadet;// vanilla does this in MarriageAction
cadet.SetLeader(claimant);
cadet.IsNoble = true;

ChangeKingdomAction.ApplyByJoinToKingdom(cadet, parent.Kingdom, default, showNotification: false);
CampaignEventDispatcher.Instance.OnClanCreated(cadet, isCompanion: false);
```

**`ApplyByJoinToKingdom` is the load-bearing step.** Private wars work by manufacturing hostility *inside one
`MapFaction`* — no `StanceLink`, no temporary kingdom. If the cadet clan does not join the same kingdom, every
model override and patch in the submodule stops applying and the war becomes inert. With decision 5 in
force, this is also what keeps the war legal under the intra-kingdom gate. It is the single easiest thing to
get wrong.

Verified API surface: `Clan.CreateClan(string)` public static; `Clan.SetLeader(Hero)` public (called from the
`StoryMode` assembly); `Hero.Clan` public setter (assigned from `StoryMode`, `CampaignSystem.Issues`,
`CampaignSystem.Actions`); `Clan.SetInitialHomeSettlement`, `Clan.Tier`, `Clan.IsNoble` all public.

**With derived holder clans, the spinoff needs no title bookkeeping at all.** Setting `claimant.Clan = cadet`
moves every title the claimant holds into the cadet clan in the same instant, because `GetTitlesByClan`
resolves through `Hero.Clan`. Under the revision-1 cache design this step would have needed an explicit
re-sync pass over the claimant's titles — a further argument for §1.

### 4.4 Traitorous vassals — the one real gap

`WarSideResolver` walks *up* the suzerain chain to the nearest belligerent principal. Sides are therefore a
pure function of the feudal hierarchy: **every vassal of the title-holder automatically fights for the
title-holder.** Defection is inexpressible.

Two options:

- **(a) Explicit per-war supporter sets.** Add `AttackerSupporters` / `DefenderSupporters` to `PrivateWar`;
  `WarSideResolver` consults them *before* the chain walk. ~15 lines in the resolver, one field on the
  record, one appended serialiser field.
- **(b) Re-parent the defector's suzerain link in DADG.** Rejected: it rewrites the permanent feudal map to
  encode a temporary allegiance and would persist wrongly after the war ends.

**Recommend (a).** A defection is a per-war fact, not a change to the hierarchy. It also serves "the claimant
would need to find ally support" with the same mechanism — an ally is a supporter who is not a vassal of
either principal. Supporters are a generic private-war concept, so this belongs in the submodule cleanly
rather than leaking DADG feudal semantics into it.

### 4.5 The two-sided strength comparison

Exactly as requested:

```
AttackerStrength = cadet.CurrentTotalStrength
                 + Σ supporters (allies + defecting vassals) and their subtrees
DefenderStrength = holderClan.CurrentTotalStrength
                 + Σ vassal subtree MINUS defectors
                 + Σ its own supporters
```

The subtree walk is `GetDirectVassalsUseCase` applied transitively. Note it is currently an O(titles) inverse
scan *per call*; transitively that is O(titles × depth), and with §1 each element also costs a genealogy
lookup. At ~50 titles this is free, but if the daily evaluator runs it per claim per day, build the inverse
map once per tick.

### 4.6 Who defects

A pure domain use case, `SolicitSupportUseCase(claimant, holder, candidates) -> supporters`. Inputs already
available: `Hero.GetRelation` between the claimant and each candidate leader, claim strength, and a
strength-bandwagon term. Threshold and done — resist building an opinion system.

### 4.7 Aftermath

- **Claimant wins** → `AssignTitleUseCase` transfers the title to the claimant *hero*; the seat follows to
  the cadet clan, which becomes a permanent house. This is *canonical* for the setting: DADG map data
  already models cadet branches as separate clans (`clan_neville_of_raby` / `_of_middleham` /
  `_of_bergavenny`, House Grey, House Scrope).
- **Claimant loses** → **reabsorb** (decision 4): `claimant.Clan = parent`, then
  `DestroyClanAction.Apply(cadet)`, unless the claimant died. Any title the claimant still personally holds
  follows them back into the parent clan automatically under §1 — no title bookkeeping in the reabsorption
  path either. Without reabsorption, a century of failed claims accumulates dead landless clans in the clan
  list and the encyclopedia.

---

## 5. Save compatibility

`TitleStateSerialiser` already tolerates variable field counts (5 or 7 for titles, 5 or 6 for claims), and
`PrivateWarStateSerialiser` does the same (11 or 12, appended `GoalLastTakenDay` defaulting to `StartDay`).
That established pattern covers two of the three changes. Dropping `HolderClanId` makes the third the one
place that is **not** a free append: title field 5 changes *meaning* from a clan id to a hero id, rather
than a new field appearing at the end.

| Record | Field | Position | Old-save behaviour |
|---|---|---|---|
| `Title` | `HolderHeroId` | 5 (**repurposed**) | **unsupported** — see below |
| `PrivateWar` | supporter sets | 13 (appended) | absent → empty → pure chain-walk sides, i.e. today's behaviour |
| `Claim` | `NextEvaluationDay` (if persisted) | 7 (appended) | absent → 0 → evaluated immediately |

**Not backfilled — decided during phase 1.** A version-flag-free backfill was designed and implemented:
probe field 5, and where it resolves as a `Clan` rather than a `Hero`, swap in that clan's `Leader.StringId`
(object ids do not collide across types, so the probe is unambiguous and idempotent). It was then removed on
the call that save compatibility is not wanted while the feudal system is pre-release.

Consequence: a save written before phase 1 loads clan ids into `HolderHeroId`, and every title in it reads
as vacant. Start a new campaign. The title field count is still 5-or-7 — the format never changed, only the
referent — so if compatibility is ever wanted, the probe above is the recipe and it can be reinstated
without touching the serialiser.

Delimiter budget is intact: `|` top level, `;` list entries, `:` pairs. Supporter sets reuse `;`.

---

## 6. Sequencing

Each phase is independently shippable and leaves the game in a working state.

| # | Phase | Verify |
|---|---|---|
| 1 | ✅ **Done.** `Title.HolderHeroId` **replaces** `HolderClanId`; `IGenealogy.GetClanOf`; registry + `GetSuzerainUseCase` derive; `titles.config.xml` resolved at bootstrap. No save backfill — pre-phase-1 saves are unsupported | `dotnet test` green at 191 + 20. **No intended behaviour change.** The one observed change was four `barony_beeston` Strong claims lost — the phase exposed a `dadg_heroes.xml` inconsistency (`clan_vernon` owning no living members) that let a holder claim his own title; the content has since been fixed, restoring 53 holder clans at 42 claims — see §7 of the phase 1 plan |
| 2 | `HeroNode.FatherId`/`Age`; `ExecuteSuccessionUseCase` called from `OnHeroKilled` before claim re-derivation | Unit tests over a synthetic genealogy: eldest son / no sons → eldest daughter / no children → brother / no kin → clan-leader backstop. Live: `campaign.kill_hero` on a duke → eldest son holds the duchy, second son holds a Strong claim |
| 3 | Hero-accurate claim derivation; drop `PrincipalTitleByClan`; per-hero self-exclusion; daughters get claims | Brothers *and* sisters of a title-holder appear in claim queries |
| 4 | Daily evaluator, **inter-clan only**, with the player and same-kingdom gates | Run a campaign at speed; `campaign.list_private_wars` shows plausible declaration rates, never involving the player, never crossing a kingdom border |
| 5 | Supporter sets in `PrivateWar` + `WarSideResolver` | Resolver unit tests; a war with an explicit defector puts that clan on the attacker side in `AreEnemies` |
| 6 | Cadet spinoff, support solicitation, reabsorb-on-defeat | **Live check required** — cadet clan exists, is in the parent's kingdom, its party fights the parent, and encyclopedia/nameplates/banners survive |

Phase 1 is larger than it was under the revision-1 cache design — it is a migration rather than an append —
but it is still behaviour-neutral, and it is the phase that makes phases 2, 4 and 6 nearly free of
bookkeeping. That trade is the whole argument of §1.

Phase 6's live check is the riskiest item in the plan. `Clan.CreateSettlementRebelClan` proves the engine
supports mid-campaign clan creation, but banner rendering, encyclopedia pages, and AI behaviour for a
*lord* clan created this way are unverified in DADG. Two things to watch specifically:
`claimant.PartyBelongedTo` should follow the clan change automatically (`WarPartyComponent.Clan` derives
from `MobileParty.ActualClan`), and settlements the claimant personally held still point at the parent via
`Settlement.OwnerClan` — the existing de jure/de facto split is the right vocabulary for that gap
(cadet holds the dignity, parent remains the occupant).

---

## 7. Decisions — resolved

| # | Question | Answer |
|---|---|---|
| 1 | Succession ordering | Eldest son → (no sons) eldest daughter → collateral (brothers, uncles) → clan-leader backstop. **Open sub-question:** representation (§2.2) — recommended, two `else` branches |
| 2 | Which ranks are hero-held | **All of them.** No split rule |
| 3 | Does the player participate | **No.** Excluded as claimant *and* as target; revisitable later (§3) |
| 4 | Cadet clan on defeat | **Reabsorb** into the parent clan |
| 5 | Cross-kingdom claims | **Intra-kingdom only.** Inter-kingdom would need real `StanceLink` wars — a different mechanism, and major evolution in the private-war module |

The one thing still genuinely open is representation in §2.2. Everything else is settled enough to build.

---

## 8. Corrections to existing docs

`doc/feudal-hero-titles-and-houses.md` (2026-06-11) reaches the same conclusions and remains a good
companion, but parts are stale:

- Its step-1 prerequisite (de jure / de facto split) **has landed**.
- It references levies, tension, and petition/attainder elections — **all deleted** (`bc12347`, `589add9`,
  `74f65b0`).

`doc/feudal-private-wars-design.md` §10 specifies the AI trigger as the petition-deny path. That path no
longer exists; §3 of this document replaces it.
