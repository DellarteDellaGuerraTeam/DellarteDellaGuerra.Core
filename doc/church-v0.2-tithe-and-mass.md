# Church v0.2 — Tithe & Mass

Second slice of the DADG religious system, building on v0.1 (see
`church-v0.1-monasteries.md`: 16 church villages, abbot notables, donation dialog). Three
mechanics, all campaign-behavior work — no scene edits, no new UI, no XML changes.

**Out of scope (later):** sanctuary (v0.3), hierarchy characters/piety-like reputation (v0.4+),
ambient churchgoers in village scenes (cut — scene work, low payoff), any change to village
tax/gold flows (cut — abbot Power growth tells the "rich Church" story without touching the
economy).

All Bannerlord APIs below verified against decompiled 1.4.6 sources.

## 1. Sunday mass (village menu option)

A new option on the `"village"` game menu, visible only in church villages.

- Registration: `CampaignGameStarter.AddGameMenuOption("village", "dadg_church_attend_mass",
  text, condition, consequence)` on `OnSessionLaunched`.
- **Sunday** = `CampaignTime.Now.GetDayOfWeek == 0` (Bannerlord has 7-day weeks in normal mode,
  3-day in fast-forward mode; day 0 exists in both, and vanilla has no weekday names, so day 0
  is our Sunday by convention).
- Condition:
  - Hidden unless `ChurchSettlements.IsChurchSettlement(Settlement.CurrentSettlement)`.
  - Disabled unless village state is `Normal` (mirror the vanilla "Take a walk" condition).
  - Disabled when not Sunday or when already attended today — use
    `MenuHelper.SetOptionProperties(args, enabled, !enabled, tooltip)` with tooltip
    *"Mass will be held on the Lord's day. ({N} {?N>1}days{?}day{\?} hence)"*.
  - `args.optionLeaveType = GameMenuOption.LeaveType.Continue` (menu action, not a mission).
- Consequence:
  - `MobileParty.MainParty.RecentEventsMorale += 4` — decays multiplicatively at 10%/day
    (verified in `MobileParty.DailyTick`), so the buff is ~+2.4 by the next Sunday and weekly
    attendance settles around +7.5 total: noticeable, self-limiting, no cap concerns (clamped
    ±100). Shows up in the party morale tooltip under recent events for free.
  - +1 relation with the resident abbot (`ChangeRelationAction.ApplyPlayerRelation`).
  - Record attendance time; once per Sunday globally (a single `CampaignTime _lastMassTime`
    in `SyncData` — you attend one mass, not a tour of them).

## 2. Tithe (weekly abbot power growth)

Parish tithes flow to the monastery; mechanically, abbots accumulate notable **Power**, which
vanilla already displays on the settlement overlay card and hero tooltip ("Power: Influential
(142)") — visible progression with zero UI work.

- `CampaignEvents.WeeklyTickEvent` (parameterless `Action`): every living preacher notable in a
  church settlement gets `hero.AddPower(+2)`.
- Dynamics (verified in `DefaultNotablePowerModel`): preachers have no passive daily power
  change; above Power 100 a correction of −(Power−100)/500 per day applies. With +2/week the
  long-run equilibrium is ≈240 — abbots trend from their random spawn power (50–400) toward
  the **"Powerful"** rank (200+) over a campaign. Exactly the fantasy: monasteries as
  quietly mighty landowners.
- v0.1 amendment folded in: the donation consequence also grants the abbot `AddPower(+5)`, so
  patron players visibly enrich their abbey and the two systems compound.

## 3. Sacrilege (raiding a church village)

- Hook `CampaignEvents.VillageLooted` (`Action<Village>`, fires when the raid completes — safer
  than `VillageBeingRaided`, which fires at raid start).
- Raider identity: `village.Settlement.LastAttackerParty` (verified vanilla pattern); its
  `LeaderHero` is the raiding lord. Player detection: `LastAttackerParty == MobileParty.MainParty`.
- If the looted village is a church settlement and the raider hero is alive:
  - **−15 relation** with that village's abbot, **−5 relation** with every other living church
    abbot. Player raider via `ChangeRelationAction.ApplyPlayerRelation`; AI lords via
    `ChangeRelationAction.ApplyRelationChangeBetweenHeroes` — so raid-happy AI lords quietly
    become hated by the Church, which v0.4's hierarchy can exploit.
  - Player raider additionally gets an information message: *"Word of your sacrilege at
    {SETTLEMENT} spreads among the clergy of England."*
- No extra power penalty: vanilla already docks notables −5 Power per raid.

## 4. Code layout

Extend the existing feature (no new projects/folders beyond one Domain file):

- `src\DellarteDellaGuerra.Domain\Church\Mass\MassPolicy.cs` — pure: eligibility
  (`isSunday`, `attendedToday` → outcome);
  `src\DellarteDellaGuerra.Domain.Tests\MassPolicyTests.cs`.
- Tithe and sacrilege have no branching logic worth a policy class — the behavior reads the
  amounts (mass morale/relation, tithe/donation power, sacrilege relations) from `ChurchSettings`
  (see Configuration below).
- `src\DellarteDellaGuerra\Church\Api\Campaign\ChurchCampaignBehavior.cs` — `WeeklyTickEvent`
  and `VillageLooted` listeners (tithe, sacrilege trigger). The mass menu option and
  `_lastMassTime` in `SyncData` were later split into `ChurchMassCampaignBehavior.cs`, the
  sacrilege cascade into the `ChurchSacrilege` injected service
  (`src\DellarteDellaGuerra\Church\ChurchSacrilege.cs` — owns its `IChurchSettingsProvider`
  lookup, so callers just call `Apply(offender, site)`), and the +5 power line in `Donate()`
  into `AbbotDialogCampaignBehavior.cs`.

## 4b. Configuration (added with the config pass, 2026-07-19)

The eight balance values across v0.1/v0.2 are configurable in `config/dadg.config.xml` under
`<ChurchConfig>` (sibling of `<JoustingConfig>`), hot-reloaded by `DadgConfigWatcher`; any
missing field/section/file falls back to the defaults below (property initializers on
`ChurchConfig`):

| Element | Default | Meaning |
|---|---|---|
| `DonationCost` | 500 | Gold cost of a donation (v0.1) |
| `DonationRelation` | 2 | Relation gained with the abbot per donation (v0.1) |
| `MassRelation` | 1 | Relation gained with the abbot per mass |
| `MassMorale` | 4 | Party morale gained per mass |
| `SacrilegeRelationLocal` | −15 | Relation with the wronged site's abbot |
| `SacrilegeRelationOthers` | −5 | Relation with every other church abbot |
| `WeeklyTithePower` | 2 | Abbot Power gained per week |
| `DonationPower` | 5 | Abbot Power gained per donation |

Deliberately **not** configurable (identity constants): donation renown (+1), the 7-day
donation cooldown, and the 40/20-day sanctuary durations (v0.3).

Wiring follows the joust pattern: Domain port `IChurchSettingsProvider` → immutable
`ChurchSettings` → Infrastructure `ChurchSettingsConfig(IConfigurationProvider<DadgConfig>)` →
registered in `DadgServiceContainer.RegisterChurchServices`, constructor-injected into the church
behaviors and into the `ChurchSacrilege` service (which owns the settings lookup for the sacrilege
cascade). Policies stay pure — `DonationPolicy.Evaluate` takes the cost as a parameter.

## 5. Verification

1. Unit: `MassPolicyTests` — not Sunday, Sunday first time, Sunday already attended.
2. Build Debug + `dotnet test --no-build` (worktree: init submodules, build via `subst` short
   path — see memory/worktree-build-quirks).
3. Live playtest:
   - Non-church village: no mass option. Church village on non-Sunday: option greyed with
     countdown tooltip; day 0: enabled.
   - Attend: morale +4 visible in party morale tooltip; option disabled for the rest of the day;
     re-enabled next Sunday; `_lastMassTime` survives save/load.
   - Let a week pass: abbot Power on the village overlay card increases by 2.
   - Raid Tintern Abbey: relation −15 with its abbot, −5 with another abbey's abbot, sacrilege
     message shown. Have an AI lord raid one (or simulate): relations shift without errors.

## 6. Risks

- Menu option placement: default `index -1` appends at the end of the village menu; tune index
  during playtest if it lands awkwardly.
- `WeeklyTickEvent` fires when elapsed campaign days ≡ 0 mod 7, which need not coincide with
  our "Sunday" (day-of-week 0 of absolute time). Harmless for tithe (any weekly cadence works);
  don't reuse it for mass logic.
- Fast-forward campaign mode shortens weeks to 3 days: mass becomes more frequent and tithe
  ticks faster. Accepted; not worth special-casing.
