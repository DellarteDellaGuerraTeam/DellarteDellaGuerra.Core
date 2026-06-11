# Feudal Title Transfer — Historical Accuracy Analysis & Design Brainstorm

> **Status:** Analysis / design brainstorm. No code changes have been made as a result of this
> document. §7 grounds the design in engine surfaces verified against the 1.3.1 decompiled
> source (via the bannerlord-search MCP).
> **Branch:** `claude/banner-lord-mcp-integration-79WN8`
> **Date:** 2026-06-11
> **Question that prompted it:** "Historically, the title was a title, right? If one lost its
> place of power, it wouldn't lose its title? It's the king that decided, no? Or vassals placing
> their allegiance to a new clan?"

---

## 1. What the mod currently does

Title transfer is keyed entirely off settlement ownership:

1. A settlement falls. The kingdom holds a claimant election; `FeudalTitleSwapBehavior`
   replaces the vanilla `SettlementClaimantDecision` with `FeudalSettlementClaimantDecision`,
   where the **chooser is the de jure Duke** (or King for crown-direct counties), support is
   weighted by feudal distance (`ComputeFeudalSupportUseCase`), and clans holding claims on the
   title are injected as extra candidates.
2. Whoever ends up owning the settlement triggers `OnSettlementOwnerChanged` →
   `AssignTitleUseCase.Execute(settlementId, newOwnerClanId)`
   (`FeudalTitleCampaignBehavior.OnSettlementOwnerChanged`). **The title follows its seat
   instantly**: the new settlement owner becomes the new count/duke/baron.
3. The dispossessed clan receives a permanent **Strong claim** of origin `Conquest` on the
   title (deduplicated by claim id `<titleId>:<clanId>:conquest`), which feeds future
   elections, tension accumulation and petitions.

The de jure tree (`titles.config.xml` → `XmlFeudalStructure`) never changes; only holders move.
Suzerainty and vassalage are computed live from holders + the de jure tree
(`GetSuzerainUseCase`, `GetDirectVassalsUseCase`), so a title swap silently re-lieges every
vassal underneath it with no cascading state updates.

**Why it was built this way:** Bannerlord's one hard, reliable signal is
`OnSettlementOwnerChanged`. Keying transfer off it is simple, self-healing, and the hierarchy
always matches what the player sees on the map. The conquest claim is the nod to history.

## 2. What actually happened historically (England, 1471)

A title was a **legal dignity held of the crown**, created by royal letters patent. It was not
attached to the castle. Losing your seat to an enemy made you a *dispossessed* earl, not a
commoner. A title moved only by:

1. **Attainder** — an act of parliament declaring the holder a traitor, forfeiting titles and
   lands to the crown, which the king then regranted to a loyalist. This is the canonical
   Wars-of-the-Roses pattern:
   - The Percys lost the earldom of Northumberland by attainder after Towton (1461).
   - Edward IV granted it to John Neville (Montagu) in 1464.
   - Edward IV restored it to Henry Percy in 1470 to keep the north quiet.
   - Castles changed hands constantly throughout; the title moved only by these acts.
2. **Inheritance / extinction** — death without heirs, or titles passing through marriage
   (Warwick's earldom came via his wife, Anne Beauchamp).
3. **Royal grant** of vacant or newly created titles.

So yes — *the king decided*, with parliament as the instrument.

On allegiance: by 1471 the binding force was no longer land tenure but **bastard feudalism** —
lords retained followers by indenture, fees and livery. Affinities genuinely re-aligned to
whoever offered better lordship (the Stanleys made a dynasty of fence-sitting). Allegiance was
sticky to persons and money, not to whoever sat in the castle.

## 3. The gap

The current model has no **de facto vs de jure holder distinction**. Conquest conflates three
things history kept separate:

| Concept | History | Mod today |
| --- | --- | --- |
| Occupation of the seat | Military fact, changes hands freely | Triggers full title transfer |
| The dignity (title) | Moves only by attainder/grant/inheritance | Follows the seat instantly |
| Vassal allegiance | Affinity-based, negotiable, sticky | Recomputed instantly from holders |

Consequences in play: a foreign conqueror instantly becomes a legitimate English earl; a count
who loses one siege is stripped of his dignity immediately; vassals flip liege the moment a
castle falls, with no period of divided or contested loyalty.

## 4. Design options

### Option A — Status quo (title follows seat + conquest claim)

- **Pros:** Self-healing; hierarchy always matches the map; zero extra UI; already shipped.
- **Cons:** Ahistorical in exactly the way described above; the king has no agency over
  dignities (only over settlement grants); "dispossessed lord" stories cannot happen.

### Option B — Full historical model (occupation ≠ title)

Split holder into **de jure holder** (the dignity) and **de facto occupant** (who holds the
seat).

- Conquest sets only the occupant. The dispossessed clan keeps the title — a count-in-exile,
  like Henry Percy in the 1460s.
- The title moves only by decision:
  - **Attainder decision** — a new `KingdomDecision` where the king (already the chooser
    pattern in `FeudalSettlementClaimantDecision.DetermineChooser`) attaints the old holder
    and grants the dignity, typically to the occupant. Gated on same-kingdom: a foreign
    conqueror holds the castle but never the *English* title until the kingdom itself wins.
  - **Inheritance** — existing `GenerateInheritanceClaimsUseCase` path, unchanged.
- **Allegiance as a third force:** prolonged occupation or a weak crown lets the de facto
  holder pressure for regularisation; vassal support weights
  (`ComputeFeudalSupportUseCase`) shift toward whoever actually exercises lordship — the
  "vassals placing their allegiance" dynamic, with the existing tension machinery as its home.
- **Pros:** Historically faithful; creates exile/restoration narratives; gives the king real
  agency; models bastard feudalism's drift of allegiance.
- **Cons:** Titles drift out of sync with the map unless surfaced clearly; needs UI for
  "Earl of X (dispossessed)" vs "occupier of X"; needs rules for when exile status ends
  (extinction? generational decay? restoration?); needs AI that actually uses the attainder
  decision, otherwise dignities freeze while the map moves on.

### Option C — Middle ground (deferred transfer)

Title still ultimately follows the seat, but not instantly:

- Conquest marks the title **contested** (occupant recorded, de jure holder unchanged).
- After a grace period (e.g. 1 game year) *or* an explicit royal grant — whichever first — the
  title transfers and the old holder gets the conquest claim as today.
- Cross-kingdom conquest never transfers until peace is made (titles are dignities *of* the
  kingdom).
- **Pros:** Most of the historical texture (exile window, royal agency, foreign occupiers
  staying illegitimate) at a fraction of Option B's complexity; degrades gracefully to today's
  behaviour if the AI never acts.
- **Cons:** Still needs a contested-state UI hint; grace-period tuning is arbitrary.

## 5. Implementation sketch (Options B/C share most of this)

Smallest first step:

1. **Domain:** extend `record Title` with `string? OccupantClanId` (de facto) alongside
   `HolderClanId` (de jure). `TitleStateSerialiser` and `InMemoryTitleRegistry` follow
   mechanically.
2. **`AssignTitleUseCase`:** on conquest, set only `OccupantClanId`; emit a `Contested`
   marker instead of transferring. Same-kingdom settlement *grants* (election outcomes) may
   still transfer directly — being granted the castle by the chooser *is* the royal/ducal
   decision in-game.
3. **New `FeudalAttainderDecision : KingdomDecision`:** chooser = king; candidates = occupant +
   claimants; outcome = transfer `HolderClanId` + conquest claim for the attainted clan.
   Proposable by the occupant (regularisation petition) or auto-proposed past a tension
   threshold — reusing the petition pattern in `FeudalPetitionDecision`.
4. **Suzerain/vassal queries:** unchanged — they keep reading `HolderClanId`, which is the
   point: dispossession does not re-liege the de jure hierarchy. Optionally weight
   `ComputeFeudalSupportUseCase` by occupation duration for the allegiance-drift effect.
5. **UI:** encyclopedia panels and `FeudalHierarchyScreen` show
   "Holder (dispossessed) / Occupied by X" when the two diverge; `BuildFeudalMapUseCase`
   exports both columns.

Open questions to settle before building:

- When does exile status end if the king never acts — extinction of the clan, a generation, or
  never (pure Option B)?
- Does a *kingdom-level* change of dynasty (crown title changing hands) mass-confirm or
  mass-attaint? (Readeptions did both, selectively.)
- Should levies (`LevyCampaignBehavior`) follow the de jure holder or the occupant during a
  contested period? Historically: whoever the retainers' indentures pointed at — likely the
  occupant for the seat's own troops, the de jure holder for sworn vassal clans.
- AI weighting for the attainder decision: kings should attaint enemies-in-exile readily but
  hesitate to attaint powerful loyal houses (the Percy restoration case).

## 6. Recommendation

Option C first. It introduces the de jure/de facto split (the load-bearing concept) and the
attainder decision without committing to indefinite-exile bookkeeping, and every later step
toward Option B (longer grace periods, allegiance drift, restoration mechanics) is additive on
top of it.

## 7. Engine integration — verified against 1.3.1 decompiled source

Every engine surface Option C needs exists and is already half-wired in the mod. Findings
below are from the 1.3.1 decompile (bannerlord-search MCP), cross-checked against our code.

### 7.1 The conquest-vs-grant signal already arrives — we discard it

`CampaignEvents.OnSettlementOwnerChangedEvent` delivers
`ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail`, and
`FeudalTitleCampaignBehavior.OnSettlementOwnerChanged` already receives it (and ignores it).
The enum (verified):

| Detail | Meaning | Proposed title effect |
| --- | --- | --- |
| `BySiege` | Conquest | **Contested**: set occupant only, de jure holder keeps the dignity |
| `ByRebellion` | Rebel clan seizes seat | **Contested** — rebels are illegitimate until regularised (free historicity) |
| `ByKingDecision` | Claimant-election outcome / royal grant | **Full transfer** — this *is* the royal/ducal decision (§5.2) |
| `ByGift`, `ByBarter` | Consensual conveyance | **Full transfer** (historically would need royal licence; not worth modelling) |
| `ByLeaveFaction`, `ByClanDestruction`, `Default` | Administrative | **Full transfer** — self-healing fallback, keeps hierarchy matching the map |

The vanilla flow already produces the two-step pattern we want: a siege fires `BySiege`
(→ contested), the kingdom then runs the claimant election, and *its* outcome fires
`ByKingDecision` via `ChangeOwnerOfSettlementAction` (→ transfer). When the election awards
the settlement to the clan that captured it, the dignity follows within ~2 days; when it
awards it elsewhere, the title correctly tracks the king's choice, not the sword. The
**grace period of Option C is therefore not even a new clock for same-kingdom conquest** —
it falls out of the election delay. The explicit grace clock is only needed for
cross-kingdom occupation (no election happens in the *title's* kingdom).

Domain mapping: keep TaleWorlds types out of the domain — the adapter collapses the enum to
a domain `SeatTransferKind { Conquest, Grant, Administrative }` passed to
`AssignTitleUseCase.Execute`.

### 7.2 KingdomDecision lifecycle supports a self-cancelling attainder decision

Verified in `TaleWorlds.CampaignSystem.Election.KingdomDecision` and
`KingdomDecisionProposalBehavior`:

- **Enqueue:** `Kingdom.AddDecision(decision, ignoreInfluenceCost)`. Vanilla enqueues
  `SettlementClaimantDecision` from the ruling clan with `ignoreInfluenceCost: true`; our
  `InternalConflictCampaignBehavior.TryTriggerPetition` already uses the identical pattern
  (dedup against `kingdom.UnresolvedDecisions`, then `AddDecision`). The attainder proposal
  is the same code shape with a different trigger (contested-duration / occupant petition
  instead of tension threshold).
- **Resolution loop:** `KingdomDecisionProposalBehavior.UpdateKingdomDecisions` iterates
  `UnresolvedDecisions`; decisions where `ShouldBeCancelled()` returns true are removed
  (with an `OnKingdomDecisionCancelled` event), the rest run
  `new KingdomElection(d).StartElectionWithoutPlayer()` once `TriggerTime` passes.
- **Self-cancellation hooks:** `ShouldBeCancelled()` already cancels when the kingdom dies,
  the proposer leaves, or `IsAllowed()` fails; `ShouldBeCancelledInternal()` is the virtual
  for decision-specific rules. `FeudalAttainderDecision.ShouldBeCancelledInternal()` returns
  true when the title is no longer contested (de jure holder retook the seat, or the
  occupant changed) — the queue cleans itself, no bookkeeping behavior needed.
- **Timing:** `TriggerTime = HoursFromNow(HoursToWait)`, `HoursToWait` virtual (default 48).
  An attainder is parliament assembling, not a council vote — override to something like a
  week so the dispossessed holder has a window to retake the seat and moot the act.
- **Player UX is free:** `NeedsPlayerResolution` + the verified `SandBox.View.Map` popup
  machinery ("Critical Kingdom Decision" → kingdom screen) means a player king rules on
  attainders with zero UI work from us; `DetermineChooser()` = king is exactly the
  `FeudalPetitionDecision` fallback path.
- **`GetFollowUpDecision()`:** consumed only by the kingdom Decisions VM (player resolution
  flow) to chain a follow-up immediately — vanilla chains
  `SettlementClaimantPreliminaryDecision` → `SettlementClaimantDecision`. Useful polish to
  chain election → attainder for the player, but AI kingdoms never call it; the daily-tick
  proposal path is the mechanism, follow-up is presentation.

### 7.3 Save & state

- `FeudalElectionSaveDefiner` (base id 2_887_350) has ids 1–4 taken; the attainder decision
  and its two outcomes take 5–7. The `FeudalPetitionDecision` outcome pattern
  (`GrantClaimOutcome`/`DenyClaimOutcome`, `EmptyImageIdentifier`, `[SaveableField]` string
  ids) is reusable verbatim: `AttaintOutcome` (transfer dignity to occupant + conquest claim
  for the attainted) / `UpholdOutcome` (dignity stays; occupant gains tension or a claim).
- Domain state rides the existing flat-string `TitleStateSerialiser`: `Title` gains
  `string? OccupantClanId` and a contested-since day stamp. The deserialiser must accept the
  old field count (pre-split saves → not contested) — same trick as the seed-on-empty path
  in `FeudalTitleCampaignBehavior.OnGameLoaded`.
- `record Title(Id, Name, Rank, SeatSettlementId, HolderClanId)` extends mechanically;
  `AssignmentResult` gains a `Contested` flag so the campaign behavior can notify.

### 7.4 Cross-kingdom rule

`CampaignEvents.MakePeace` delivers
`(IFaction side1, IFaction side2, MakePeaceAction.MakePeaceDetail detail)` (verified). Rule:
while the occupant's kingdom differs from the title's kingdom (the crown holder's engine
kingdom), no attainder is possible — the title sits contested indefinitely, a dignity in
exile. On `MakePeace`, contested titles whose seats stay with the enemy kingdom finalise:
transfer + conquest claim (Option C), or remain exile dignities (Option B upgrade, later).
War-end is the only event needed; no per-tick polling.

### 7.5 Revised smallest first step

1. Domain: `Title.OccupantClanId` + contested stamp; `SeatTransferKind`;
   `AssignTitleUseCase` branches on it. Tests first (`AssignTitleUseCaseTests` has the
   harness).
2. Adapter: `FeudalTitleCampaignBehavior.OnSettlementOwnerChanged` maps the already-received
   `detail` per §7.1 table. Serialiser round-trip + backward-compat test.
3. `FeudalAttainderDecision` (save ids 5–7), proposed from
   `InternalConflictCampaignBehavior`'s daily tick when a same-kingdom title has been
   contested ≥ N days (covers the rare case where no claimant election ran, e.g. the
   capturer already owned the seat de facto via `ByLeaveFaction` chains).
4. `MakePeace` listener finalising cross-kingdom contested titles.
5. UI: hierarchy screen + encyclopedia mixins show "Occupied by X" when holder ≠ occupant.

Step 1+2 alone already deliver the headline historical fix: a foreign or rebel conqueror no
longer becomes an English earl by storming a wall.