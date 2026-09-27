# Cross-Border Private Wars — the Marches (proposal)

> **Status:** Proposal for the user to think through. **Nothing here is decided.** Nothing is
> implemented either: cross-border capture of towns and castles is not built, and automatic
> claim-driven declaration stays same-kingdom only (`feudal-hero-claims-and-succession-design.md`,
> decision 5).
> **Date:** 2026-09-27
> **Background:** `cross-kingdom-private-wars-analysis.md`, which lives in the main checkout's `doc/`
> (untracked) and not in this worktree (§4 blockers, §5 land options,
> §7 v2 gates H1–H3 / S1–S7). Line numbers are quoted from that analysis (PrivateWars at `bd1fd24`,
> v2 at `36db43b`). Code being changed alongside this doc may have shifted them.
> **Prompt:** "we'll need to define what usecases can include the capture of a town/castle across the
> border. The reference being the anglo-scottish marches". On defender identity: "we need to think
> through the feature again".

---

## 1. What already works (or is landing now)

The private-war substrate is per clan pair and does not look at kingdoms. Work landing alongside this doc:

- **Resolvers (in place):** clan-grain hostility resolvers work across kingdoms.
- **Crown-war escalation (analysis blocker a):** vanilla's escalation for a player hostile act is
  suppressed between registered enemies.
- **Kingdom change (blocker b):** a private war persists when a belligerent changes kingdom. It folds
  into a crown war through `ResolveWar` when the two crowns go to war, so `WarResolved` fires.
- **Nameplates and menus (blocker d):** these colour the pair as enemies across kingdoms.
- **DADG side:** distraction is checked against the defender's own kingdom (S1), and supporters are
  solicited along the title chain across kingdoms (H2).
- **Content:** a Scottish title tree is being added to `config/titles.config.xml`.

What this doc must settle: (a) **when**, if ever, a private war may take a town or castle across the
border; (b) **who the defender is** across a border. Until both are settled, automatic declaration
stays behind the gate `ClaimPressureCampaignBehavior.cs:150-151` (H1).

## 2. Historical reference: the Anglo-Scottish marches, c.1450–1490

Confidence is marked where it matters: **[firm]** means standard narrative history, and **[check]**
means from memory and worth verifying before it drives a design choice.

- **Wardens of the Marches.** Each side had an East, a Middle and a West March, each under a warden
  appointed by the crown. **[firm]**
  - English side: wardenships were largely held by the Percys (East) and the Nevilles (West) for most
    of the century. Richard of Gloucester was warden of the West March from 1470. **[firm]** In 1483
    he was granted a hereditary West March wardenship, with Cumberland as a quasi-palatinate. **[check]**
  - Scottish side: the Black Douglases dominated the marches until their fall in 1455 (Arkinholm).
    Afterwards the leading figures were the Red Douglas earls of Angus and local families such as the
    Homes, Kerrs, Johnstones and Maxwells. **[firm on 1455; family list approximate]**
  - The warden was the crown's officer on the border, not a private lord. The office was usually held
    by the region's dominant magnate, which blurred royal and private force.
- **March law and days of truce.** The *leges marchiarum* were codified in 1249. **[firm]** Wardens
  of both realms met on days of truce to hear complaints ("bills") of raids, theft and killing, and
  to award redress. **[firm]** The point matters for design: **raiding was tolerated under a truce
  and settled by redress, and it did not trigger war between the crowns.**
- **Raiding.** Cross-border raiding for cattle, goods and prisoners (ransom) was endemic and continued
  under truces. **[firm]** "Border reivers" as a named culture of riding surnames is mostly documented
  for the **16th** century, so using it for 1450–1490 is a mild anachronism. **[check]** Magnate feud
  also crossed the border: Percy against Douglas at Otterburn (1388) and Homildon Hill (1402).
  **[firm; earlier than the mod's period, context only]**
- **Castles and towns did change hands, but as crown acts:**
  - **Roxburgh, 1460:** English-held since 1346. Besieged by James II in person, who was killed on
    3 August 1460 when one of his guns burst. The castle fell days later and the Scots **slighted**
    it rather than garrisoning it. Wark was taken in the same campaign. **[firm on the first three;
    Wark: check]**
  - **Berwick, 1461:** after Towton, Margaret of Anjou surrendered it to Scotland (April 1461) for
    Scottish support of the Lancastrians. This was a **cession for political support**, not a
    conquest. A parallel attempt to hand over Carlisle failed. **[firm on Berwick; Carlisle: check]**
  - **Berwick, 1482:** retaken by Richard of Gloucester's royal army during the war of 1480–82. The
    town fell first and the castle held out until late August. It has been English since. **[firm]**
  - **Northumbrian castles, 1461–64:** Alnwick, Bamburgh and Dunstanburgh passed back and forth
    between Lancastrian garrisons (with Scottish and French help) and Yorkist besiegers. This was a
    civil war spilling across the border. **[firm]**
- **Truces and war:** a long truce in 1464, a marriage alliance in 1474, open war in 1480–82, and
  Albany and the exiled 9th Earl of Douglas raiding at Lochmaben in 1484, followed by a new truce.
  **[firm on the shape; exact dates: check]**

**Design reading (a judgement, not a fact):** on the border, a *private* lord raided, took prisoners,
fought battles and was answerable at a day of truce. Holding a castle or town across the border was
the business of crowns: royal war (Roxburgh 1460, Berwick 1482) or cession (Berwick 1461). When a
private power took a march castle, the historical answer was to slight it rather than keep it.

## 3. (a) Use cases for cross-border capture

| # | Use case | Historical anchor | Takes a fief? | Fits the engine? |
|---|---|---|---|---|
| U1 | **Border raid / feud.** Battles, village raids, prisoners and ransom across the border, with no sieges. | Endemic raiding under truce; Percy–Douglas | No | Yes. Needs a CB with **no main goal** (analysis §5 option 3). |
| U2 | **March-castle strike.** A private war may besiege only castles tagged as **march holds**, and the castle **reverts or is slighted** at war end: nothing is kept across the border. | Roxburgh 1460 (slighted), Wark | Temporarily | Partly. While held, the fief sits in the attacker's kingdom (blocker e), and the revert must run. |
| U3 | **Royal war only.** Towns and castles change hands across the border only in a crown war. A private war that escalates is **folded** into it. | Roxburgh 1460, Berwick 1482 | Yes, via vanilla war/peace | Yes. The fold is landing now. Titles finalise on `MakePeace` (uti possidetis). |
| U4 | **Cession for support.** A crown or claimant gives a seat to the other realm in exchange for aid. | Berwick 1461 | Yes, by grant | Out of scope for private wars. It would be a `ByGift`/`ByBarter` → `Grant`. |
| U5 | **Keep what you take.** A private war captures and keeps a cross-border town or castle. | None found | Yes, permanently | Cheapest to build (analysis §5 option 1) and the worst side effects: sovereignty moves while the crowns are at peace. |

**Castles vs towns.** Even U2 should be **castles only**. Towns such as Berwick and Carlisle changed
hands only by royal war or cession. Villages are raid targets under U1 and are never captured.

**March zones.** "March hold" could be a per-settlement tag (a small config list, the way abbeys are
tagged in `config/dadg.church_settlements.xml`), or it could be derived from the title tree (for
example, seats under a march lordship). A tag is simpler and matches history better, since the march
was a geographic zone, not a rank.

### Recommendation (for discussion)

1. **First cut: U1 + U3.** No cross-border sieges in a private war. Cross-border feuds raid, fight and
   take prisoners. Capture happens only when the feud escalates into (folds into) a crown war. This
   removes blocker e and the S4 "contested forever" problem entirely, and keeps sovereignty with crowns.
2. **Later, optional: U2** for tagged march castles, with a **mandatory revert at war end** and no
   title prize across the border. It reuses the existing revert machinery.
3. **Reject U5.** Keep **U4** as a separate diplomatic feature if it is ever wanted.

## 4. (b) Defender identity across a border

The code today: the defender is the **de jure holder's clan** (`ClaimPressureCampaignBehavior.cs:136`),
and `SelectMainGoal` requires the defender to own a de jure seat (S5). The older design said the
defender is the **seat's occupant** (`feudal-private-wars-design.md` §7, now marked superseded).

Within one kingdom the two rarely diverge, because a claimant election or `ApplyByDefault` realigns
them. Across a border they diverge whenever a seat is contested:

- **During a crown war:** the seat was taken with `ApplyBySiege`, so the title is contested; the holder
  is in exile and the occupant is foreign.
- **After a U2 capture:** the same situation, temporarily.

| Option | Who can be attacked | Trade-offs |
|---|---|---|
| D1 **De jure holder only** (status quo) | The title's holder, wherever they are | Simple; claims are against a person's dignity, and supporter solicitation follows the title chain. **Gap:** a dispossessed holder can never press to recover an occupied seat. He holds the title, so he has no claim to press, and the occupant is not a defender. A third-party claimant who attacks the holder cannot win the main goal, because someone else sits in it. |
| D2 **Occupant only** | Whoever holds the seat | Recovery wars become possible ("retake Roxburgh"). But the defender is then often foreign, so winning **requires cross-border capture** (U2/U5). That conflicts with the U1+U3 first cut. The prize is also muddled: the title is already the attacker's (recovery) or belongs to a third party. |
| D3 **Both** | The holder is the principal and the occupant a co-belligerent, or the reverse | The most faithful option, but it needs two pairs per war or the §18.A sides model. The registry holds one binary war per unordered clan pair, so the attribution and scoring rules get complicated. |

**Interactions to weigh:**

- **Contested titles:**
  - `ApplyBySiege` → Conquest → contested.
  - A contested title finalises only on a crown `MakePeace` between the title's kingdom and the
    occupier's (`FeudalTitleCampaignBehavior.cs:138-167`).
  - Under U1+U3, private wars never create cross-border contests, so D1 only has to cope with contests
    created by crown wars. Those are already resolved at the crown peace.
- **`ApplyByDefault` reverts:**
  - The adapter maps `ApplyByDefault` to `Administrative`, which **moves the dignity outright** to the
    new owner.
  - If a war snapshots a seat whose owner is an *occupant*, which D2 or D3 make likely, then the
    end-of-war revert to that owner **hands the occupant the title as a side effect**. D2/D3 need
    either a revert kind that preserves the contest or a snapshot of the title state.
- **Prisoners:**
  - `PrivateWarPrisonerRetentionPatch` (a prefix on `EndCaptivityAction.ApplyInternal`) keeps
    private-war captives through `PrisonerReleaseCampaignBehavior`'s releases on `MakePeace` and on a
    clan's kingdom change. That matters more across a border, where a crown peace or a defection
    releases prisoners.
  - After a **fold**, the war is a crown war and the crown peace should release as usual. Confirm
    that the retention patch stops claiming those captives once the pair is unregistered (blocker g;
    needs a test).
  - U1 makes ransom a central part of the score, so prisoner value matters more.
- **Peace and truce:**
  - Under U1 there is no seat to settle, so a **day-of-truce style ending** fits well: a fixed-length
    feud window, or fatigue without a goal, ending in status quo ante plus ransoms.
  - Under D2 with capture, the end rule has to say what happens to the seat, which is the S4 problem.

**Recommendation (for discussion):**

1. Keep **D1** with U1+U3. The cross-border feud targets the de jure holder, fights with no seat as
   its goal, and ends by fatigue or timer.
2. Treat "recover my occupied seat" as a **separate CB later**, a restricted D2: the defender is the
   occupant, and it is only allowed when the seat is a tagged march castle (U2) or the crowns are at war.

## 5. (c) Code changes per option

Paths are relative to the worktree `src/` (DADG) or the PrivateWars submodule.

**U1 (raid-only feud: no main goal, no cross-border siege)**
- A second casus belli type. `DeclarePrivateWarUseCase` requires a main goal, and fatigue drifts
  toward whoever holds it (analysis §5). U1 needs its own score (battles, raids, prisoners and ransom)
  and its own end rule (fatigue only, or a fixed feud window).
- Block cross-border sieges for cross-kingdom pairs in three places:
  - the attacker-intent / siege driver in `PrivateWarCampaignBehavior` (the capture path around :402);
  - the player encounter menu, `PrivateWarEncounterMenuOptions.cs` (the attack/siege option, near
    the `NotAttackableByPlayerUntilTime` check at :46);
  - the AI target-score override for settlements.

  Villages stay raidable.
- DADG: `ClaimPressureCampaignBehavior` picks the U1 CB when the defender's kingdom differs, then
  lifts H1 (:150-151) for that case only.
  - The player stays excluded (:142).
  - Rules for null kingdoms still need deciding.

**U3 (royal war only)**
- Most of it is landing: the fold into the crown war via `ResolveWar`, and `WarResolved` on fold.
- DADG: decide whether a folded war's claimant gets anything at the crown peace. S7 applies:
  `PrizeAward`/`AttackerClaimLost` are consumed nowhere, and `WarResolved` carries only
  `(AttackerClanId, AttackerWon)`.

**U2 (march-castle strike, mandatory revert)**
- A content tag for march holds, and a siege gate that allows only tagged **castles** for
  cross-kingdom pairs.
- End of war: always revert cross-border captures, and skip the prize.
  - `ResolveWar` already applies only `plan.Reverts` via `ApplyByDefault` (:608).
  - The revert returns the fief to the original owner clan's kingdom automatically.
- Accept, or mitigate, the interim state while the castle is held (blocker e):
  - the castle sits in the attacker's kingdom, with its borders, fief counts and AI war maths;
  - its title is contested (S4) until the revert.
- Optional: "slight" on revert, for example damaging walls or prosperity, as a nod to Roxburgh.

**U5 (keep what you take): not recommended.** It would need an S4 fix, meaning a private-war outcome
that finalises the contested title (`FeudalTitleCampaignBehavior` gains a `WarResolved` handler that
issues a Grant). It also needs policy on sovereignty moving while the crowns are at peace.

**D1:** no change. **D2 / D3:**
- Defender selection at `ClaimPressureCampaignBehavior.cs:136`, and `SelectMainGoal`'s
  de-jure-seat requirement (S5).
- A revert kind or snapshot that does not hand the occupant the dignity (§4 interactions).
- D3 additionally needs multi-pair or sides support (private-wars design §18.A).

**Whatever option is chosen:**
- S3: a clan with titles in both trees resolves its allegiance through `GetHighestTitle`, and equal
  ranks go by dictionary order. Decide whether this needs a deterministic tiebreak now that a
  Scottish tree exists.
- `FeudalHierarchyOverRealContentTests` assumes a single `clan_lancaster` root. The Scotland work
  owns that update.

## 6. (d) Open questions for you

1. **Scope:** is U1 + U3 (no cross-border capture in private wars) acceptable as the first cut? Or is
   taking a march castle (U2) essential to the feel you want?
2. **If U2:** should march holds be a hand-tagged list, and which castles? Candidates: Berwick castle
   (but not the town?), Roxburgh, Wark, Norham, Carlisle? Should a captured castle always revert, or
   be slighted?
3. **Towns:** never by private war? Or should Berwick be a deliberate exception?
4. **Defender identity:** keep D1 (de jure holder)? Is "recover my occupied seat" a CB you want, and
   only in the marches or everywhere?
5. **Automatic declaration:** once the cross-border CB exists, should AI clans declare cross-border
   feuds automatically, or only by claim? Should the player be able to start one? (Today the player
   is excluded and has no "press claim" path.)
6. **Wardens:** should the warden office exist as an actor, for example a warden clan that can end
   a feud (a day of truce) or that is called to arms first? Or is this flavour only?
7. **Escalation:** under what conditions should a cross-border private war escalate into a crown war,
   rather than folding only when the crowns happen to go to war for other reasons? (Vanilla escalation
   for the player is now suppressed.)
8. **End rule for goal-less feuds:** fatigue only, a fixed window (a "day of truce" after N days), or
   the defender's crown intervening?
9. **Contested titles at peace (S4):** if any capture path is allowed, how does a contested title
   finalise without a crown `MakePeace`? Options: revert, private-war resolution, or never.
