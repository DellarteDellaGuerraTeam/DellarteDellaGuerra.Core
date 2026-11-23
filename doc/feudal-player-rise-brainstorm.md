# From Nobody to Baron — Player Entry into the Feudal System

> **Status:** Brainstorm. No code, no decisions. Companion to `feudal-and-private-wars-guide.md`
> (what exists) and `feudal-hero-claims-and-succession-design.md` (hero-level claims).
> **Date:** 2026-09-29
> **Question that prompted it:** "It's difficult for the player to enter the claim system since the
> player has no claims. What goals would naturally make the player a fief owner at some point?"

---

## 1. The problem

Every door into the feudal system requires already being inside it:

- Storming a *held* seat makes you its **occupant**, not its holder.
- A private war needs a **claim**; claims come from conquest, inheritance or de jure data — all of
  which presuppose holding or having held a title.
- In a vacant-fief vote an outsider speaks with **a fifth of a voice**.
- `feudal-hero-claims-and-succession-design.md` decision 3 excludes the player as claimant *and*
  as target of private wars.
- `ClaimOrigin.Marriage` and `ClaimOrigin.Purchase` exist but nothing produces them.

A vanilla-style start (adventurer, clan tier 0) has no ramp onto a ladder whose bottom rung is
Baron.

## 2. The historical answer: a rung below Baron

In 15th-century England "noble" meant the **peerage** — some 50–60 families summoned to
Parliament. Below them sat several thousand **gentry** families (knights, esquires, gentlemen):
armigerous, landholding, office-holding, but commoners. Self-made men almost never jumped from
nothing to a peerage. They went:

- **commoner → gentry** in a lifetime or a few generations, through law, trade, war profit or
  household service — ending with **manors**, not titles;
- **gentry → peer** through royal/magnate favour, forfeited land or an heiress, usually during
  upheaval.

Anchors (from memory — verify before using in game text):

| Who | Path |
|---|---|
| Paston family | husbandman → lawyer/judge buying manors → gentry heir of Caister, in 3 generations |
| de la Pole | Hull wool merchant → crown lender → Earls, then Dukes of Suffolk |
| Geoffrey Boleyn | London mercer, Lord Mayor 1457 → bought Blickling and Hever, married a peer's daughter |
| Sir John Fastolf | minor gentry → French-war ransoms → bought estates, built Caister |
| William Hastings | gentry household man of Edward IV → Baron 1461 on forfeited Lancastrian land |
| William Herbert | Welsh gentry → bought Henry Tudor's wardship (1462) → Earl of Pembroke 1468 |
| Richard Woodville | knight → married a dowager duchess → Earl Rivers |

Two more hooks that fit 1471 exactly:

- **Battlefield knighting** — Edward IV dubbed a batch of men on the field after Tewkesbury (1471).
- **Distraint of knighthood** — a man with **£40 a year** in land was compelled to take up
  knighthood or pay a fine. Wealth *forces* rank on you.

## 3. The proposed ladder

| Rung | Status | How you get here | What it unlocks |
|---|---|---|---|
| 0 | **Commoner** — soldier, free company captain, trader, bandit | vanilla start | mercenary work, trade, loot, tournaments (melee/archery only) |
| 1 | **Gentleman** (armigerous) | renown threshold, or *buy* a grant of arms from the heralds | jousting, being approached by lords for retaining |
| 2 | **Esquire / retainer** | **indenture** with a lord: annual fee, service owed | minor offices, buying manors, called to the patron's levies |
| 3 | **Knight** | dubbed by patron/king after a battle, **or** distraint (estate income over threshold) | major offices, commanding a levy contingent, eligible for **promises** of land |
| 4 | **Baron** | patron's grant, forfeited-title grant, heiress, fulfilled promise | **enters the claim system** |

The ladder is **player functionality** — AI lords start where they are, as in vanilla.
Rungs 0–3 are outside the title tree; rung 4 is its existing bottom. Rung 0 is essentially the
vanilla game — the new work is rungs 1–3 and the hand-off into rung 4.

## 4. The pieces

### 4.1 Making a name — rung 0 → 1

Mostly vanilla: mercenary contracts, trading, reselling loot, banditry, tournaments. The additions
are about **being noticed**:

- **Tournaments as auditions.** Commoners enter melee and archery; **jousting requires gentility**
  (DADG already has joust code). A commoner who wins repeatedly draws a retaining offer.
- **Remarkable deeds** — killing a notable enemy, holding a breach, winning a battle as a small
  captain — raise renown and relation with the lord commanding, *if he was there to see it*.
- **Enlisting as a soldier** in a lord's party (à la *Serve as a Soldier* / *Freelancer*) is
  the commoner-start fantasy. Big feature; could be delegated to compatibility with an existing
  mod rather than built.

### 4.2 Retaining (indenture) — rung 1 → 2

A lord offers an indenture: an **annual fee** plus war wages, in exchange for service when
called. Mechanically, the player becomes a **de jure dependent of that lord** without holding a
title, so:

- the existing **levy** logic applies (answer +2, refuse −5) and becomes the progress meter;
- the patron's private wars pull the player in via the liege-chain walk — **if** the chain can
  pass through a titleless dependent (see open questions);
- breaking an indenture (joining a rival) is possible but burns the patron and damages Honour.

Who offers? Lords with high relation, who have seen you fight, and who **need men** — a lord
facing or planning a private war is the natural recruiter.

### 4.3 Estates (manors) — money becomes land

Gold needs a destination that isn't troops. A **manor** is *not* a settlement you own — vanilla
villages follow their bound castle/town and fighting that model is expensive. Instead a manor is
an **asset attached to a village**, like a workshop is to a town:

- bought from an indebted lord, a widow, the crown, or at the forfeiture of an attainted lord;
- yields rent income, a small recruit pool, and **prestige** that counts toward the next rung;
- lost if the village's lord turns hostile to you, or seized if you are on the losing side of a
  forfeiture.

Enough manor income triggers **distraint**: take the knighthood (and its obligations) or pay the
fine.

### 4.4 Offices — responsibility grows with trust

Appointments made by a patron or the crown. Income, influence, relation, and — the real payoff —
**presence in the right place when things fall apart**.

| Office | Granted by | Flavour |
|---|---|---|
| Bailiff / steward of a manor or castle | patron | could map onto vanilla **governor** for a castle the patron holds |
| Constable of a castle | patron / crown | you hold it *for* him; if he falls, you are the man inside the walls (cf. Harlech, held for Lancaster until 1468) |
| Sheriff | crown, **one-year term** | lucrative, collects and keeps a share; makes local enemies |
| Justice of the peace | crown | influence, relation with local gentry |
| Warden of the Marches | crown | Scottish border; Percy/Neville territory; military command |

Advanced — a phase 2 idea, not a prerequisite.

### 4.5 Knighthood — rung 2 → 3

Two triggers:

- **Dubbing** — after a battle where the player served under a patron/king with high relation and
  a notable contribution. Event-driven, the Tewkesbury moment.
- **Distraint** — estate income over the threshold.

Knighthood is **personal** (hero-level), which fits the hero-titles direction.

### 4.6 The promise — rung 3 → 4

This is the bridge the whole ladder leads to. A patron offers:

> *"Serve me in my quarrel with Lord X. If I win the County, the Barony of Y is yours."*

**What can be promised:**

- a barony the patron already holds and can spare (subinfeudation);
- a barony **inside** the title the patron's private war is pressing (de jure below the main
  goal);
- a forfeited barony the patron expects to control after an attainder.

**Why it is "potentially":** delivery depends on

1. the patron **winning** (private war score reaches +100);
2. the patron's **Honour** trait — honourable lords keep their word, devious ones find reasons;
3. relation at the time of delivery, and whether a rival with a stronger claim appears.

**The key design trick — a broken promise is itself a door.** If the patron wins and reneges, the
player gains a **claim** on the promised barony (a new origin, `Promise`, weak strength). That
claim is enough to petition, and eventually to open a private war. So every outcome of a
promise moves the player forward:

| Outcome | Result |
|---|---|
| Patron wins, keeps word | player becomes **Baron** |
| Patron wins, reneges | player gets a **claim** → petition / private war |
| Patron loses | promise void; player keeps pay, renown, relation, and is marked as the patron's man by the winner |

**Promises as a trade.** Promises can also flow the other way — a strong free-company captain
*sells* service to either principal of a private war and negotiates the price (gold, a manor, a
promised barony). Bidding between the two sides is where the "strong mercenary captain" fantasy
lives.

### 4.7 Other entry doors (from the earlier discussion)

Not the main ladder, but alternate routes for the same rung-4 goal:

- **Marriage** — wed an heiress; if she is the heir the title passes *jure uxoris*, otherwise a
  `Marriage` claim. Needs clans seeded with heiresses.
- **Wardship** (`Purchase`) — buy custody of a minor heir, marry them into your house.
- **Forfeiture pool** — attainted lords' titles escheat to the crown; loyalty, not only claims,
  weighs in their regrant. Attainted families keep a claim and can petition for **reversal** —
  every grant the player receives comes with an enemy.
- **Turncoat reward** — switching sides at a decisive battle (Stanley, 1485).

## 5. Collisions with the current design

- **Player exclusion (decision 3)** — the hero-claims design keeps the player out of private
  wars as claimant and target. The promise path needs at least *participant* status, and the
  broken-promise door needs *claimant* status. The patch layer reportedly already supports a
  player belligerent.
- **Allegiance is the liege-chain walk.** A titleless retainer has no chain today; a hired
  mercenary has no chain at all. Either the walk learns about indentures, or sides get an
  explicit contract override.
- **The player never triggers automatic same-kingdom siege capture.** A player besieging a
  private-war enemy must still be able to take the fief.
- **Titles are clan-level** while knighthood is naturally personal.

## 6. Open questions

1. ~~Player-only, or do AI "new men" climb the same ladder?~~ **Settled: player-only.** As in
   vanilla, there are no AI adventurers; the ladder is player functionality. AI new men are not
   ruled out later but are not the focus.
2. Is the rung-1 grant of arms bought, earned, or both? (The heralds were incorporated in 1484,
   just after the campaign starts.)
3. Should a manor be a new asset type, or can it ride an existing vanilla asset?
4. Does the indentured player count as a **vassal** for the liege-chain walk, or only for levies?
5. Which offices, if any, are phase 1? Constable-as-governor looks cheapest.
6. Does a patron's promise need a UI (a "letter of promise" in the encyclopedia / clan page), or
   is a notification plus a quest log enough?
7. What stops a player from farming promises from both sides of the same war?
