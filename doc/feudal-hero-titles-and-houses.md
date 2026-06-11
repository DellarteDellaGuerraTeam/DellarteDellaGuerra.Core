# Hero-Level Titles, Houses & Private Wars — Design Brainstorm

> **Status:** Analysis / design brainstorm. No code changes have been made as a result of this
> document. Companion to `feudal-title-transfer-design.md` (de jure/de facto split) and
> `feudal-system-analysis.md` Part B (abstract private war).
> **Branch:** `claude/banner-lord-mcp-integration-79WN8`
> **Date:** 2026-06-11
> **Question that prompted it:** "What would it take to reason about individuals instead of
> clans? Ideally two or more persons within the same clan could have a title. How would that
> impact a future private-wars feature? Could we introduce clans within clans?"

---

## 1. What hero-level titles would take

### The domain layer is nearly free

Every identifier in the domain is an **opaque string**: `Title.HolderClanId`,
`Claim.ClaimantClanId`, `LevyCall.IssuingClanId/VassalClanId`, the tension keys. The
algorithms — suzerain walk, inverse vassal query, claim dedup, tension accumulation — never
inspect what kind of entity the id names. Switching the holder id from a clan `StringId` to a
hero `StringId` is a rename plus signature sweep (`GetSuzerain(heroId)` etc.). Hero StringIds
are stable and persist through the existing pipe-delimited `TitleStateSerialiser` unchanged.

One part actively *improves*: inheritance. Today `FeudalTitleCampaignBehavior.OnHeroKilled`
approximates succession by generating claims for passed-over relatives' *clans* — a
clan-level contortion. With hero holders, holder death runs a real succession use case
(primogeniture or whatever rule) passing each title to a specific heir, with personal claims
for the passed-over. That is the most CK3-like payoff of the whole change.

### The engine boundary is where the cost lives

Bannerlord's political machinery is clan-anchored, and each adapter needs a collapse rule:

| Engine surface | Granularity | Impact |
| --- | --- | --- |
| Settlement ownership (`owner="Faction.clan_*"`, `ChangeOwnerOfSettlementAction`) | **Clan** | Seats stay clan-owned. A hero title tied to a seat needs a rule for *which* hero in the owning clan holds it (default: clan leader; better: explicit grant). |
| Elections (`SettlementClaimantDecision`, `ClanAsDecisionOutcome`) | **Clan** | Keep elections clan-level; add a second step (or a custom `DecisionOutcome`) choosing the hero within the winning clan. |
| Influence (`Clan.Influence`, `DadgClanPoliticsModel`) | **Clan** | A hero's title rank bonus must flow to his clan's influence pool regardless. |
| Kingdom membership / `MapFaction` | **Clan** | A clan has exactly one kingdom; heroes cannot individually defect without leaving the clan. |
| Relations (`ChangeRelationAction`) | **Hero** | Already hero-level — a better fit than today. |
| Parties (`MobileParty.LeaderHero`) | **Hero**-led | Levies could become hero-to-hero indentures (historically truer: bastard feudalism). |
| Encyclopedia UI | Both | Hero page mixin already exists; clan page would aggregate members' titles. |

### Recommended shape: heroes in the domain, collapse at the adapters

- `Title.HolderHeroId` becomes the stored truth; `HolderClanId` becomes a *derived* view
  (resolve hero → clan at the adapter). Suzerain/vassal queries exist at both granularities —
  the clan-level ones derived from members' titles — so existing consumers (election chooser,
  support weights, clan UI) keep working during migration.
- **Prerequisite:** the de jure/de facto split from `feudal-title-transfer-design.md`. While
  "title follows seat" is instant and seats are clan-owned, a hero-held dignity can never
  diverge from clan ownership and the change buys nothing. Decouple transfer from conquest
  first; hero holders then slot in naturally.

## 2. Two or more titled heroes in one clan

Falls out of §1 for free at the domain level — titles key by hero, nothing prevents brothers
holding a county and a barony each. The interesting consequences:

- **Split allegiance becomes expressible.** Brother A holds a barony under the Duke of York;
  brother B a county under the crown. Per-hero suzerainty differs while engine kingdom
  membership (clan-level, single-valued) stays whatever the clan leader's politics are. That
  divergence is not a bug — it is the Wars of the Roses (Nevilles on both sides), and it is
  precisely the tension fuel that internal-conflict and private-war mechanics want.
- **Collapse rule needed** wherever the engine asks "the clan's position": use the **clan
  leader's** titles for kingdom-level effects (influence, election weight), individual titles
  for hero-level effects (relations, claims, succession).

## 3. Impact on private wars

The recommended private-war design (`feudal-system-analysis.md` Part B, option b: abstract
war score, engine never sees a war) is **helped, not hurt**:

- Because it is abstract, the `MapFaction` obstacle never applied — and therefore nothing
  stops the belligerents being *heroes* instead of clans. `PrivateWar.AttackerClanId/
  DefenderClanId` become hero ids; war-score ticks compare hero party/retinue strength
  instead of `Clan.TotalStrength`.
- **Intra-clan conflict becomes possible** — a succession war between two brothers of the
  same clan is just a private war whose belligerents share a clan. Pure domain state; the
  engine is structurally incapable of representing it, which is exactly why the abstract
  design is the right substrate.
- The one engine touchpoint remains outcome application: transferring a *seat* still needs
  `ChangeOwnerOfSettlementAction` (clan-level). But with the de jure/de facto split, many
  private-war outcomes transfer only the **dignity** (hero-level, pure domain) and never
  touch settlement ownership — fewer engine calls, fewer edge cases.

## 4. "Clans within clans"

Two readings, opposite feasibility:

### (a) Nested clans in-engine — reject

Bannerlord has no sub-clan concept; everything from party ownership to influence to kingdom
membership assumes a flat clan list. Faking nesting under one engine clan means reimplementing
allegiance, hostility and elections per-member — against the engine, Harmony-heavy.

### (b) Houses *above* clans — the map data already does this

Invert it: keep engine clans small (one titled line each) and add a domain **House/Dynasty**
grouping over them. The DADG map data already models cadet branches as separate engine clans:

- House Neville → `clan_neville_of_raby`, `clan_neville_of_middleham`, `clan_neville_of_bergavenny`
- House Grey → `clan_grey_of_ruthin`, `clan_grey_of_codnor`, `clan_grey_of_wilton`
- House Scrope → `clan_scrope_of_bolton`, `clan_scrope_of_masham`
- House Fiennes → `clan_fiennes_of_saye_and_sele`, `clan_fienne_of_dacre`

So "clans within clans" is best modelled as a static `houses.config.xml` (house id → member
clan ids), a pure domain concept feeding: kin-aware support weights in elections and private
wars (kin clans cheaper to rally, costlier to fight), inheritance claims crossing branch
lines (already half-true: `CollectPassedOverHeirClanIds` looks at relatives in *other*
clans), and house-level UI grouping in the hierarchy screen.

### (c) Optional later: runtime cadet-branch spinoff

When a second son holds a major title (county+), an action could spin his line off into a new
engine clan within the same house — converting an intra-clan titled hero into an
engine-visible clan exactly when the engine needs one (parties, elections, kingdom politics),
which is also what history did (Neville of Middleham *was* a cadet spinoff). Precedent for
runtime clan creation exists (`RebellionsCampaignBehavior` spawns rebel clans; the Diplomacy
mod creates kingdoms via `KingdomManager.CreateKingdom`), but the exact official API for
creating a lord clan mid-campaign is **UNVERIFIED** — smoke-test before committing to (c).

## 5. Sequencing

Each step is additive on the previous; stopping at any point leaves a coherent system:

1. **De jure/de facto holder split** (`feudal-title-transfer-design.md`, Option C) — the
   enabler; without it hero titles cannot diverge from clan-owned seats.
2. **`HolderHeroId` migration** — domain id sweep + succession use case replacing the
   clan-claims approximation; adapters collapse hero → clan; both-granularity queries.
3. **Houses config** — static grouping over existing clans; kin-aware weights; UI grouping.
4. **Private wars** (already designed) — now lands with hero-grade belligerents and intra-clan
   succession wars nearly free.
5. *(Optional)* **Cadet spinoff action** — only if (c)'s API check passes and the gameplay
   wants it.

## 6. Open questions

- Succession rule for step 2: strict male primogeniture (period-correct), or configurable?
- Do *all* titles move to heroes, or only county+ (baronies staying clan-held retainer
  rewards)? A mixed model halves the UI churn.
- When a titled hero dies heirless, does the title revert to the clan leader, the house, or
  the crown (escheat — historically the crown)?
- Should levies become hero-to-hero indentures in step 2, or stay clan-to-clan until private
  wars need the finer grain?