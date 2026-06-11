# Feudal Title Transfer — Historical Accuracy Analysis & Design Brainstorm

> **Status:** Analysis / design brainstorm. No code changes have been made as a result of this
> document.
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