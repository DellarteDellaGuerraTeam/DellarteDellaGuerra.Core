# Church v0.4 — Bishops & the Church's Favour

Fourth slice of the DADG religious system, building on v0.1 (church villages, abbots, donations),
v0.2 (mass, tithe, sacrilege) and v0.3 (sanctuary). The hierarchy begins: the three cathedrals
get bishops, and the Church forms an opinion of the player — a piety-like reputation derived from
what already exists, not a new tracked score.

**Out of scope (later/cut):** dioceses (regional structure mapping abbeys to their cathedral),
an archbishop/primate character, excommunication and denunciation of raid-happy AI lords (the
v0.2 hook this sets up — v0.5+), any AI-facing favour effects, piety as an accumulating stat
(deliberately derived instead, see §2), any new XML or scene work.

## 1. Bishops (hierarchy tier one)

The cathedral clergy notable *is* the bishop. No new hero spawns: the three cathedral preachers
from v0.1 (Ely, Llandaff, St Asaph) are retitled from **Dean** to **Bishop** — hierarchy arrives
as rank, not as another body in the village.

- `ChurchSettlements.GetClergyTitle` returns *Bishop* (new localization id, replacing the Dean
  entry `{=hN6cRw3B}`) for `ChurchSettlementKind.Cathedral`. Abbot/Prior unchanged.
- Update the v0.1 doc's clergy-title table (Dean → Bishop) when implementing.
- `ChurchSettlements` exposes the kind (e.g. `IsCathedral(Settlement)`) so dialog can gate
  bishop-only lines. No other structural change.

## 2. The Church's favour (derived piety)

One number: the average of the player's relation with every living clergy notable across the 16
church settlements. Everything the player already does — donations (+), mass (+), sacrilege
(−15/−5 cascade) — already moves those relations, so favour needs **zero new save state** and is
always consistent with the world. No clergy alive → average is 0.

Domain (pure, tested):

- `src\DellarteDellaGuerra.Domain\Church\Favour\ChurchFavourPolicy.cs` —
  `Evaluate(float averageRelation)` → `ChurchFavourRank`:

| Rank | Range |
|---|---|
| `Reviled` | avg ≤ −10 |
| `IllRegarded` | −10 < avg ≤ −2 |
| `Indifferent` | −2 < avg < +2 |
| `Favoured` | +2 ≤ avg < +10 |
| `Beloved` | avg ≥ +10 |

- Thresholds are identity constants, not config knobs.
- `ChurchFavourPolicyTests` — boundary cases at −10, −2, +2, +10.

Bannerlord side: computed on demand in the dialog behavior by iterating church settlements'
living preachers and averaging the player relation. **Verify the relation-read API during
implementation** (expected `Hero.GetRelation(Hero)` or
`CharacterRelationManager.GetHeroRelation` in 1.4.6 — check decompiled sources; must be the
same value `ChangeRelationAction` writes).

## 3. Bishop dialog

Two new player lines from the existing `dadg_church_talk` hub in `AbbotDialogCampaignBehavior`
(same conversation tree — the greeting/donation/blessing flow is shared by all clergy; these two
lines are simply hidden unless the conversation partner is a cathedral's bishop). This keeps one
dialog tree in one class; no new behavior unless state forces it — the single
`_lastBishopBlessingTime` (`CampaignTime`) joins `AbbotDialogCampaignBehavior`'s `SyncData`.

1. **"How does the Church regard me, Your Grace?"** — always visible at bishops. Reply is one of
   five lines keyed by `ChurchFavourPolicy` rank:
   - Beloved: *"All England's cloisters speak your name with love. You are a true friend of Holy Church."*
   - Favoured: *"The Church counts you among her faithful {?PLAYER.GENDER}daughters{?}sons{\?}."*
   - Indifferent: *"The Church knows little of you, my {?PLAYER.GENDER}lady{?}lord{\?}. Works, not words, commend a soul."*
   - IllRegarded: *"There is murmuring against you in the chapter houses. Mend your ways."*
   - Reviled: *"You stand in the shadow of anathema. Repent, before God and His Church."*
   → back to `dadg_church_talk`.
2. **"Grant me your blessing, Your Grace."** — visible only when rank ≥ `Favoured` **and** 7 or
   more in-game days since the last bishop's blessing (hidden otherwise, like the donate line on
   cooldown). Reply: *"Kneel, then. May God make you strong in battle and merciful in victory."*
   Consequence: `MobileParty.MainParty.RecentEventsMorale += BlessingMorale` (config, default
   **5**), `GainRenownAction.Apply(+1)` (fixed), record `_lastBishopBlessingTime`.
   → back to `dadg_church_talk`.

Eligibility is a pure policy:
`src\DellarteDellaGuerra.Domain\Church\Favour\BishopBlessingPolicy.cs` —
`Evaluate(ChurchFavourRank rank, float? daysSinceLastBlessing)` →
`Allowed | NotFavoured | OnCooldown` (cooldown 7 days, fixed — mirrors `DonationPolicy`).
`BishopBlessingPolicyTests` — rank gate, cooldown boundary, first-ever blessing (null days).

All new strings use fresh inline `{=id}` localization ids in the established 8-char style; no
existing id changes meaning except the Cathedral title, which gets a **new** id for *Bishop*
(do not reuse `{=hN6cRw3B}` — its translated value is "Dean").

## 4. Configuration

One new knob in `<ChurchConfig>` (config/dadg.config.xml), same plumbing as the other eight
(`ChurchConfig` XML model → `ChurchSettings` → `ChurchSettingsConfig`), hot-reloaded:

| Element | Default | Meaning |
|---|---|---|
| `BlessingMorale` | 5 | Party morale gained from a bishop's blessing (v0.4) |

Update the v0.2 doc's §4b table with this row. Deliberately not configurable: favour thresholds,
blessing renown (+1), blessing cooldown (7 days).

## 5. Code layout

- `src\DellarteDellaGuerra.Domain\Church\Favour\ChurchFavourPolicy.cs` (+ rank enum) and
  `BishopBlessingPolicy.cs` — pure, no ports needed (plain-value inputs, like
  `DonationPolicy`/`MassPolicy`).
- `src\DellarteDellaGuerra.Domain.Tests\ChurchFavourPolicyTests.cs`, `BishopBlessingPolicyTests.cs`.
- `src\DellarteDellaGuerra\Church\ChurchSettlements.cs` — Bishop title, `IsCathedral`.
- `src\DellarteDellaGuerra\Church\Api\Campaign\AbbotDialogCampaignBehavior.cs` — two bishop
  lines, favour computation, `_lastBishopBlessingTime` in `SyncData`
  (key `"_dadgChurchLastBishopBlessingTime"`).
- `ChurchConfig`/`ChurchSettings`/`ChurchSettingsConfig` — `BlessingMorale`.
- No DI or SubModule changes (no new services, no new behaviors).

## 6. Verification

1. Unit: new policy tests green; all Domain tests pass (13 existing + new).
2. Build Debug via `subst` short path + `dotnet test --no-build` (memory/worktree-build-quirks).
3. Live playtest (deferred with v0.1–v0.3's):
   - At Ely Cathedral the clergy is addressed as Bishop; abbeys/priories unchanged.
   - Fresh campaign (avg 0): favour question → Indifferent reply; no blessing line.
   - Donate a few times / attend mass until avg ≥ +2: Favoured reply; blessing line appears;
     blessing → morale +5 visible, renown +1, line gone for 7 days, cooldown survives save/load.
   - Raid an abbey (−15/−5 cascade): favour drops, reply changes to IllRegarded/Reviled.

## 7. Risks

- **Relation API mismatch**: if the read API returns clamped/display relation while
  `ChangeRelationAction` writes raw, thresholds shift subtly. Verify read/write symmetry in
  decompiled sources before wiring.
- **Average dilution**: 16+ clergy means one donation moves the average by ~+0.13 — reaching
  Favoured legitimately takes ~15 donations/masses, Beloved a long campaign. That slow burn is
  intended; if playtest shows it too grindy, tune thresholds (they are in one Domain file).
- **Dialog crowding**: the `dadg_church_talk` hub now has up to four player options at bishops.
  Acceptable; revisit if v0.5 adds more.
