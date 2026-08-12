# Private Wars — Temp-Kingdom Substrate: Precise Cost Accounting

> **Status:** Cost-analysis companion to `feudal-private-wars-design.md` (the chosen **stay-in-kingdom**
> substrate). **No code changes.** This document deliberately **re-opens the rejected alternative**
> from that doc's §13.1 (dual temporary splinter kingdoms) and costs it at the **same granularity** —
> model overrides, Harmony patches, Campaign Actions, cascades, mirror items, orchestration components —
> so the two substrates can be compared apples-to-apples. **It is not a reversal of the decision.** The
> findings *confirm* the decision and quantify why.
> **Branch:** `claude/banner-lord-mcp-integration-79WN8` · **Date:** 2026-06-15
> · **Engine:** verified against 1.3.1 (seven-agent source sweep).

---

## Table of Contents

1. [Headline: the fork](#1-headline-the-fork)
2. [What temp-kingdom makes free — and the asterisk](#2-what-temp-kingdom-makes-free--and-the-asterisk)
3. [Cost A — lifecycle (Actions, cascades, orchestration)](#3-cost-a--lifecycle-actions-cascades-orchestration)
   - [3.1 Setup / teardown Campaign Actions](#31-setup--teardown-campaign-actions)
   - [3.2 Cascades that must be suppressed or guarded](#32-cascades-that-must-be-suppressed-or-guarded--4)
   - [3.3 Orchestration components](#33-orchestration-components--9-required)
4. [Cost B — the membership re-attachment surface](#4-cost-b--the-membership-re-attachment-surface-the-real-price-of-the-hard-constraint)
   - [4.1 Policy & finance](#41-policy--finance--9-overrides--34-harmony)
   - [4.2 Voting & decisions](#42-voting--decisions--4-harmony)
   - [4.3 UI / display](#43-ui--display--8-consumers-3-uiextender)
   - [4.4 Shared external war — the win clawed back](#44-shared-external-war--the-win-clawed-back)
5. [Concurrency ceiling: one private war per clan](#5-concurrency-ceiling-one-private-war-per-clan)
6. [Head-to-head tally](#6-head-to-head-tally)
7. [Verdict](#7-verdict)

---

## 1. Headline: the fork

**Temp-kingdom is not cheaper. The "zero Harmony" claim only holds while the design is *violating* the
project's hard constraint. Fix the violation — as the project requires — and temp-kingdom needs *more*
patches than stay-in-kingdom, on a *worse* surface, plus a pile of lifecycle machinery stay-in-kingdom
never needs.**

The substrate forces a fork, and **both prongs lose**:

- **Prong 1 — leave the clan ejected (the §13.1 "zero Harmony" path).** The clan genuinely leaves
  kingdom K: it loses its policies, its tribute, its votes, and its participation in K's external wars.
  That directly **violates the hard constraint** — *"a feuding clan must NOT be out of the loop of its
  kingdom."* So this prong is **out of scope by definition.**

- **Prong 2 — make the ejected clan still behave as a member of K** (policies, voting, UI, shared
  external war). This is what a serious implementation must do. But "behave as a member of K while
  living in temp kingdom T" means **patching every consumer that reads kingdom membership** to redirect
  the ejected clan back to K — a **~11 model override + ~15 Harmony + 3 UIExtender + 2 mirror-handler**
  surface (§4), **on top of** the lifecycle cost (§3), and it **claws back the combat-side win** that
  was temp-kingdom's only advantage (§4.4).

In other words: temp-kingdom keeps the clan as **two faction identities at once** — temp-K for the feud,
home-K for membership — and every consumer in the engine must be patched to pick the right identity.
Stay-in-kingdom keeps **one** identity (home K) and patches only the bounded combat-pair exceptions.
One identity with a small exception set is strictly cheaper to build and maintain than two identities
with a large redirection set.

And even Prong 2, fully paid for, still **caps a clan at one private war at a time** (§5) and still
**cannot put the ejected clan in K's armies** (§4.4) — two failures no amount of patching fixes.

---

## 2. What temp-kingdom makes free — and the asterisk

The real advantage, stated honestly. Because the two belligerents have **different `MapFaction`s and a
real `StanceLink` at war**, the MapFaction wall (design doc §2) works *for* us. The hostility surface
stay-in-kingdom must manufacture is inherited:

| Stay-in-kingdom must build (design §4/§5) | Temp-kingdom gets it free because… |
| --- | --- |
| ~18 Harmony patches on hostility consumers (`IsEnemy`, `StartPartyEncounter`, `StartSettlementEncounter`, `CanPartyJoinBattle`, prisoner hold/capture, recruitment, retreat-target, army-disband…) | each reduces to `IsAtWarAgainstFaction(MapFaction, MapFaction)`, **true** for two real enemy kingdoms |
| 8 model overrides (target scoring, defender parties, encounter menu, reinforcements, army filter, recruit tiers, initiative, town access) | stock-correct once the two factions are really at war |
| The DADG war-drive loop + event-sourced score (§5) | strategic AI plans the siege itself; a real `StanceLink` carries the casualty/siege ledger |
| The player encounter/menu layer (§4.3) | native — the player attacks an enemy kingdom normally |

**Net genuinely free: the entire A↔C feud-combat surface.** This is real and it is temp-kingdom's only
win.

> **The asterisk (developed in §4.4).** This "free combat" is free **only for the feud itself** (A vs C).
> The *other* half of the combat requirement — the ejected clan continuing to fight **K's external
> wars** — is **not** free. Re-attaching it duplicates the very same hostility consumers, this time to
> make T_A hostile to K's enemies, and it *still* fails at army coordination. So even the combat win is
> only half a win.

---

## 3. Cost A — lifecycle (Actions, cascades, orchestration)

This is the cost that exists **regardless** of the constraint — just to stand the temp kingdoms up,
keep them alive, tear them down, and survive save/load. (Source-verified, 1.3.1.)

### 3.1 Setup / teardown Campaign Actions

The lifecycle *can* be driven with official Actions (no Harmony for the happy path — the §13.1 claim
survives *for the orchestration itself*), but it is **~7 order-sensitive Actions**, not "a couple of calls."

**Setup (per war, both belligerents):** `KingdomManager.CreateKingdom` ×2 → `MakePeaceAction.Apply`
×N to peel back the wars `CreateKingdom` auto-inherits from each clan's old kingdom →
`DeclareWarAction.ApplyByDefault(tempA, tempC)` → subscribe `CanKingdomBeDiscontinuedEvent` veto.

**Teardown (per war):** `MakePeaceAction.Apply(tempA, tempC)` **first** → `ChangeKingdomAction.ApplyByJoinToKingdom`
×2 (peace-first, or `AdjustFactionStancesForClanJoiningKingdom` silently drops the feud war) →
`ChangeClanInfluenceAction.Apply` ×2 to restore the influence every leave zeroes →
`DestroyKingdomAction.Apply` ×2 (or rely on auto-discontinue).

Compare: stay-in-kingdom's lifecycle is `registry.Add(pair)` / `registry.Remove(pair)` + a fief
snapshot — **two in-memory ops, no faction objects created or destroyed, no ordering hazards.**

### 3.2 Cascades that must be suppressed or guarded — 4

Side-effects the engine fires automatically on kingdom create/join/leave that the design does **not**
want:

| Cascade | Mechanism (1.3.1) | Severity | Fix |
| --- | --- | --- | --- |
| **Belligerent surrenders its fiefs on eject** | `ApplyByCreateKingdom` → `ClanLeaveKingdom(giveBackFiefs: true)` hands every town/castle to the **old kingdom's leader** via `ChangeOwnerOfSettlementAction.ApplyByLeaveFaction` | **CRITICAL** — a landed feuding clan loses its fiefs at war start | Harmony-patch `ClanLeaveKingdom` (**the first real Harmony**) or pre-transfer + restore fiefs (more Actions + a mis-owned window) |
| **Temp kingdom auto-discontinued mid-war** | `CanKingdomBeDiscontinued` = `!Eliminated && != PlayerClan.Kingdom && Settlements.IsEmpty()` → landless temp kingdom qualifies immediately | HIGH | `CanKingdomBeDiscontinuedEvent` veto |
| **AI votes to end its own war** | `DiplomaticBartersBehavior` proposes `MakePeaceKingdomDecision` daily; a landless one-clan kingdom scores the war as ruinous | HIGH | `DiplomacyModel` peace-scoring override or scrub `UnresolvedDecisions` each tick |
| **Temp kingdom hires mercenaries** | mercenary-accept path fires for any kingdom with gold > 20 000 | MEDIUM | gate temp-kingdom gold at 0 / override mercenary model |

Stay-in-kingdom has **zero** of these — no clan ever changes faction, so the whole class is absent.

### 3.3 Orchestration components — 9 required

Registry (clan→temp→home), war-end/peace callback, empty-kingdom veto, war-inheritance guard,
army-detachment handler, home-kingdom-destroyed guard, save/load heal, war-queue/N=1 enforcement,
player-clan UX suppression. (All source-confirmed required; see the agent sweep.) Stay-in-kingdom needs
only a registry + a save serialiser.

---

## 4. Cost B — the membership re-attachment surface (the real price of the hard constraint)

This is the section the first draft missed and the correction surfaced. To satisfy *"a feuding clan
must NOT be out of the loop of its kingdom,"* the ejected clan must **still behave as a member of K**
even though its `MapFaction` is the temp kingdom. That means patching every consumer that reads kingdom
membership to redirect the ejected clan back to K. The surface, enumerated against 1.3.1:

### 4.1 Policy & finance — 9 overrides + 3–4 Harmony

Policy effects and kingdom income are read off `clan.Kingdom` / `settlement.OwnerClan.Kingdom`, which
now point at the temp kingdom (no policies, no tribute, empty budget). Redirecting them:

- **9 clean model overrides** (each `Default…Model` subclassed via `AddModel`): `ClanPoliticsModel`
  (influence-from-policies), `SettlementLoyaltyModel`, `SettlementMilitiaModel`, `SettlementTaxModel`,
  `SettlementProsperityModel`, `SettlementEconomyModel`, `ClanFinanceModel` (tribute share, budget
  draw, debt, wallet shares — one override covers all finance seams), `SettlementFoodModel`,
  `PartyWageModel`.
- **3–4 Harmony** where the kingdom lookup is hardcoded outside a model seam:
  `ChangeClanInfluenceAction` (`MilitaryCoronae` ×1.2 reads `clan.MapFaction`), `Building.GetConstructionCost`
  (`CastleCharters` reads `OwnerClan.Kingdom`), `DefaultPartySizeLimitModel` (`NobleRetinues`/`RoyalGuard`
  read `partyMapFaction`), and optionally the `DailyTickClan` budget top-up.

### 4.2 Voting & decisions — 4 Harmony

`KingdomDecision.DetermineSupporters()` iterates `Kingdom.Clans` and is a **non-virtual base method** —
the ejected clan is excluded from **every** vote in K (no weight, no influence spend, no sponsorship).
Plus `DetermineInitialCandidates()` (×2 subtypes iterate `Kingdom.Clans` for candidacy),
`KingdomDecisionProposalBehavior` (scopes AI proposals by `clan.Kingdom`), and `DetermineChooser()`
(only if the ejected clan is K's ruling clan). None has a model seam — **all Harmony.** This is the
mechanical heart of the hard-constraint violation: without these patches the clan literally cannot vote
in its kingdom.

### 4.3 UI / display — 8 consumers (3 UIExtender)

So the clan shows and acts as part of K, not a phantom faction: `EncyclopediaClanPageVM` (parent
kingdom) and `EncyclopediaFactionPageVM` (clan list) and `KingdomClanVM`/`KingdomManagementVM`
(management clan list) are **UIExtender-eligible mixins (3)**; `CampaignUIHelper.GetHeroKingdomRank`
(static), `PartyNameplateVM` + `SettlementNameplateVM` (raw color logic), `KingdomClanItemVM` (clan
type), and `AgentOrigin.FactionColor/Color2/Banner` across 3 origin classes (battle banners) are **raw
Harmony (~5)**.

### 4.4 Shared external war — the win clawed back

This is the decisive item. The clan must keep fighting **K's external wars** while ejected — but war
participation is `MapFaction`-based, and the clan's MapFaction is now T_A, not K. The agents tested both
ways to restore it:

- **Stance-declaration alone fails.** Calling `DeclareWarAction(T_A, E)` for each of K's enemies E
  *does* make A's lone parties hostile to E — but T_A then **makes its own peace with E independently**
  (a landless one-clan kingdom scores peace as instantly attractive; the AI runs per-pair peace checks),
  E may **declare its own war on T_A**, and the two war states **desync** from K's with no notification.
  Containing that needs `DiplomacyModel` scoring overrides + **2 continuous mirror handlers**
  (`WarDeclared`/`MakePeace`, with re-entrancy guards) to keep T_A's war slate chained to K's.
- **It re-introduces the stay-in-kingdom combat surface.** Making T_A's parties correctly hostile to E
  touches the **same five consumers** stay-in-kingdom patches (`IsEnemy`, `CanPartyJoinBattle`, siege
  defender selection, army target scoring, encounter start) — so the "free combat" of §2 is **only free
  for the feud**, not for the external war. You pay a chunk of that surface anyway.
- **And it still fails at army coordination.** `Army.Kingdom` is a hard field and
  `GetMobilePartiesToCallToArmy` iterates `leaderParty.MapFaction.WarPartyComponents`. A's parties live
  in `T_A.WarPartyComponents`, so **K's armies never consider them** — the ejected clan can fight K's
  enemies only as isolated parties, never in K's coordinated campaigns. This is the **double-identity
  contradiction**: no single MapFaction-based answer can make A simultaneously feud-hostile to C,
  war-hostile to E *inside K's army pool*, and socially friendly to K. It is **not patchable** without
  re-architecting army membership.

**§4 subtotal (to satisfy the constraint): ~11 model overrides + ~15 Harmony patches + 3 UIExtender
mixins + 2 continuous mirror handlers — and shared external war still degraded.**

---

## 5. Concurrency ceiling: one private war per clan

`Clan.Kingdom` is single-valued (`SetKingdomInternal` removes from the old kingdom before setting the
new). One clan → one kingdom → one temp kingdom → **one** eject-based private war at a time. The escapes
fail: a second eject abandons the first war; one shared temp kingdom puts both opponents at the same
MapFaction so they **can't fight**. The only safe behaviour is to **queue** the second war.
Stay-in-kingdom has no such ceiling (many pairs = many registry entries; design §9). For a CK3-style
layer where a strong clan feuds several neighbours at once, **N=1 is a feature regression.**

---

## 6. Head-to-head tally

| Dimension | **Stay-in-kingdom** (chosen) | **Temp-kingdom, constraint-satisfying (Prong 2)** |
| --- | --- | --- |
| Model overrides | **8** (2 heavy) | **~11** (9 policy/finance + diplomacy + army filter) |
| Harmony patches | **~18** — on the **bounded combat surface**, stable 1.3.1→1.4.6 | **~15** — on the **unbounded membership surface** (grows with every kingdom feature) **+ ~3 combat consumers re-introduced for external war** |
| UIExtender mixins | 0 (2 cosmetic optional) | **3** required for membership display |
| Lifecycle Campaign Actions | **2 in-memory ops** + fief snapshot | **~7 order-sensitive Actions** |
| Engine cascades to guard | **0** | **4** (incl. CRITICAL fief surrender) |
| Orchestration components | registry + serialiser | **9 required** |
| Continuous mirror handlers | 0 | **2** (war/peace, with guards) |
| Faction identities to maintain | **1** (home K + small pair exception set) | **2** (temp-K + home-K, redirected per consumer) |
| DADG war-drive loop | required | not needed (native war AI) — *but* external-war army drive still broken |
| **Shared external war** | **native & complete** (clan never leaves K) | **degraded** — lone parties only; **cannot** join K's armies (unfixable) |
| **Concurrency** | **N wars per clan** | **1 war per clan** (hard ceiling) |
| **Hard constraint** | **satisfied by construction** | satisfied **only** by paying §4 in full — and **still incomplete** (army coordination) |
| Version resilience (§17) | combat surface → **stable** (1.4.6 *smaller*) | membership surface → **grows** with every kingdom feature |

**Reading it:** once the constraint is actually honoured, temp-kingdom's patch count is **comparable or
higher** than stay-in-kingdom (~11+15+3 ≈ 29 patches/mixins vs ~26), **plus** ~7 Actions + 4 cascade
guards + 9 orchestration components + 2 mirror handlers that stay-in-kingdom simply doesn't have. And
its patches sit on the **unbounded** surface while stay-in-kingdom's sit on the **bounded, version-stable**
one. The "fewer patches" intuition is exactly inverted.

## 7. Verdict

**The correction is decisive: temp-kingdom's apparent cheapness was an artifact of the constraint
violation.** Counting patches while the clan is genuinely ejected (Prong 1), temp-kingdom looks free —
but that state is forbidden. Counting patches in the only permitted state (Prong 2, the clan re-attached
to K), temp-kingdom is **more** work than stay-in-kingdom, on a **worse-aging** surface, and it carries
the entire lifecycle/orchestration burden besides.

Worse, two costs are **unpayable**:

1. **Shared external war cannot be fully restored** — the ejected clan can never join K's armies
   (`Army.Kingdom` / per-MapFaction `WarPartyComponents`), so it sits out K's coordinated campaigns no
   matter how many consumers we patch.
2. **Concurrency is capped at one war per clan** by the single-valued `Clan.Kingdom`.

Stay-in-kingdom pays a **larger-looking but bounded, single-identity, version-stable** combat-layer cost
(the ~18 Harmony of design §4) and in exchange gets the membership surface, shared external war,
unlimited concurrency, and the hard constraint **for free, by construction**. That remains the correct
trade. **No change to the chosen substrate.**

> **Honest caveat (parity with design §4.3 / §17.4).** The Action-level lifecycle and the consumer
> enumerations are verified against 1.3.1 decompiled source; a few bodies (`ClanLeaveKingdom` give-back
> branch, `DiplomaticBartersBehavior` peace scoring for a landless one-clan kingdom, a handful of UI VM
> internals) were read from signatures + partial bodies. Mechanisms are certain; exact seams would need
> confirmation at implementation time — moot unless the decision is revisited.
