# Private Wars — Real, Map-Represented Casus-Belli Wars

> **Status:** Analysis / design document. No code changes have been made as a result of this
> document. Companion to `feudal-title-transfer-design.md` (de jure/de facto split, implemented),
> `feudal-titles.md` (architecture), and `feudal-hero-titles-and-houses.md` (future hero grain).
> **Branch:** `claude/banner-lord-mcp-integration-79WN8`
> **Date:** 2026-06-14
>
> **Substrate decision (locked):** belligerent clans **remain members of their home kingdom**;
> hostility between them is **manufactured via engine-model overrides and Harmony patches on the
> clan/party-grain hostility consumers.** This **supersedes** this document's own earlier design
> (dual temporary splinter kingdoms) and `feudal-system-analysis.md` Part B's recommendation
> (design **(b)**, abstract private war). The two superseded substrates — temp splinter kingdoms and
> lone-clan ejection — are retained as **§13 Rejected alternatives** with the reasoning that killed
> them.
>
> **Why stay-in-kingdom won (one line):** ejection and temp-kingdoms both force us to *mirror* an
> **unbounded** kingdom-membership surface (policies, tributes, diplomacy participation, kingdom
> votes, encyclopedia/settlement display) that grows with every future TaleWorlds kingdom feature;
> staying in the kingdom inherits all of that **natively and for free**, and confines our changes to
> the **small, stable** combat-hostility area. See §1, §12, §13.

---

## Table of Contents

1. [What changed and why](#1-what-changed-and-why)
2. [The engine constraint: the MapFaction wall](#2-the-engine-constraint-the-mapfaction-wall)
3. [The mechanism: stay-in-kingdom with pair-scoped manufactured hostility](#3-the-mechanism-stay-in-kingdom-with-pair-scoped-manufactured-hostility)
4. [Patch surface: model overrides vs. Harmony](#4-patch-surface-model-overrides-vs-harmony)
5. [The war must be DADG-driven](#5-the-war-must-be-dadg-driven)
6. [War score model](#6-war-score-model)
7. [Casus belli framework](#7-casus-belli-framework)
8. [Settlement revert — status quo ante except the prize](#8-settlement-revert--status-quo-ante-except-the-prize)
9. [Concurrent wars: a clan in several at once](#9-concurrent-wars-a-clan-in-several-at-once)
10. [War lifecycle (state machine)](#10-war-lifecycle-state-machine)
11. [Domain & integration mapping](#11-domain--integration-mapping)
12. [Kingdom membership preserved natively (the resilience win)](#12-kingdom-membership-preserved-natively-the-resilience-win)
13. [Rejected alternatives](#13-rejected-alternatives)
14. [Edge cases](#14-edge-cases)
15. [Open risks to verify in-game](#15-open-risks-to-verify-in-game)
16. [Locked-in v1 parameters](#16-locked-in-v1-parameters)
17. [Version compatibility (1.3.1 → 1.4.6)](#17-version-compatibility-131--146)

---

## 1. What changed and why

The requirement is fixed: **all wars are real and represented on the campaign map; war cannot be
simulated** (overrules `feudal-system-analysis.md` Part B's abstract-war recommendation). Two clans
in the same kingdom must be able to fight a *real, on-map* war over a claim.

The whole design turns on one question: **what is the substrate that makes two same-kingdom clans
map-hostile?** Three candidates were evaluated against the 1.3.1 engine:

- **(a) Dual temporary splinter kingdoms** — eject *both* belligerents into one-clan kingdoms at war
  only with each other, restore on war end. (This document's earlier design.)
- **(b) Lone-clan ejection** — make the belligerent kingdomless (its own MapFaction) for the duration.
- **(c) Stay-in-kingdom + manufactured hostility** — the clan never leaves; we make the engine treat
  the specific clan pair as enemies via model overrides and Harmony patches. **← chosen.**

(a) and (b) share a fatal property: once a clan leaves its kingdom, it **loses everything kingdom
membership confers** — policies, tributes, shared diplomacy, kingdom-decision participation, and its
encyclopedia/settlement identity — and the "temporary" design must then **mirror and restore all of
it**. That mirror surface is **unbounded**: it already includes a non-trivial list, and it *grows
with every kingdom feature TaleWorlds ships in future patches*. Worse, a clan mid-feud that is "out
of the loop of its kingdom" violates a hard project constraint.

(c) inverts the tradeoff. By keeping the clan in its kingdom, **all kingdom-membership state is
inherited natively and for free** (§12), and the design **de-facto benefits from future kingdom
features without being patched for them**. The cost moves to the *combat* layer: we must manufacture
A↔C hostility by patching the engine's hostility consumers (§3, §4) and **drive the war ourselves**
(§5). That cost is real and larger than "a few patches" (§4 enumerates ~12 patches + 6 model
overrides), but it lives in a **bounded, stable** part of the engine (combat side-assignment has not
materially changed across versions), whereas the ejection mirror surface is unbounded and
version-fragile. We accept the combat-layer cost to escape the membership-mirror cost.

> **Honest note carried from analysis:** §2's old framing ("patching `IsAtWarAgainstFaction` to lie
> for selected clan pairs *is* rewriting the war subsystem") was an argument *against* substrate (c).
> The probes in §4 confirm the patch surface is indeed sizeable. We choose (c) anyway because the
> resilience argument is *orthogonal* to patch-count: it is about which surface is **bounded over the
> maintenance timeline**, and the membership surface is the unbounded one.

---

## 2. The engine constraint: the MapFaction wall

Bannerlord resolves **all** map hostility through `MapFaction`. For a clan inside a kingdom,
`Clan.MapFaction` is the **kingdom**; for a kingdomless clan it is the clan itself
(`Clan.MapFaction => Kingdom ?? this`, confirmed 1.3.1). Party-AI targeting, encounter menus,
`MapEvent` side assignment, siege defense and `FactionManager.IsAtWarAgainstFaction` all reduce both
parties to MapFaction **before** consulting the stance:

- `FactionManager.IsAtWarAgainstFaction(f1, f2)`: `if (f1==null || f2==null || f1==f2 || f1.IsEliminated || f2.IsEliminated) return false;` then `IsAtConstantWar` → `GetShallowDiplomaticStance` → `StanceLink`. Two clans in kingdom K both reduce to `f1==f2==K` → short-circuits to `false`.
- `DefaultMobilePartyAIModel.IsEnemy` → `FactionManager.IsAtWarAgainstFaction(party.MapFaction, mobileParty.MapFaction)` → same wall.
- `EncounterManager.StartPartyEncounter`: `if (attackerParty.MapFaction == defenderParty.MapFaction) { /* merge to same side */ } else { StartBattleAction.Apply(...) }` — a **direct `==`**, not even the method call.

Two clans that share a MapFaction can never be mutually hostile through the stock paths.

**Crucial corollary — there is no single central lever.** Both `IsAtWarAgainstFaction` and the
diplomacy-model stance methods (`IsAtConstantWar`, `GetShallowDiplomaticStance`) operate at
**faction grain** (`IFaction` = K for *both* A and C). They cannot distinguish A's party from C's
party — both are K — so patching them is useless for a same-kingdom pair. The only place the
distinction survives is the **call sites that still hold party/clan context** (`IsEnemy` has both
parties; `CanPartyJoinBattle` has the joining party; `GetTargetScoreForFaction` has the mobile
party). **This is why the patch surface is many small consumers rather than one chokepoint:** we
manufacture hostility at **clan grain** by intercepting the consumers and de-referencing party →
`ActualClan`, then consulting our private-war pair registry (§3). The faction-grain stance is left
untouched, which is also what keeps K's *real* (external) diplomacy intact (§12).

---

## 3. The mechanism: stay-in-kingdom with pair-scoped manufactured hostility

For a private war between attacker clan **A** and defender clan **C** (same kingdom, different
kingdoms, or independent — the procedure is uniform), **neither clan leaves its kingdom.** Instead:

1. **Register the ordered pair** `(A, C)` in a `PrivateWarHostilityRegistry` (an in-memory,
   save-synced set keyed by *unordered* clan pair, with attacker/defender roles stored per record).
   Registration is the single switch that flips every patch in §4 to treat A↔C as enemies.
2. **Snapshot** `OriginalFiefOwners` for revert (§8). No influence/home-kingdom snapshot is needed —
   the clan never leaves K.
3. The patches in §4 now **manufacture clan-grain hostility for this pair only**: each intercepted
   consumer recovers the acting clan from the party (`party.ActualClan` / `LeaderHero.Clan`) and, if
   the *pair* is registered, returns the "enemy" answer it would have returned for a cross-kingdom
   pair; otherwise it returns the stock answer unchanged.
4. **DADG drives prosecution** (§5): because the engine will not *autonomously* fight a same-faction
   pair (its planners short-circuit on `MapFaction` equality before our consumer patches are even
   reached), DADG issues the siege/army intent for the attacker and tracks the score from campaign
   events (§6).
5. **Resolve** (§10): apply the prize (attacker win) or revert captures (§8), then **unregister the
   pair** — the patches revert to stock and A↔C are friendly again. No rejoin, no kingdom teardown.

```
SETUP (per war, attacker A vs defender C)
  registry.Add(PrivateWarPair(A, C, attacker=A))      // flips §4 patches for this pair
  snapshot OriginalFiefOwners for A's + C's fiefs + goal   // §8
  persist PrivateWar record                            // §11

PROSECUTE (daily / on events)
  drive attacker intent toward the goal settlement     // §5 — engine won't do it unprompted
  recompute score from event-sourced observations      // §6 (no StanceLink exists for the pair)
  resolve when |S| ≥ 100  (or white peace)

RESOLVE (per war)
  apply prize (attacker win, §7) OR revert this pair's captures (§8)
  registry.Remove(pair)                                // §4 patches revert to stock; A↔C friendly
  mark record resolved
```

**Why same-kingdom stops being special:** the registry is keyed by clan pair, and the §4 consumers
work at clan grain, so whether A and C share a kingdom, are in different kingdoms, or are independent
makes **no** difference — the topology during the war is always exactly "the pair (A,C) is hostile,
everyone else sees stock stances." One code path covers every participant combination, and — unlike
the temp-kingdom design — there are **no faction objects created or destroyed**, so the orchestration
traps of §9 (teardown / separate-peace) simply do not exist.

**Hostility is clan-grained; the engine checks are faction-grained.** Every patch in §4 must
de-reference the faction-grain argument back to the acting clan before consulting the registry. A
party with no clan (caravans, bandits, garrison-only) is never in a private war, so the patches
fast-path to stock for them.

---

## 4. Patch surface: model overrides vs. Harmony

This is the cost of substrate (c), verified against 1.3.1. Each entry says whether it is a **clean
model override** (register a `Default…Model` subclass via `CampaignGameStarter.AddModel` — DADG's
preferred zero-Harmony path) or a **Harmony patch** (static class, core data class, private helper,
or `CampaignBehavior` with no model seam). Every entry is **pair-scoped**: it consults the §3
registry and returns the stock answer when the pair is not in a private war.

### 4.1 Clean model overrides (no Harmony) — 8

| What it controls in a private war | Model seam (declaration) | Notes |
| --- | --- | --- |
| **Siege/raid target scoring** *and* **kingdom-relief damping** | `TargetScoreCalculatingModel.GetTargetScoreForFaction` — `public abstract` | One override does double duty: the Defender-objective war gate that would otherwise pull uninvolved K lords into relieving the private siege lives **here**, not in `AiMilitaryBehavior`. This is the single biggest de-risker — the scariest "kingdom-wide AI" item is mostly a *model* change. |
| **A's own field army can defend A's besieged fief** | `EncounterModel.GetDefenderPartiesOfSettlement` / `GetNextDefenderPartyOfSettlement` — `virtual` | `DefaultEncounterModel` delegates to `Town.GetDefenderParties` (non-virtual). Override at the **model** layer, not `Town`. Without this only the static garrison defends (A's relief parties are excluded by the besieger war-check). |
| **Player routed to hostile (not friendly) settlement menus** | `EncounterGameMenuModel.GetEncounterMenu` — `public abstract` (`DefaultEncounterGameMenuModel`) | For C's settlement, `settlement.MapFaction == MainParty.MapFaction` short-circuits the player into `town_outside`/`castle_outside`/`village_outside`/`army_wait_at_settlement` — the *friendly entry* menus, with no siege/raid option. Override to fall through to the hostile menus for a registered pair. **Load-bearing for the player path** (pairs with the besiege-button Harmony patch in §4.5). |
| **Reinforcements join the correct side of a private battle** | `EncounterModel.FindNonAttachedNpcPartiesWhoWillJoinPlayerEncounter` — `virtual` | Both `IsAtWarWith` calls fail same-K, so **no nearby ally/enemy parties join either side**. Override to add parties whose clan is a registered ally/enemy of the pair. |
| **Keep C out of A's army** | `ArmyManagementCalculationModel.GetMobilePartiesToCallToArmy` — `public abstract` | Stock candidate pool is `leaderParty.MapFaction.WarPartyComponents` (whole kingdom) → C auto-invited into A's army. Filter the pair out. |
| **A can't freely recruit C's troops (tier count)** | `VolunteerModel.MaximumIndexHeroCanRecruitFromHero` — `public abstract` | Governs recruitable tiers + the same-faction `+1` bonus / war penalty. (The *entry* into C's settlement is a separate Harmony item below.) |
| **Strength accumulation / engage initiative** | `MobilePartyAIModel.GetBestInitiativeBehavior` — `public abstract` | **Clean seam but heavy:** the whole method body must be re-implemented; there is no smaller hook. Stock sums force by MapFaction equality (inflating A's perceived strength with C's troops) and routes through the private `IsEnemy`/`CalculateStanceScore` helpers (below). |
| **Town access** | `SettlementAccessModel.CanMainHeroEnterSettlement` — `public abstract` | **Clean seam but heavy:** `CanMainHeroEnterTown`/`…Castle`/`…KeepInternal` are *private* dispatch helpers (all key on `DiplomacyHelper.IsSameFactionAndNotEliminated`, which is true same-K → `FullAccess`, so the player walks into C's town/castle/keep unchallenged); overriding the public method means re-implementing the town/castle/village dispatch yourself. |

> **Implementation status — defender-population consolidation (runtime-verified, 2026-06-24).**
> The defender population of a same-kingdom private-war fortification is reached through **three
> independent, faction-war-gated engine seams** that must all admit the same set or a surface drifts:
> - `EncounterModel.GetDefenderPartiesOfSettlement` (`DadgEncounterModel` override) — battle candidacy
>   + siege-overlay *presence*.
> - `EncounterModel.GetNextDefenderPartyOfSettlement` (`DadgEncounterModel` override) — siege-overlay
>   defender/attacker *bucketing*. **This was the un-overridden seam:** `EncounterMenuOverlayVM` buckets
>   via `Settlement.HasInvolvedPartyForEventType`, which loops this method, so the garrison/militia were
>   present in the involved list yet rendered on the player's (besieger) panel.
> - `SiegeEvent.CanPartyJoinSide` (`SiegeDefenderJoinPatch` postfix, §4.2) — assault `MapEventSide`
>   *membership*.
>
> All three (plus the `SallyOutStrengthPatch` strength scan, §4.2) now consult one shared rule,
> `PrivateWarSiegeDefenderPolicy.IsDefender/AreEnemies` (MAIN layer; composes over the domain
> `FeudalServices.PrivateWarHostility`), so the seams cannot disagree. The rule is **player-agnostic** —
> it keys on `besiegerClan` vs `settlement.OwnerClan`, never on `MainHero`.
>
> Runtime-verified PASS (`privatewartest`):
> - **Player besieger** (Pontefract Castle, `clan_york`): garrison (137) + militia bucket to the
>   Defender panel; absent from the Attacker/besieger panel.
> - **Sally-out** (weak besieger, 1 troop): the garrison sallies (296 vs 1). `SallyOutStrengthPatch`
>   fires with `AreEnemies=true` (`num3`=324.99 sally side vs `num`=0.797 besieger → `garrisonWouldSally`);
>   `CanPartyJoinSide(garrison, Defender)=true`; no crash.
> - **AI vs AI** (`clan_hastings` besieging `clan_howard`'s `Norwich_town`, no player):
>   `GetDefenderPartiesOfSettlement` admits garrison (502) + militia (407); `CanPartyJoinSide=true`;
>   `TryCaptureCompletedSieges` transferred Norwich → `clan_hastings`; war stays Active; no crash.
>   Confirms same-kingdom AI sieges resolve via the **prep-complete capture** path (§10), not an assault
>   `MapEvent`.
>
> **Vanilla sally-out parity (resolved + runtime-verified 2026-06-24).** `IsDefender` now takes
> `mapEventType` and excludes **militia** from the `SallyOut` defender set, mirroring vanilla
> `Town.GetDefenderParties` (`!IsMilitia || battleType != SallyOut`) — militia are static wall defenders,
> not field troops. The garrison and any feud-belligerent lord parties still sally normally.
> `CanPartyJoinSide` (the wall assault, a `Siege` battle) passes `Siege`, so militia continue to defend
> the walls there; only the `SallyOut`/strength-scan path drops them. This keeps `num3` in
> `SallyOutStrengthPatch` aligned with vanilla (no inflated sally-side strength). `BlockadeSallyOutBattle`
> still includes militia, matching vanilla, which gates only on `SallyOut`. Verified PASS at the live
> `DadgEncounterModel.GetDefenderPartiesOfSettlement` call site (`privatewartest`, Pontefract):
> `IsDefender(militia, …, SallyOut)=false`, `IsDefender(garrison, …, SallyOut)=true`,
> `IsDefender(militia, …, Siege)=true`; no exception.
>
> Fixtures for all three scenarios: `doc/features/private-wars-siege-defenders.feature`.

### 4.2 Harmony patches required — ~12

| What it controls in a private war | Host (kind) | Why no model seam |
| --- | --- | --- |
| **AI treats C as a target** (the central hostility signal) | `DefaultMobilePartyAIModel.IsEnemy` — **private** method | **Not on the abstract base.** The most load-bearing hostility check is a private helper the model interface never exposes. Harmony-patch the private method (cleaner than re-implementing `GetBestInitiativeBehavior` wholesale just to reach it). |
| Same-faction → −1 "ally" stance score | `DefaultMobilePartyAIModel.CalculateStanceScore` — **private** | Private helper, not on the base; must be flipped for the pair alongside `IsEnemy`. |
| **Un-merge the encounter** (start a battle instead of merging to one side) | `EncounterManager.StartPartyEncounter` / `HandleEncounterForMobileParty` — **static** | Static manager class; the same-side decision is a direct `MapFaction ==` (§2). |
| **Siege actually resolves** (assault transition) | `EncounterManager.StartSettlementEncounter` — **static** | The `AssaultSettlement` branch is gated by `IsAtWarAgainstFaction(attacker.MapFaction, settlement.MapFaction)`; without a patch a same-K siege camps forever and never becomes a battle. **Load-bearing for "real sieges."** |
| **Reinforcements can join the right side** | `MapEvent.CanPartyJoinBattle` — core class, **non-virtual** | Core campaign data class; filters joiners by `IsAtWarWith`. |
| **C can re-besiege a fief it lost** | `ChangeOwnerOfSettlementAction.ApplyInternal` — **static action** | After A takes a fief, the siege-cancel / besieger-cancel branches (keyed on `IsAtWar`/`MapFaction`) block C from re-besieging it; needed for war back-and-forth. |
| **Uninvolved K lords don't jam on relief** (behavior side) | `AiMilitaryBehavior.CalculateMilitaryBehaviorForSettlement` — **private**, CampaignBehavior | Has no war gate of its own (keys on `LastAttackerParty.IsActive`). **Mostly covered by the `GetTargetScoreForFaction` model override (4.1)**; a Harmony patch here is a fallback only if the model override proves insufficient in testing. |
| **Captured private-war lords stay captured (AI side) — until the war ends** | `PrisonerReleaseCampaignBehavior.ReleasePartyPrisoners` — private sweep, CampaignBehavior | **AUTO-END risk #1.** On any peace/owner-change/load it frees prisoners whose faction is `!IsAtWarWith` the captor — and A↔C are never *formally* at war, so private-war prisoners auto-free. Triggers on `MakePeace`, `ClanChangedKingdom`, `OnSettlementOwnerChanged`, `OnGameLoaded`. No model hook. **The `PrivateWarPrisonerRetentionPatch` (prefix on `EndCaptivityAction.ApplyInternal`) blocks only the *involuntary* funnels** — `ReleasedAfterPeace`/`ReleasedAfterBattle` of a captive whose clan `AreEnemies` the captor — so the captive stays held *while the war is active*. Escape (`ApplyByEscape`), ransom and death are never blocked, so escape attempts still occur. **When the war concludes, `PrivateWarCampaignBehavior.ReleaseWarPrisoners` deliberately frees the war's captives** (`EndCaptivityAction.ApplyByPeace`) — auto-release during the war makes no sense, but the principals are at peace once it ends. This runs *after* the status flips to `Concluded`; since `AreEnemies` is Active-gated, the retention patch no longer blocks the release. |
| **Captured player stays captured (player side)** | `PlayerCaptivityCampaignBehavior.CheckCaptivityChange` — CampaignBehavior | **AUTO-END risk #2** (NEW). `!IsAtWarAgainstFaction(captor.MapFaction, MainParty.MapFaction)` is true same-K → switches to `menu_captivity_end_no_more_enemies` and **releases the captured player the first tick after capture.** Player-side twin of `PrisonerRelease`. Harmony prefix. |
| **Enemy heroes don't walk free in the captor's town** | `PrisonerCaptureCampaignBehavior` — private, CampaignBehavior | Capture/eject sweep gated on `IsAtWarWith(hero.MapFaction, settlement.MapFaction)`; never fires for same-K. **Resolved at the synthetic capture instead** (§10): a same-kingdom siege never stages an assault `MapEvent`, so the engine's own `MapEvent.LootDefeatedPartyMembers → TakePrisonerAction.Apply` post-assault prisoner-taking never runs. `TryCaptureCompletedSieges` mirrors it directly — when the goal falls, every losing-side lord party still inside has its leader taken prisoner by the besieger (`TakePrisonerAction.Apply(besiegerParty, hero)`), exactly as a real assault would. The AUTO-END-risk-#1 `PrisonerReleaseCampaignBehavior` patch then keeps them held despite the shared MapFaction. No model hook. |
| **A can't freely recruit inside C's town/castle** (entry gate) | `RecruitmentCampaignBehavior.OnBeforeSettlementEntered` — CampaignBehavior | Hardcoded `ownerClan.IsAtWarWith` gate (and **villages have no gate at all**). |
| **AI doesn't freely wander into enemy fiefs** | `AiBehaviorCampaignBehavior.IsSettlementSuitableForVisitingCondition` — private, CampaignBehavior | No model hook. Also blocks the expulsion sweep that would eject C's parties from A's towns (`!Owner.MapFaction.IsAtWarWith(settlement.MapFaction)`). |
| **Attacker's feud army doesn't auto-disband** | `ArmyManagementCampaignBehavior` / cohesion-loss path (`FactionsAtWarWith.AnyQ(x => x.Fiefs.Any())`) — CampaignBehavior | NEW, HIGH. The disband guard reads `FactionsAtWarWith`, which is **blind to a registry-only war** → an army A raises for the feud sees "no enemy with fiefs" and dissolves. Patch (or supplement) the disband condition for active pairs. |
| **Enemy fiefs aren't offered to A as safe retreat/camp targets** | `SettlementHelper.FindNearestFortificationToMobileParty` / `FindNextSettlementAroundMobileParty` — static helpers | NEW, HIGH. Both filter on `!OwnerClan.IsAtWarWith(mobileParty.MapFaction)` = K-vs-K = false → C's fortifications pass as valid retreat targets for A's parties. Harmony postfix to exclude the enemy set. |
| **Crime by a player belligerent doesn't spawn a phantom K-vs-K war** | `CrimeRatingChangeAction.ApplyByCrimeRatingChange` — static action | NEW, HIGH (player only). If the player (A) commits crimes against C's fief, `!faction.IsAtWarWith(MainHero.MapFaction)` is true → it tries to `DeclareWarAction` K-vs-K. The same-faction guard blocks the declaration, but the crime/relation escalation still fires confusingly. Harmony prefix to guard the same-kingdom pair. |
| **Villagers from A can be intercepted en route to C** (optional) | `VillagerCampaignBehavior` redirect — CampaignBehavior | NEW, optional. `IsAtWarAgainstFaction(villager.MapFaction, target.MapFaction)` = false → A's villagers trade peacefully at C's town. Patch only if the design wants economic pressure. Low priority. |
| **Registry survives a belligerent changing kingdom** | `ChangeKingdomAction.ApplyInternal` join/leave cascade — static action (**registry-maintenance, not a hostility patch**) | NEW, HIGH. War-inheritance (join) and peace-cleanup (leave) run off `FactionsAtWarWith`, which is **blind to the registry war** → nothing auto-syncs. DADG must hook `OnClanChangedKingdom` to **prune** the pair if a participant leaves (or decide it persists) — see §10/§14. No engine patch needed; a behavior callback. |
| **No free food/trade in enemy towns** (minor) | food-supply `CampaignBehavior` (`TryBuyingFood`) | Gate hardcoded in the behavior (only `DoesPartyConsumeFood` is a model). Low priority. |

### 4.3 Player-facing encounter & menu layer (Harmony) — only when the player is a belligerent

This whole block is **dormant unless the player's own clan is A or C.** It is the player-path twin of
the AI drive layer: the AI never *autonomously* attacks C (§5), and the player is symmetrically
*blocked from* attacking C until these gates are patched. The model-layer half of the player path
(`GetEncounterMenu`, `SettlementAccessModel`, `FindNonAttachedNpcParties…`) is the clean overrides in
§4.1; the items below have **no model seam** (static managers, private `PlayerEncounter` methods, and
private `EncounterGameMenuBehavior` condition/consequence delegates).

| What it controls | Host (kind) | Failure symptom for a player belligerent |
| --- | --- | --- |
| **Player put on the correct battle side** | `PlayerEncounter.SetupFields` — **private** | CRITICAL. Attacking C's settlement, `defender.Settlement.MapFaction == MainParty.MapFaction` force-assigns the player to **Defender** → wrong UI, no attack options, corrupts all downstream routing. |
| **The "Besiege" button exists at all** | `EncounterGameMenuBehavior.game_menu_town_town_besiege_on_condition` — **private** | CRITICAL. `IsAtWarAgainstFaction(K,K)`=false → besiege option **hidden entirely**; the player cannot start a siege against C. (Same root: `…continue_siege…`, the sally-out and break-out conditions.) |
| **Raiding C's village proceeds** | `EncounterGameMenuBehavior.UpdateVillageHostileActionEncounter` + `game_menu_village_hostile_action_*` — **private** | CRITICAL. `!AttackerSide.MapFaction.IsAtWarWith(DefenderSide.MapFaction)` → redirected to a "you're being hostile to a non-enemy" warn menu instead of the raid. |
| **A mobile-party clash becomes a battle, not a merge** | `EncounterManager.StartPartyEncounter` / `HandleEncounterForMobileParty` — **static** | CRITICAL. Already in §4.2 (the same `MapFaction ==` merge), but it also silently absorbs a *player-adjacent* clash onto one side. |
| **C joins as enemy, not as a friendly joiner** | `EncounterManager.StartSettlementEncounter` (3rd-party join branch) — **static** | HIGH. `attacker.MapFaction == MainParty.MapFaction` calls `OnPartyJoinEncounter` on the **player's** side → C reinforces the player instead of opposing. |
| **Fight menu instead of army-meeting menu** | `PlayerEncounter.DoMeetingInternal` — **private** | HIGH. `!_defender.MobileParty.MapFaction.IsAtWarWith(MainParty.MapFaction)` routes to `army_encounter` (talk) instead of the fight menu when C leads an army. (`game_menu_army_attack_on_condition` likewise hides "Attack army.") |
| **C's port shows as hostile** | `EncounterGameMenuBehavior.naval_town_outside_on_init` — **private** | MEDIUM. Player docks peacefully at C's port (no "hostile" text, falls to `port_menu`). |
| **C's lords greet the player as enemies + an end-the-feud option exists** | `PlayerIsEnemyTag.IsApplicableTo` / `LordConversationsCampaignBehavior` (greeting, barter-peace) — tags / CampaignBehavior | HIGH (UX). Both clans are K, so C's lords use the **friendly** dialogue tree and **no barter-peace UI surfaces**. The engine's peace path cannot represent a private war → a **bespoke "end the feud" conversation/menu is required** regardless (it can't be a patch of the stock barter). |

The `EncounterAttackCondition` / "Attack!" and "Send troops" buttons (`MenuHelper`, static) are **FINE
as-is** — they check only "are there enemy troops in this map event" and the safe-passage timer, not
`IsAtWarWith`; once a map event is correctly created they work unchanged.

> **Confidence note.** The §4.3 line-quotes are reconstructed from 1.3.1 signatures and partially
> decompiled bodies; the *mechanism* (every check reduces to the MapFaction wall, §2) is certain, but
> the exact gating member for a few menu options should be confirmed at implementation time. This
> layer is **phase 4** (§15) — defer until the AI-vs-AI core is proven.

> **Implementation status — phase 4 slice 1 (player initiation, runtime-verified).** The four
> CRITICAL initiation gates are implemented and playtested: the player can declare a private war and
> besiege a same-kingdom rival.
> - `PlayerEncounter.SetupFields` → `PlayerEncounterSetupFieldsPatch` (postfix; flips player
>   Defender→Attacker when besieging a rival settlement). Registered as a normal `IPatch`.
> - `game_menu_town_town_besiege_on_condition` → `BesiegeMenuConditionPatch` (verified: "Besiege"
>   appears + siege starts).
> - `game_menu_town_besiege_continue_siege_on_condition` → `ContinueSiegeMenuConditionPatch`
>   (verified: "Continue siege preparations" appears).
> - `game_menu_army_attack_on_condition` → `ArmyAttackMenuConditionPatch` (implemented; not yet
>   exercised in a playtest).
>
> All four gate on `PrivateWarPatchHelper.AreEnemies(Hero.MainHero.Clan, rivalClan)`. **Critical
> gotcha:** the three `EncounterGameMenuBehavior` patches must NOT be applied at `OnSubModuleLoad`
> (the standard `IPatch`/`ApplyPatches` path). Harmony's apply forces that type's static ctor, which
> calls `GameTexts.FindText` before `GameTexts` is initialized → `TypeInitializationException` →
> crash on save load. They are applied once in `SubModule.InitializeGameStarter` (after
> `Game.Initialize`) via a dedicated `Harmony("com.dadg.private-wars-menus")` + a run-once guard.
>
> **Implementation status — phase 4 slice 2 (player raid + town access, runtime-verified).**
> - `VillageHostileActionCampaignBehavior.game_menu_village_hostile_action_on_condition` →
>   `VillageHostileActionConditionPatch` (postfix; shows "Take a hostile action" on the village menu
>   when the owner is a registered private-war enemy). Deferred along with the other menu patches.
> - `VillageHostileActionCampaignBehavior.game_menu_village_hostile_action_raid_village_on_condition`
>   → `VillageRaidConditionPatch` (postfix; shows "Raid the village" inside the village_hostile_action
>   submenu). Deferred along with the other menu patches.
>   **Note:** both methods are in `VillageHostileActionCampaignBehavior`, NOT `EncounterGameMenuBehavior`.
> - `SettlementAccessModel.CanMainHeroEnterSettlement` → `DadgSettlementAccessModel` (clean model
>   override; returns `NoAccess/HostileFaction` for the player trying to enter a private-war rival's
>   town or castle). Registered via `campaignGameStarter.AddModel(...)`.
>   **Note:** the private dispatch helpers (`CanMainHeroEnterTown`/`…Castle`) check
>   `DiplomacyHelper.IsSameFactionAndNotEliminated` which is true same-K. The override intercepts
>   before the dispatch and returns early, so the helpers are never reached for private-war pairs.
>   Town/castle access blockade is implemented; village access and keep-internal are not (not needed
>   for raid — the `VillageHostileAction*` patches handle the village hostile-action gate separately).
>
> The two village patches gate on `PrivateWarPatchHelper.AreEnemies` (Harmony layer); the access
> model — being in the MAIN project with no Harmony reference — gates on the equivalent domain service
> `FeudalServices.PrivateWarHostility.AreEnemies(stringId, stringId)`.
>
> Build: 0 C# errors. **Runtime-verified PASS** (privatewartest save): player declared a private war
> vs same-kingdom `clan_percy`; at Percy's village (Morpeth) both "Take a hostile action" and "Raid
> the village" appeared/enabled (Postfix confirmed `AreEnemies=true`); at Percy's castle
> (Dunstanburgh) the menu showed "cannot enter — belongs to the enemy" with no peaceful entry. Clean
> load, no `TypeInitializationException`, no CLR exceptions. Port hostility
> (`naval_town_outside_on_init`) remains deferred.
> End-to-end menu behaviour (village raid buttons, town NoAccess) requires human map navigation to
> verify.
>
> Still deferred: `UpdateVillageHostileActionEncounter` warn-redirect (1.3.1 false-branch is a
> Release-stripped `Debug.FailedAssert` — harmless, no patch needed per §17 notes), 3rd-party join,
> `DoMeetingInternal` army-meeting routing, port hostility (`naval_town_outside_on_init` reads
> `MapFaction.IsAtWarWith` directly — needs a separate deferred patch), and the bespoke "end the
> feud" conversation (§4.3 last row).
>
> **Implementation status — phase 4 slice 3 (player-captivity retention guard, implemented).**
> - `PlayerCaptivityCampaignBehavior.CheckCaptivityChange` → `PlayerCaptivityRetentionPatch` (prefix;
>   skips the method body while `AreEnemies(mainHero.Clan, captorClan)` is true, preventing the
>   `!IsAtWarAgainstFaction && same-MapFaction` branch from routing the player to
>   `menu_captivity_end_no_more_enemies`). Registered as a normal `IPatch` in `DadgServiceContainer`
>   (safe to apply at `OnSubModuleLoad` — `PlayerCaptivityCampaignBehavior` has no `GameTexts`
>   static-initializer trap). When the war concludes `AreEnemies` returns false and vanilla resumes,
>   releasing the player through the standard "no more enemies" path. Player-initiated escape via menu
>   consequences (`EndCaptivityAction.ApplyByEscape`) is unaffected. TEMP debug command
>   `campaign.capture_player <captorClanId>` added to stage the player as prisoner.
>
> **Slice 3 (crime→phantom-war guard) — NON-ISSUE (no patch needed).**
>   `CrimeRatingChangeAction.ApplyInternal` (1.3.1) guards the `DeclareWarAction.ApplyByCrimeRatingChange`
>   call with `Hero.MainHero.MapFaction != faction` — where `faction` is the `IFaction` (the kingdom)
>   against which crime was accumulated. For a same-kingdom private war the rival clan's `MapFaction`
>   IS the player's kingdom, so `faction == Hero.MainHero.MapFaction` → the guard condition is
>   false → `DeclareWarAction` is never called. No phantom war, no escalation, no patch needed.

### 4.4 Cosmetic / deferrable

| Item | Host | Note |
| --- | --- | --- |
| Friendly map colors between belligerents | `MapEntityVisual.IsAllyOf` (SandBox **view** layer, `virtual`) | No campaign-logic `IsAllyOf` exists (campaign uses `IsAtWarWith` inline). View-layer only; cosmetic. |
| Target-circle color green (friendly) not red | `SandBox.View.Map` cursor/target tint — **view layer** | Player has no visual cue that C is hostile; postfix the color flags for a registered pair. Cosmetic but worth doing for the player path. |
| Encyclopedia "Enemies" list omits the belligerent | `EncyclopediaFactionPageVM` | UI accuracy only. |
| Workshop seize-compensation / clickability; NPC marriage-blocking; raid-log "realms at war" flag | various CampaignBehaviors | LOW; each is a design choice (block marriages? compensate workshops?) not a correctness bug. |
| "Ally thanks" conversation lines | `conversation_ally_thanks_*` dialog conditions | Immersion polish; deferrable. |

> **UIExtender eligibility (cosmetic layer only).** The **ViewModel-backed** rows above —
> nameplate enemy/ally coloring (`PartyNameplateVM` etc.) and `EncyclopediaFactionPageVM` — can be done
> as `UIExtenderEx` `ViewModelMixin`s instead of raw Harmony, which buys conflict-resistance with other
> UI mods and a cleaner API (a mixin is still Harmony underneath, so it does not lower the patch count).
> Everything else is out of UIExtender's reach: `MapEntityVisual.IsAllyOf` and the `SandBox.View.Map`
> cursor tint are non-VM view code, the §4.3 besiege/sally/break-out gates are the **GameMenu** system
> (extend via native `CampaignGameStarter.AddGameMenuOption`, not UIExtender), and all of §4.2 is
> campaign-logic/game-model with no UI surface. Net: UIExtender touches **only** these 2 cosmetic rows,
> not the load-bearing count.

### 4.5 Tally and strategic read

**8 clean model overrides** (2 "heavy" — full re-implementation) + **~18 Harmony patches** across the
AI/state layer (§4.2) and the player-only encounter/menu layer (§4.3), plus cosmetic items (§4.4) that
are mostly deferrable. Four facts worth stating plainly:

1. **The central hostility signal is Harmony-only.** `IsEnemy` and `CalculateStanceScore` — the
   methods that actually decide "does the AI treat C as a target" — are private helpers, not model
   surface. The "zero-Harmony feudal-core" convention is genuinely broken at the most load-bearing
   point, not just the periphery. This is a conscious, documented exception for private wars.
2. **`GetTargetScoreForFaction` is a force multiplier.** One clean model override covers siege/raid
   targeting *and* most of the kingdom-relief damping, so the scariest item from the relief analysis
   (patching a kingdom-wide AI behavior) is largely a model change — materially de-risking the
   "private war stays private" concern.
3. **Two independent mechanisms auto-*end* the war if unpatched.** `PrisonerReleaseCampaignBehavior`
   (AI prisoners) and `PlayerCaptivityCampaignBehavior.CheckCaptivityChange` (the player) both read
   "no longer at war" off the faction wall and free captives — so any prisoner taken in a private war
   evaporates on the next common trigger. These two are the highest-severity correctness patches after
   the core hostility signal, because they don't just degrade the war, they silently terminate it.
4. **The player path is a clean phase boundary.** Everything in §4.3 (plus the player half of §4.1)
   is dormant unless the player's clan is a belligerent. The AI-vs-AI war is fully functional without
   any of it, so it can ship as a later phase (§15 phase 4) without blocking the core.

---

## 5. The war must be DADG-driven

In a cross-kingdom war the engine's strategic AI *autonomously* plans sieges and forms armies because
its planners see two different MapFactions at war. In stay-in-kingdom, those planners **short-circuit
on `MapFaction` equality before our §4 consumer patches are reached** — the attacker's kingdom-war AI
never proposes besieging a fellow K member's town, and `GetTargetScoreForFaction` returns 0 for a
same-faction target until our override changes it. The §4 patches make a battle *resolve correctly
once it starts*, but something must *start* it.

Therefore **DADG drives prosecution**:

- **Attacker intent.** On setup/daily tick, DADG directs the attacker toward the goal settlement —
  the cleanest lever is the `GetTargetScoreForFaction` override (4.1) returning a high score for the
  goal for the attacker's parties, so stock army/siege AI then picks it up. If that proves
  insufficient (§15 risk #1), explicitly form an army for the attacker with the goal as siege target.
- **Score tracking is event-sourced.** Because A and C share MapFaction K, **there is no
  `StanceLink(A, C)`** to read casualties/sieges/raids from (the temp-kingdom design got those for
  free). DADG must instead accumulate them from campaign events filtered to the pair (§6.1).
- **Resolution is DADG-decided.** The engine has no notion this war exists, so |S| ≥ 100 / white
  peace is evaluated by DADG, which then applies the prize/revert and unregisters the pair.

This is the structural cost of the substrate: stay-in-kingdom buys native membership at the price of
**owning the war's drive loop**. It is bounded (one daily tick + a handful of event handlers) and
testable in isolation (§11 domain calculator).

---

## 6. War score model

A **single signed score** `S ∈ [−100, +100]`, attacker-positive. **Attacker wins at `S ≥ +100`,
defender wins at `S ≤ −100`.** (Displayable as two opposing bars; the signed form captures every
clause of the spec cleanly.)

`S` has two parts: an **objective** term (instantaneous, from world state) and a **fatigue** term
(accumulating, sign set by who holds the goal). Fatigue is what guarantees termination.

### 6.1 Objective term (recomputed each tick)

Because the pair shares a MapFaction, **every counter is sourced by DADG from campaign events
filtered to the (A,C) pair**, not from a `StanceLink` (which does not exist for the pair — see §5).

| Source | Effect on `S` | Read from |
| --- | --- | --- |
| Attacker **holds the main-goal settlement** | large `+` (≈ +30) | `goalSettlement.OwnerClan == attacker` (de facto) |
| Defender holds the goal (default) | 0 baseline | ditto |
| Each **secondary** defender fief the attacker holds | small `+` (≈ +10 each, stacks) | ownership vs snapshot |
| Each **attacker** fief the **defender** has captured | `−` (≈ −10 each) | ownership vs snapshot |
| Battle impact (casualties inflicted) | `±`, proportional to differential | DADG-tallied from `MapEvent` results where the two sides are A-parties vs C-parties |
| Successful sieges / raids | `±` | DADG-tallied from `OnSiegeCompleted` / village-raid events filtered to the pair |
| Enemy heroes imprisoned | `±` | `HeroPrisonerTaken` filtered to the two clans |

The main goal is the **dominant** driver and, crucially, sets the **direction of fatigue**.
Secondary captures, battles, raids and prisoners are bumps stacked on top (the user's "secondary
goals just stack war score"; "if the defender takes attacker fiefs it also counts").

### 6.2 Fatigue term (the forcing function)

```
dS_fatigue/day = k · sign(attacker holds the main goal ? +1 : −1)     // uncapped
```

- Attacker **holds** the goal → fatigue drifts `S` toward **+100** → attacker wins eventually.
- Attacker does **not** hold the goal (defender holds it, *or a third party does* — §9) → fatigue
  drifts `S` toward **−100** → **attacker loses** (classic exhaustion: you failed your casus belli).

Because fatigue is **uncapped**, `|S|` necessarily reaches 100 — the war always terminates, in favor
of "the side accomplishing the main goal." `k` may ramp with war duration so stalemates end faster.

**Epoch resets when the goal changes hands.** Fatigue accrues over `daysElapsed = currentDay −
GoalLastTakenDay`, *not* from the war's `StartDay`. Each time the main goal is captured (attacker
takes it, or defender reclaims it — §10), the holder's `GoalLastTakenDay` is stamped to "now", wiping
the drift the previous holder had banked. A capture therefore lands `S` near the objective value
(goal ±50) rather than jolting it, and the new holder must then hold the goal for a fresh ~50 days to
drive `S` to ±100. This is what gives the dispossessed side time to retake the goal before the war
resolves.

### 6.3 Outcomes

- **`S ≥ +100` — attacker victory:** attacker gets the **prize** (the claimed dignity + its seat,
  §7); all *other* captures revert (§8).
- **`S ≤ −100` — defender victory:** attacker **loses the casus belli** (claim removed/weakened);
  **all** captures revert.
- **White peace** (negotiated exit near `S ≈ 0`, or mutual AI desire): "nothing happens except the
  attacker loses its casus belli" — all captures revert; claim removed/weakened. Same net effect as
  a defender victory minus the relation/prestige consequences.

---

## 7. Casus belli framework

The casus belli is the abstraction neither vanilla nor Diplomacy has. It defines **the goal**, **the
prize**, and **what stacks** — and it is the single extension point for future war types. It is
**substrate-independent**: it only ever asks "the goal," "the secondaries," "the prize."

```csharp
// Domain — pure, no TaleWorlds
public interface ICasusBelli
{
    string Type { get; }                                   // "Claim" for v1
    bool IsValid(PrivateWarContext ctx);                   // claim still exists, etc.
    string GetMainGoalSettlementId(PrivateWarContext ctx); // the settlement whose control drives fatigue
    IReadOnlyList<string> GetSecondaryFiefIds(PrivateWarContext ctx);
    PrizeAward GetPrize(PrivateWarContext ctx);            // what transfers on attacker victory
    void OnAttackerDefeatOrWhitePeace(PrivateWarContext ctx); // strip/weaken the CB
}
```

**v1 — `ClaimCasusBelli`** (the only implementation):
- **Defender** = the clan currently **owning the claimed title's seat** (de facto holder). Pressing
  a claim means attacking whoever physically holds it; this keeps "holds the main goal" well-defined.
- **Main goal** = `Title.SeatSettlementId` (county → county town; duchy → ducal capital; kingdom →
  royal capital — the seat of the title at the claimed rank).
- **Secondary fiefs** = the defender's other fiefs (capturing them stacks score; they revert).
- **Prize (attacker victory)** = the claimed **title** + its **seat**, applied through the
  **already-implemented** transfer path: `AssignTitleUseCase.Execute(seatId, attackerId,
  SeatTransferKind.Conquest, day)` for the seat, plus the dignity transfer (attainder/AssignTitle).
- **On defeat/white peace** = `IClaimRepository.RemoveClaim` (or downgrade `ClaimStrength`).

**Dovetail with the de jure/de facto split (already in code):** when the attacker captures the goal
town mid-war, the stock `ChangeOwnerOfSettlementAction.ApplyBySiege` fires (real ownership moves even
within a kingdom — the §4 patches make the siege resolve, but the *ownership transfer itself* is
unguarded), and the existing `FeudalTitleCampaignBehavior` already marks the title
**contested-occupied** by the attacker (`OccupantClanId` + `ContestedSinceDay`). On **attacker
victory** we finalize the **dignity** (`HolderClanId`) to the attacker → title uncontested under the
attacker. On **defeat/white peace** the seat reverts → occupant clears → title uncontested under the
defender. The private war is, in effect, *the real-war driver for machinery the title system already
has.*

**Extensibility:** future CBs (independence, duchy conquest, subjugation, restoration) implement the
same interface with different goal/prize/secondary logic. The registry/patch/score machinery is
entirely CB-agnostic.

---

## 8. Settlement revert — status quo ante except the prize

Real sieges change **real** ownership (even between two clans of the same kingdom), so a private war
can shuffle fiefs that must be undone unless the attacker won the prize. We mirror Diplomacy's
`ConsolidateKingdomsAction` revert pattern:

- **At setup:** snapshot `OriginalFiefOwners : settlementId → ownerClanId` for all fiefs of both
  belligerents (and the goal).
- **At resolve:** revert **only settlements whose ownership moved between *this war's own two
  belligerents*** (current owner is this war's attacker and snapshot owner its defender, or vice
  versa), via `ChangeOwnerOfSettlementAction.ApplyByDefault(originalOwner.Leader, settlement)` —
  **except**:
  - on **attacker victory**, the **goal/prize** stays with the attacker (transferred via the CB
    prize path, not reverted);
  - skip fiefs now owned by a **third party** (owner outside this war's pair) — not ours to move;
  - skip if the original owner is eliminated.

Net effect matches the spec: white peace / defender win → "nothing happens" (full status quo ante);
attacker win → attacker keeps **only** the war goal, everything else reverts (CK3 semantics).

---

## 9. Concurrent wars: a clan in several at once

In stay-in-kingdom this is **much simpler than the temp-kingdom design** because there are no faction
objects to share, refcount, or peace. A clan stays in K throughout; participation in several private
wars is just several entries in the pair registry. The two orchestration traps that dominated the
temp-kingdom design (**teardown trap** — destroying a shared faction too early; **separate-peace
trap** — neutrality enforcement silently peacing another concurrent war) **do not exist here** — there
is nothing shared between a clan's wars except the clan itself.

**Scoring stays isolated per pair (the "black and white" rule):**

- Each war is its own `PrivateWar` record over an ordered pair, scored from **its own**
  event-sourced counters filtered to that exact pair (§6.1). Two wars sharing clan C
  (C-vs-A and C-vs-B) accumulate into two separate scoreboards; casualties/sieges/raids never
  cross-contaminate because each event is attributed by the *two clans actually involved in it*.
- **Cross-belligerent battles don't count in either war.** If A and B both fight C and also fight
  *each other*, that combat is attributed to the (A,B) pair — which has no `PrivateWar` record — so
  it scores in neither war against C. (The user's "both attackers can battle each other, not counting
  to the war score in either war against the defender.")
- **A third party taking the goal makes the attacker lose.** Score reads *who currently owns the
  goal*, not "did the defender lose it." Same mechanism whether the third party is in another private
  war or a normal kingdom war: attacker doesn't hold the goal → fatigue → −100.

**Revert stays isolated per pair.** Each war reverts (§8) **only** settlements whose ownership moved
*between its own two belligerents*. A fief clan B captured from C in war-2 is a *third party* to
war-1 (A⚔C), so war-1's revert skips it; war-2 handles it on its own resolution. The shared-goal case
resolves coherently: if A and B both claim C's title T and A captures the seat S, war-1 (A⚔C) awards
S+T to A; war-2 (B⚔C) sees S now owned by A (third party to war-2) → B fails the goal → B loses the
CB, and war-2's revert leaves S with A. One title, one winner, no special-casing.

**One hard rule — at most one private war per *unordered clan pair*.** In the temp-kingdom design this
was *forced* by the single `StanceLink` between two factions. In stay-in-kingdom there **is** no
StanceLink for a same-K pair, so the constraint is instead **enforced by our registry** (keyed by
unordered pair) and by the §4 patches (which can only return a single binary "are these two clans
privately at war" answer). The rationale is the same: two CBs between the same two clans would share
one manufactured-hostility relationship and one event-attribution channel, so a given battle couldn't
be assigned to one CB versus the other. A clan in many wars against *different* opponents is fine
(distinct pair → distinct registry entry); two wars between the *same pair* (including role-swapped
A⚔C and C⚔A) is not — queue it, or fold into a multi-goal CB later.

**Consequence worth flagging — and a stay-in-kingdom *improvement* over ejection:** because the clan
never leaves K, it **continues to fight K's regular (external) wars** while prosecuting a private feud
(§12). The ejection/temp-kingdom designs made a feuding clan vanish from its kingdom's front; stay-in
keeps it present. This satisfies the hard constraint ("a clan totally out of the loop of its kingdom
is a big no-go") *by construction*, and removes the "dodge kingdom-war duty by declaring a cheap
private war" exploit that the earlier designs had to balance-test.

---

## 10. War lifecycle (state machine)

```
                 declare                                   score |≥100| or white peace
   (none) ───────────────────────▶ Active ──────────────────────────────────▶ Resolved
            registry.Add(pair)        │  §4 patches treat A↔C as enemies         │  apply prize/revert (§7/§8)
            snapshot fiefs (§8)       │  DADG drives siege (§5)                  │  registry.Remove(pair)
            persist record (§11)      │  track score from events (§6)           │  clan never left K
```

**Trigger.**
- *AI:* hooks the existing petition-deny path. `FeudalPetitionDecision` → `DenyClaimOutcome` already
  surges tension on denial (`SurgeTensionOnDenial`, ×5.0). After denial, a claimant clan with a
  strong claim, sufficient strength, and tension over a threshold may escalate to a private war for
  that claim. (Gate on cooldown + a not-already-at-private-war-with-this-clan check.)
- *Player:* a menu/dialog option "Press your claim by force" against the clan holding the claimed
  seat. Same precondition checks.

**Setup (atomic).** Snapshot `OriginalFiefOwners` for every fief of both clans + the goal (§8);
register the ordered pair in the hostility registry; persist a `PrivateWar` record (§11). **No
kingdom is created, no clan leaves, no influence is touched** — the empty-kingdom and rejoin guards
of the old design are gone.

**Prosecute (daily tick + event handlers).** Drive attacker intent toward the goal (§5); accumulate
score counters from filtered campaign events (§6); check resolution.

**Resolve.** Decide winner by score sign, or white peace. Apply prize or revert (§8). **Unregister
the pair** — the §4 patches revert to stock and A↔C are friendly again. Mark the record resolved.

This is a saveable state machine; the registry and records sync like the existing feudal registries so
a mid-war save/load is safe (§14). On load, the registry *is* the source of truth — the §4 patches
read it directly, so there are no faction objects to reconcile or heal.

---

## 11. Domain & integration mapping

Follows the hexagonal layout and the `FeudalServices` static-locator pattern already in use.

**Domain** (`src/DellarteDellaGuerra.Domain/PrivateWars/`) — pure, unit-testable:
- `PrivateWar` record (one per ordered belligerent pair): `Id`, `AttackerClanId`, `DefenderClanId`,
  `CasusBelliType`, `TitleId`, `MainGoalSettlementId`, `OriginalFiefOwners`, `Score`, `StartDay`,
  `Status`. **No `WarFooting`/temp-kingdom/refcount state** — the clan never leaves its kingdom, so
  the only per-clan state is "which pairs is this clan in," derived from the records themselves.
- `ICasusBelli` + `ClaimCasusBelli` (§7).
- `PrivateWarScoreCalculator.Compute(observations, daysElapsed) → S` — pure function of fed-in
  observations (goal owner, fief deltas, casualty/siege/raid counts, prisoners, days). The testable
  heart of §6; **zero** TaleWorlds types.
- Use cases: `DeclarePrivateWarUseCase`, `TickPrivateWarUseCase` (updated score + resolution
  decision), `ResolvePrivateWarUseCase` (prize/revert instructions).
- Ports: `IPrivateWarRepository` (Get/Add/Remove/GetAll, GetByClan), and the pair-membership query
  the patches consult — `IPrivateWarHostility.AreEnemies(clanIdA, clanIdB)` (+ role lookup).

**Integration / engine** (`DellarteDellaGuerra/PrivateWar/`):
- `PrivateWarCampaignBehavior` — registers triggers (petition-deny escalation, player menu), drives
  the daily tick (attacker intent + score from events), and executes setup/resolve via the Actions
  in §8/§10. Owns the score event-sourcing (MapEvent results, siege/raid completion, prisoner taken)
  filtered to active pairs. Also owns **registry maintenance** that the engine won't do for a
  registry-only war: an `OnClanChangedKingdom` hook (§14) to persist/prune/convert a pair when a
  belligerent moves kingdom, and the **bespoke "end the feud" conversation/menu** (§4.3) — the engine's
  barter-peace path can't represent a private war, so the negotiated-exit UI is ours, not a patch.
- `PrivateWarHostilityRegistry` (implements `IPrivateWarHostility`) — the in-memory, save-synced set
  of active pairs. **This is the hot path:** every §4 patch calls `AreEnemies(...)` per hostility
  check, so it must be an O(1) lookup keyed by unordered clan-id pair.
- **`FeudalServices` gains a `PrivateWars` property and a `PrivateWarHostility` property** threaded
  through `Initialise(...)`/`Reset()`. The §4 model overrides and Harmony patches reach the registry
  **only** through `FeudalServices` (static locator) — required because models and patches can't take
  constructor injection. Patches must null-guard the locator (private wars inactive / not yet
  initialised → fall through to stock).
- **Model registration:** the 6 overrides in §4.1 are registered via `CampaignGameStarter.AddModel`
  in the module's game-start path, each subclass wrapping the stock `Default…Model` and delegating to
  it when `AreEnemies` is false.
- **Harmony:** the ~12 patches in §4.2 live in a dedicated `PrivateWar/Patches/` area, each a thin
  prefix/postfix that consults `FeudalServices.PrivateWarHostility` and otherwise no-ops. **This is a
  documented, deliberate exception to the zero-Harmony-in-feudal-core convention**, justified by §2
  (the central hostility signal `IsEnemy` is a private helper with no model seam).
- Persistence: extend `IFeudalStateStore` with an Initialise/Snapshot pair for private wars;
  `FeudalStateStoreAdapter` delegates to a new `InMemoryPrivateWarRegistry`; the behavior calls
  `dataStore.SyncData("DadgPrivateWars", ref _serialised)` with a pipe-delimited serialiser mirroring
  `TitleStateSerialiser`. **No new `SaveableTypeDefiner` ids** for the record itself (strings only) —
  but any new `KingdomDecision`/`DecisionOutcome` subclasses introduced for the player-facing flow
  consume the **next free `FeudalElectionSaveDefiner` ids: 8, 9, 10…** (1–7 are used:
  petition/claimant/grant/deny/attainder/attaint/uphold).

---

## 12. Kingdom membership preserved natively (the resilience win)

This is the payoff that motivated the substrate choice. Because each belligerent **remains a formal
member of its kingdom**, every kingdom-membership behavior works **with no code from us** — and keeps
working as TaleWorlds extends kingdoms:

| Membership benefit | How it's preserved | Ejection/temp-kingdom would have had to… |
| --- | --- | --- |
| **Policies** | Clan stays under K's enacted policies | mirror enacted policies onto the temp faction and re-apply on return |
| **Tributes / income** | K's tribute and budget flows are unbroken | snapshot and restore tribute agreements |
| **Shared diplomacy** | A still fights K's external wars and shares K's peace (it never left) | leave A out of K's wars, then re-sync stances on return |
| **Kingdom decisions / votes** | A still proposes and votes in K's `KingdomDecision`s | exclude A from votes, lose its voice mid-feud |
| **Encyclopedia identity** | A shows under K (no phantom kingdom) | relabel a temp kingdom in the UI to hide it |
| **Settlement display** | A's fiefs fly K's banner and count as K's | re-map settlement faction display |
| **Defense vs. external enemies** | K still relieves A's fiefs against *outside* attackers (relief suppression is **pair-scoped** — only the private siege by C is ignored, §4.1) | re-establish defensive coverage for an ejected clan |
| **Future kingdom features** | **Inherited automatically** — nothing to patch | be patched for each new feature, indefinitely |

The last row is the decisive one: the ejection mirror surface is **unbounded over the maintenance
timeline**, while the stay-in-kingdom patch surface (§4) is **bounded and stable** (combat
side-assignment is a part of the engine that has not materially changed across versions). The hard
constraint — *a feuding clan must not be out of the loop of its kingdom* — is satisfied **by
construction** rather than by mirroring.

---

## 13. Rejected alternatives

Both were viable substrates that were designed out in earlier passes; their reasoning is preserved
here so the decision is auditable.

### 13.1 Rejected — dual temporary splinter kingdoms (this document's earlier design)

Eject *both* belligerents into one-clan "war-footing" kingdoms at war only with each other, neutral
to all else (the Diplomacy civil-war machinery with a clan-vs-clan topology), then rejoin and destroy
the temp kingdoms on resolution. Verified achievable with **zero Harmony** (official campaign Actions
only), and it inherits stock **kingdom-level** war AI (so the attacker AI can actually plan and
execute the goal siege — the one thing stay-in-kingdom has to drive itself, §5).

**Why rejected:** it still ejects the clan from its kingdom for the war's duration, inheriting the
**unbounded membership-mirror problem** (§12) — policies, tributes, diplomacy participation, votes,
display all have to be snapshotted and restored, and that surface grows with every future kingdom
feature. It also created genuine orchestration complexity that stay-in-kingdom eliminates entirely:
per-clan **refcounted** temp kingdoms, a **war-footing invariant** to prevent concurrent wars from
peacing each other, an **empty-kingdom guard**, **rejoin/heal-on-load** logic, and a `WarFooting`
record. The AI-siege advantage is real but does not outweigh the membership-mirror cost, and stay-in
recovers most of it through the `GetTargetScoreForFaction` override (§4.1, §5).

### 13.2 Rejected — lone-clan ejection

Make the belligerent kingdomless (its own MapFaction) for the duration — lighter than two temp
kingdoms, and hostility comes "for free" (different MapFactions). Probed in depth:

- **Autonomy must be suppressed.** A kingdomless clan's join/war/peace is driven solely by
  `DiplomaticBartersBehavior.DailyTickClan` — it would try to join another kingdom (incl. as
  mercenary), declare its own wars, and make its own peace. Suppression needs a `DiplomacyModel`
  override for joining **plus** a Harmony prefix on `DailyTickClan` to kill autonomous join/war/peace.
- **Existential bug:** `FactionDiscontinuationCampaignBehavior.CanClanBeDiscontinued` gives a
  **landless** kingdomless noble clan a **28-day countdown to `DestroyClanAction`**. A belligerent
  that loses its last fief mid-war would be *deleted*. Requires a guard.
- **Degraded membership:** garrison wage-limit and auto-recruit silently no-op for a non-kingdom
  MapFaction (`if (!clan.MapFaction.IsKingdomFaction) return;`).
- **Targetability:** a lone clan is structurally invisible to autonomous war-declaration and
  separate-peace AI (`GetRandomWarDecision`/`GetRandomPeaceDecision` draw from `Kingdom.All` only) —
  convenient for containment, but it also means the lone clan can't be a normal diplomatic actor and
  would need scoring overrides to be targetable *only* by valid claims.

**Why rejected:** same **unbounded membership-mirror** problem as §13.1 (it's the same ejection
cost), **plus** a worse autonomy/safety surface (the 28-day discontinuation timer is a latent clan
deletion) and degraded kingdom services. Strictly dominated by both other options.

---

## 14. Edge cases

Several edge cases from the earlier (temp-kingdom) design **no longer apply** because no faction is
created, destroyed, or left: empty-home-kingdom guard, home-kingdom-destroyed-mid-war rejoin,
teardown/separate-peace traps, `ApplyByKingdomCreation` war inheritance, army-detachment-on-
`ChangeKingdom`, and the "player temporarily leaves their kingdom" UX. The remaining stay-in-kingdom
cases:

| Case | Handling |
| --- | --- |
| **Both belligerents in the same kingdom** | The core case; handled by construction — pair-scoped patches make exactly A↔C hostile, everyone else (incl. K's other clans) sees stock stances. |
| **Belligerents in different kingdoms / one or both independent** | Same code path — the registry/patches are clan-grained, so cross-kingdom and independent pairs work identically (the only difference is that for a cross-kingdom pair the stock engine *also* sees them at war; the patches simply agree). |
| **K declares an external war while A is in a private war with fellow-member C** | A fights K's external war normally (it never left K). Patches are pair-scoped, so A↔C hostility never bleeds into K's external side-assignment. |
| **K's army AI tries to pool A and C into one army** | The `GetMobilePartiesToCallToArmy` override (§4.1) excludes the opposing belligerent from each other's army candidate pool. |
| **Second claim against a clan already privately at war with the claimant** | Disallowed: at most one private war per *unordered clan pair* (the registry/patches can only express one binary hostility per pair, and events can't be attributed across two CBs). Queue it or fold into a multi-goal CB later (§9). A second claim by a *different* clan is fine. |
| **Belligerent clan eliminated mid-war** | This war voids; surviving side treated as walkover (attacker win if defender eliminated and attacker holds goal; else white-peace cleanup). Unregister the pair. No prize if the attacker is the one eliminated. |
| **Belligerent changes kingdom mid-war** | The registry war is invisible to `ChangeKingdomAction`'s `FactionsAtWarWith`-driven join/leave cascade (§4.2), so nothing auto-syncs. DADG hooks `OnClanChangedKingdom`: the pair **persists** by default (the feud is clan-vs-clan, not kingdom-vs-kingdom) — but if the move makes the pair *cross-kingdom* and the two kingdoms are now formally at war, fold the private war into that war or resolve it. Also re-evaluate `GetMainGoalSettlementId` if the goal's de-facto holder changed. |
| **Attacker's feud army dissolves** | The cohesion/disband guard reads `FactionsAtWarWith` (blind to the registry war), so an army raised for the feud auto-disbands. The §4.2 disband patch keeps it alive while the pair is active; on resolve it disbands normally. |
| **Leader captured** | War continues (DADG drives it); capture feeds the score as a prisoner event. The `PrisonerCapture`/`PrisonerRelease` patches (§4.2) keep the captive held despite same-MapFaction. |
| **Player is a belligerent** | Player's clan fights C with real sieges while *staying in its kingdom* — keeps allies, policies, votes. Needs clear messaging that this is a private feud, not a kingdom war. |
| **Player is the suzerain watching two vassals feud** | Notification; (future) mediation hook. The relief-damping (§4.1) stops the player's other AI vassals from being dragged in. |
| **Player is an uninvolved kingdom member** | Stock stances apply to them (pair-scoped), so they are not pulled into the feud; cosmetic guards (§4.3) reduce immersion breaks. |
| **Save/load mid-war** | The registry and `PrivateWar` records `SyncData` like existing feudal registries; on load the registry *is* the source of truth the patches read — no faction objects to reconcile. |

---

## 15. Open risks to verify in-game

These cannot be settled from decompiled signatures (bodies are stripped) and need a GABS smoke test:

1. **AI siege-seeking under DADG drive (highest risk).** Will boosting `GetTargetScoreForFaction`
   (§4.1, §5) for the attacker's parties actually route stock army/siege AI to besiege the goal, or
   does the same-faction short-circuit reassert somewhere downstream? If weak, fall back to explicitly
   forming an army for the attacker with the goal as siege target. **Test first; this decides how much
   DADG drive is needed.** (This is the cost stay-in-kingdom pays that the temp-kingdom design did not
   — temp kingdoms inherited kingdom-grade war AI directly.)
2. **Relief damping sufficiency.** Confirm the `GetTargetScoreForFaction` override alone keeps
   uninvolved K lords from gathering to "relieve" the private siege; if it leaks, add the
   `AiMilitaryBehavior.CalculateMilitaryBehaviorForSettlement` Harmony fallback (§4.2).
3. **Defender-side composition.** Confirm the `EncounterModel.GetDefenderParties…` override lets A's
   own field army (not just the static garrison) defend A's besieged fief, and that it does **not**
   accidentally pull in uninvolved K parties. **Verified (2026-06-24):** garrison, militia, and
   feud-belligerent lord parties admitted across all three defender seams (player *and* AI besieger);
   neutral same-kingdom parties excluded. Sally-out and AI-vs-AI both PASS. Militia are now excluded from
   the `SallyOut` set to match vanilla (resolved 2026-06-24); they still defend the walls in an assault.
   See the §4.1 status note and `doc/features/private-wars-siege-defenders.feature`.
4. **Patch hot-path cost.** `IsEnemy`/`CalculateStanceScore` and the per-tick model overrides run
   constantly; verify the `AreEnemies` registry lookup is genuinely O(1) and the patches no-op cheaply
   when no private war is active.
5. **Ownership-transfer side effects.** Real same-kingdom siege capture fires
   `ChangeOwnerOfSettlementAction` paths never normally exercised intra-kingdom; smoke-test that fief
   capture, contested-title marking (§7), and re-besiege (after the §4.2 patch) all behave.

**Suggested phasing.** (1) Domain: `PrivateWar`, `ClaimCasusBelli`, `PrivateWarScoreCalculator` with
unit tests for §6/§8 logic — pure, no engine. (2) Registry + the model overrides (§4.1) + the
core hostility Harmony patches (`IsEnemy`, `StartPartyEncounter`, `StartSettlementEncounter`,
`CanPartyJoinBattle`) **plus the feud-army disband guard** (§4.2) — enough for a battle and a siege to
*resolve* and an attacker army to *persist*; GABS-test risks #1–#3 AI-vs-AI. (3) State-leak patches
(AI-prisoner hold/capture, recruitment, visitation, `OnClanChangedKingdom` registry maintenance) +
revert/prize wiring. (4) Player path: trigger + the §4.1 player-menu model overrides + the §4.3
encounter/menu Harmony + the bespoke "end the feud" conversation + cosmetic guards (§4.4). The
auto-end patches split across phases by side: `PrisonerReleaseCampaignBehavior` is phase 3 (AI),
`PlayerCaptivityCampaignBehavior` is phase 4 (player).

---

## 16. Locked-in v1 parameters

From the Q&A; treat as fixed for v1:

- **Real, map-represented wars only** — no simulation. (Overrules Part B.)
- **Substrate:** belligerents **stay in their home kingdom**; hostility is manufactured per clan pair
  via 8 model overrides + ~18 Harmony patches (§4), of which the player-only encounter/menu layer
  (§4.3) is a deferrable later phase. Ejection and temp-kingdoms rejected (§13).
- **Participants:** clan vs clan (including two clans in the same kingdom). Hero-grade and ally/kin
  belligerents are future; the framework is extensible.
- **Casus belli:** Claim only, framework extensible (`ICasusBelli`).
- **Score:** single directional %, first to ±100 wins; **uncapped fatigue** whose direction is set
  by control of the CB's main-goal settlement; secondary captures, battles, raids and prisoners
  stack; defender capturing attacker fiefs counts against the attacker. Counters are **event-sourced
  by DADG** (no `StanceLink` exists for a same-kingdom pair).
- **Prize (attacker win):** the claimed dignity + its seat, via the existing
  `AssignTitleUseCase`/attainder path; everything else reverts.
- **White peace / attacker loss:** status quo ante; attacker loses the casus belli (claim
  removed/weakened).
- **Concurrency:** at most one private war per *unordered clan pair* (enforced by the registry, §9);
  a clan may be in many wars against different opponents and **continues to fight its kingdom's
  external wars** throughout (§12).

---

## 17. Version compatibility (1.3.1 → 1.4.6)

§1–§16 are written against **1.3.1**. This section records a source-grounded diff against **1.4.6**
(three-agent sweep of the substrate wall, the §4.1 model seams, and the §4.2/§4.3 patch points).

**Bottom line: the design holds unchanged on 1.4.6, with a *marginally smaller and cleaner* patch
surface — no new patch points, one fewer Harmony, and one AI-behavior gate refactored into a model
seam.** Porting is re-pointing ~4 renamed/moved members, not adding new ones.

### 17.1 Substrate wall — fully intact (the decisive result)

All three load-bearing facts in §2 are **byte-for-byte identical** in 1.4.6: `Clan.MapFaction =>
Kingdom ?? this`; `FactionManager.IsAtWarAgainstFaction`'s `f1==f2` short-circuit; and
`EncounterManager.StartPartyEncounter`'s `MapFaction ==` merge-vs-`StartBattleAction` branch. The
substrate decision (§1) therefore transfers to 1.4.6 without revisiting.

### 17.2 Deltas that need handling

| Area | 1.3.1 (this doc) | 1.4.6 | Effect on count |
| --- | --- | --- | --- |
| **Army candidate filter** (§4.1) | `ArmyManagementCalculationModel.GetMobilePartiesToCallToArmy` (abstract, returns list) | **GONE** — refactored to `CheckPartyEligibility(MobileParty, out TextObject)` + threshold properties on the model | Lateral: re-point the override to `CheckPartyEligibility`. **Still a clean model override.** |
| **Army-creation war gate** (was §4.2 Harmony) | inline `FactionsAtWarWith.AnyQ(x => x.Fiefs.Any())` in `AiMilitaryBehavior.AiHourlyTick` | moved to `DefaultArmyManagementCalculationModel.CanLordCreateArmy` (a **model** method) | **Harmony → model override.** One fewer Harmony, one more (easy) override. |
| **Feud-army auto-disband** (§4.2 / §14) | `Army.Tick()` → `DisbandArmyAction.ApplyByNoActiveWar` | **GONE entirely** — the `NoActiveWar` dispersion path no longer exists in 1.4.6 | **−1 Harmony.** Part of the disband concern evaporates. |
| **AI fief-visitation** (§4.2) | `AiBehaviorCampaignBehavior.IsSettlementSuitableForVisitingCondition`, narrow war-exception | moved to `AiBehaviors` namespace; war-exception **widened** (`\|\| mapFaction.Settlements.Count == 0`) | Same count; re-point + cover the wider clause. |
| **Player captivity auto-release** (§4.2) | `CheckCaptivityChange`: bare `!IsAtWarAgainstFaction(...)` | war-check now `&&`-compounded with a crime-rating condition | Same count; adjust for the compound guard. |
| **Village-raid warn redirect** (§4.3) | `UpdateVillageHostileActionEncounter` false branch switches to a warn-menu | false branch now hits a (Release-stripped) `Debug.FailedAssert` | Same: guard unchanged and still needs suppressing; the missed-patch consequence is now harmless. |

### 17.3 Confirmed SAME (no action)

Substrate (§17.1); the other **7 of 8** model seams in §4.1; the core hostility signal
(`DefaultMobilePartyAIModel.IsEnemy` / `CalculateStanceScore`, still private); `StartSettlementEncounter`,
`HandleEncounterForMobileParty`, `MapEvent.CanPartyJoinBattle`, `SiegeEvent.CanPartyJoinSide` /
`CheckSiegeEventContinuity`, `BeHostileAction`, `PrisonerRelease`, `PrisonerCapture`, `Recruitment`,
`SettlementHelper.FindNearest*`, `CrimeRatingChangeAction`; and **6 of 7** §4.3 player-path members
(`PlayerEncounter.SetupFields`/`DoMeetingInternal`, the besiege / sally-out / break-out conditions,
`PlayerIsEnemyTag.IsApplicableTo`, `ChangeKingdomAction.ApplyInternal`).

### 17.4 Unconfirmed — verify before porting

- `ChangeOwnerOfSettlementAction.ApplyInternal` besieger-abort sweep (§4.2): the 1.4.6 search backend
  could not surface the method body. Class and entry points (`ApplyBySiege`, `ApplyByBarter`) exist;
  the sweep needs **manual IL inspection** on 1.4.6 before relying on it.

### 17.5 New hostility consumers in 1.4.6

**None mandatory.** The new alliance mechanics (`StartAllianceDecision`,
`ProposeCallToWarAgreementDecision`) iterate `FactionsAtWarWith` but fire only on real
`DeclareWar`/`MakePeace`, which the design never calls for K — so they are inert for a registry-only
private war.

### 17.6 Net tally

Model overrides **8 → ~9**; Harmony **~18 → ~17**; ~4 members to re-point on upgrade. The 1.4.x
refactors moved two of our concerns *toward* clean override seams and deleted one patch, so 1.4.6 is a
slightly **better** target than 1.3.1, not a worse one.

---

## 18. Amendments — call-to-arms, sides, and scoring v2 (2026-06-15)

> Agreed after the substrate was locked; **supersedes the named parts of §6, §7, §9**. The substrate
> (§1–§5, §12) is unaffected — these amendments only change *who participates* and *how score is
> counted*, both of which sit inside the bounded combat layer. The build order lives in the companion
> `feudal-private-wars-implementation-plan.md`.

**A. Recursive call-to-arms — sides, not pairs (supersedes §9's "unordered pair" framing).**
A war has two **principals** (claimant ⚔ title-holder) and two **sides**: each principal plus its
entire (sub)vassal subtree, following the **DADG feudal title hierarchy** (not the Bannerlord kingdom),
exactly as kingdom call-to-arms works. Membership is **dynamic** — a clan's side is resolved by walking
**up** its suzerain chain to the **first belligerent principal** (nearest-belligerent-ancestor, so a
clan attacking its own liege keeps its subtree on its own side). The hostility registry becomes
side-set-keyed (cached, invalidated on hierarchy change); `AreEnemies(X,Y)` = "opposite sides of some
active war." Patch count unchanged. Concurrency rule relaxes to **one war per pair of principals (per
CB)**; vassals may be called into several wars at once. Stay-in-kingdom is *reinforced* by this — you
would never want to eject whole vassal trees.

**B. Scoring v2 (supersedes §6.1's objective table; §6.2 fatigue retained).**
Attacker-positive, clamped ±100. **Two kinds of term:** *state-derived* (recomputed each tick from
current world state) and one *accumulated* (battle flow, persisted on the record).

*State-derived (recomputed each tick):*
- **Main goal = 50** (flat, regardless of town/castle), **town = 30**, **castle = 10**.
- Settlements of the **whole side** (principal + sub-vassals) count. Attacker holding a contested
  defender-side fief adds its weight; defender holding an attacker-side fief subtracts it.
- **Prisoners = ±5 each.** While a side holds a captive who is a member of the *opposing principal's
  clan* (the claimant's or the title-holder's own clan), it scores +5 per head in that side's favour;
  the bonus vanishes on release/escape (it is state, like settlement control, not an event tally).

*Accumulated (the one persisted score term — `PrivateWar.BattleScore`):*
- **Battle outcomes** add a **normalized** bump per resolved field/siege battle between the two sides:
  `delta = BATTLE_WEIGHT × clamp(enemyForceDefeated ÷ losingSideTotalStrength, 0, 1)`, signed `+`
  attacker / `−` defender by who won, accumulated across the war and **capped to ±`BATTLE_SCORE_CAP`**.
  Normalizing by the *whole losing side's* strength makes one decisive battle worth far more than many
  skirmishes, and the cap keeps battles a *bump* subordinate to the main goal + fatigue. `BATTLE_WEIGHT`
  and `BATTLE_SCORE_CAP` are tunable constants (start both at 50 — annihilating a side in one battle
  caps it). This is the only term that cannot be recomputed from current state, so it lives on the record.

*Forcing function:*
- **Fatigue retained** (direction = who holds the frozen main goal) → guarantees termination in a
  stalemate, regardless of the bumps above.

**Village raids do not score** (deferred). `S = clamp(stateTerms + BattleScore + fatigue, −100, +100)`.

**C. Main-goal computation (supersedes §7's "main goal = `Title.SeatSettlementId`").**
Main goal = the **highest-prosperity** settlement among the **claimed title's de jure settlements
currently held by the defendant**. **Frozen at declaration — never recomputed**, except case E.

**D. No-main-goal precondition (new).** If the defendant holds **none of the claimed title's de jure
settlements**, no main goal can be computed and **the CB cannot be pressed**. Usurping a title held by
a landless (or de-facto-dispossessed) clan is a separate future feature.

**E. Multi-attacker interactions (extends §9).** When a clan defends against two attackers who are also
at war with each other (no score between attackers — no CB):
- **Different CB:** if attacker-2 (no CB vs the defendant) ends up holding attacker-1's frozen main
  goal and then **makes peace with the defendant**, attacker-1's main goal is **re-evaluated** by rule
  C (the only exception to the freeze — the prize is now behind a neutral).
- **Same CB (same claimed title):** if attacker-2 captures attacker-1's main goal, it converts
  CK3-style — **attacker-1 makes peace with the defendant and goes to war with attacker-2** (you fight
  whoever physically holds the title you both claim).
