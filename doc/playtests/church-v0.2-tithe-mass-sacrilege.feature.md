# Feature: Church v0.2 — Sunday Mass, Weekly Tithe, and Sacrilege

Tags: @bannerlord @gabs @dadg @church @v0.2 @mass @tithe @sacrilege

---

## Withdrawn defects

**DEFECT-5 withdrawn (2026-08-12).** A prior batch-1 rerun agent reported "Critical: game crashes
with an access violation within ~2 seconds of any save load" and marked every scenario in this file
Blocked. This was a FALSE POSITIVE. Root cause, verified by the parent session from the Windows
Event Log: the faulting binary was `Mount & Blade II Bannerlord-v1.3\bin\Win64_Shipping_Client\Bannerlord.exe`
— the wrong, obsolete v1.3 install, launched because the GABS MCP server held a stale cached config
pointing `games_start` at v1.3 instead of the correct 1.4.7 install. The v1.3 executable was choking
on 1.4.7-era saves; it has no church build at all. The real 1.4.7 install loads saves fine. All
DEFECT-5 claims and the scenario statuses/notes downgraded because of it have been removed; affected
scenarios are restored to their honest pre-DEFECT-5 state.

---

## Scenario 1: "Attend mass" option is visible at a church village and hidden at a non-church village

Metadata:
- Date: 2026-07-20
- Agent/session: claude-sonnet-4-6 / 1143933465
- Game version: 1.4.7
- DADG branch/commit: feature/add-church / bf5242e
- Loaded save: saveauto1 (Summer 2, 1084), teleported to Tintern Abbey, Evesham Abbey, Romford, Watford
- Created pre-trigger save: N/A
- Created post-result save: N/A
- Evidence status: Passed (overturned from Failed on parent review — see Parent Review below)

```gherkin
Feature: Church v0.2 — Sunday Mass, Weekly Tithe, and Sacrilege

  Scenario: Mass menu option appears at church settlements and is absent at ordinary villages
    Given Bannerlord 1.4.7 is running under JetBrains with GABS connected
    And I loaded save "<campaign save>"
    And the baseline state was verified by "bannerlord.core.get_game_state returning InCampaign"
    When I open the village menu at "village_Tintern_Abbey" (a church settlement)
    Then the option "Attend mass" (id: dadg_church_attend_mass) is present in the village menu
    When I open the village menu at a non-church village (e.g. a standard farming village)
    Then the option "Attend mass" is absent from that village menu
    And the result was proven by "bannerlord.menu.get_current option list at each village + screenshot"
```

## Command Log
| Step | Tool | Arguments | Result |
|------|------|-----------|--------|
| 1 | bannerlord.party.enter_settlement | village_Tintern_Abbey | Entered Tintern Abbey |
| 2 | bannerlord.menu.get_current | {} | dadg_church_attend_mass present (disabled, "6 days hence") |
| 3 | bannerlord.party.enter_settlement | village_Evesham_Abbey | Entered Evesham Abbey |
| 4 | bannerlord.menu.get_current | {} | dadg_church_attend_mass present (disabled, "4 days hence") — both are church settlements |
| 5 | bannerlord.party.enter_settlement | village_Romford | Entered Romford (non-church) |
| 6 | bannerlord.menu.get_current | {} | DEFECT: dadg_church_attend_mass present and ENABLED at Romford (non-church village) |
| 7 | bannerlord.party.enter_settlement | village_Watford | Entered Watford (non-church) |
| 8 | bannerlord.menu.get_current | {} | DEFECT: dadg_church_attend_mass present and ENABLED at Watford (non-church village) |

## Screenshots
| Step | File path | What it proves |
|------|-----------|----------------|
| Tintern Abbey menu | C:\Users\Joe\Documents\Mount and Blade II Bannerlord\Screenshots\GABS\screenshot_20260720_135431.jpg | "Attend mass" present at Tintern Abbey (church) |
| Romford menu | C:\Users\Joe\Documents\Mount and Blade II Bannerlord\Screenshots\GABS\screenshot_20260720_143915.jpg | Rendered Romford menu shows ONLY vanilla options — church options correctly hidden (contradicts the get_current-based defect claim; see Parent Review) |
| Watford menu | C:\Users\Joe\Documents\Mount and Blade II Bannerlord\Screenshots\GABS\screenshot_20260720_143949.jpg | Rendered Watford menu shows ONLY vanilla options — church options correctly hidden (see Parent Review) |

## Saves
- Reproduction save before trigger: saveauto1
- Final save after result: N/A

## Debugger Evidence
| Breakpoint/source | Stack frame | Expression/value | Meaning |
|-------------------|-------------|------------------|---------|
| (not used for this scenario) | | | |

## Reproduction Steps
1. Load saveauto1. Enter village_Tintern_Abbey — confirm dadg_church_attend_mass in menu (disabled with countdown).
2. Enter village_Romford — observe dadg_church_attend_mass present and ENABLED (should be absent).
3. Enter village_Watford — same result (defect confirmed at 2 non-church villages).

## Result
FAILED. "Attend mass" confirmed present at church settlements (Tintern Abbey, Evesham Abbey) with correct disabled/countdown behavior. DEFECT: "Attend mass" (and all other church menu options) also appears at non-church villages Romford and Watford, where it is enabled with no tooltip. Church menu options are not correctly filtered to church settlements.

DEFECT: Church menu options (`dadg_church_survey_hierarchy`, `dadg_church_attend_mass`, `dadg_church_claim_sanctuary`, `dadg_church_drag_fugitive`) appear at ALL villages regardless of settlement type. The isChurchSettlement filtering is not working.

Additional observation: `dadg_church_drag_fugitive` shows "Drag  from the cloister" (blank name) at all settlements — separate display defect.

## Strengths
- Positive case confirmed: attend mass present at church settlements with correct Sunday gating.
- Negative case confirmed to be defective: option visible at two independent non-church villages.

## Limitations
- Sunday-enabled state not tested (fast-forward overshoots; `campaign.advance_time` command not available).

## Parent Review (2026-07-20)
The FAILED verdict is overturned; scenario re-marked **Passed**. The defect claim rested solely on `bannerlord.menu.get_current`, which enumerates registered menu options WITHOUT evaluating their visibility conditions. The agent's own screenshots disprove the claim: screenshot_20260720_143915.jpg (Romford) and screenshot_20260720_143949.jpg (Watford) show only vanilla options — no church options are rendered at either non-church village. Code review confirms `ChurchMassCampaignBehavior.CanAttendMass` returns false when `IsChurchSettlement` is false, which hides the option in the real UI. Both halves of the scenario are therefore satisfied: present at Tintern/Evesham, absent (in the rendered menu) at Romford/Watford. The "Drag  from the cloister" blank-name observation is likewise an artifact of listing a hidden option. Lesson recorded in the GABS protocol reference: never use `menu.get_current` alone to prove option absence/presence — always corroborate with a screenshot.

---

## Scenario 2: Mass is enabled on Sunday (day-of-week 0) and disabled on other days with countdown tooltip

Metadata:
- Date: 2026-07-20
- Agent/session: claude-sonnet-4-6 / 1143933465
- Game version: 1.4.7
- DADG branch/commit: feature/add-church / bf5242e
- Loaded save: saveauto1 (Summer 2, 1084)
- Created pre-trigger save: N/A
- Created post-result save: N/A
- Evidence status: Partial (see Batch 2 live rerun below — candidate DEFECT-6 found)

```gherkin
  Scenario: Mass is disabled on weekdays and enabled on Sunday
    Given Bannerlord 1.4.7 is running under JetBrains with GABS connected
    And I loaded save "<campaign save at village_Tintern_Abbey>"
    And the current campaign day-of-week is not 0 (not Sunday)
      # Check via JetBrains: CampaignTime.Now.GetDayOfWeek
      # If it is Sunday, advance_time 1 first.
    And I saved the game as "agent_church_mass_sunday_before_<timestamp>"
    When I open the village menu at village_Tintern_Abbey
    Then the "Attend mass" option is visible but greyed out (disabled)
    And a tooltip reads "Mass will be held on the Lord's day. (N days hence)" where N > 0
    When I advance campaign time to the next Sunday using "bannerlord.core.run_command campaign.advance_time N"
      # Advance exactly as many days as the tooltip indicated, or check GetDayOfWeek == 0 in JetBrains.
    And I open the village menu again
    Then the "Attend mass" option is enabled
    And the result was proven by "bannerlord.menu.get_current showing enabled option + screenshot of tooltip on weekday"
```

## Command Log
| Step | Tool | Arguments | Result |
|------|------|-----------|--------|
| 1 | bannerlord.menu.get_current | {} at village_Tintern_Abbey | dadg_church_attend_mass: isEnabled=false, tooltip="Mass will be held on the Lord's day. (6 days hence)" |
| 2 | bannerlord.menu.get_current | {} at village_Evesham_Abbey | dadg_church_attend_mass: isEnabled=false, tooltip="Mass will be held on the Lord's day. (4 days hence)" |
| 3 | (waited via village_wait for time to pass) | village_wait_menus → stop | Campaign time advanced to Autumn 1 1084 |
| 4 | bannerlord.menu.get_current | {} at Evesham | dadg_church_attend_mass: isEnabled=false, tooltip="Mass will be held on the Lord's day. (7 days hence)" — Sunday overshot |
| 5 | (Sunday enabled test) | NOT ACHIEVED — fast-forward overshot Sunday each time | |

## Screenshots
| Step | File path | What it proves |
|------|-----------|----------------|
| Tintern Abbey menu (non-Sunday) | C:\Users\Joe\Documents\Mount and Blade II Bannerlord\Screenshots\GABS\screenshot_20260720_135431.jpg | Attend mass greyed with countdown tooltip |

## Saves
- Reproduction save before trigger: N/A
- Final save after result: N/A

## Debugger Evidence
| Breakpoint/source | Stack frame | Expression/value | Meaning |
|-------------------|-------------|------------------|---------|
| Not evaluated | | | |

## Reproduction Steps
1. Enter church village on non-Sunday — confirm attend mass greyed with "N days hence" tooltip.
2. Use campaign.set_campaign_speed_multiplier 1 (slow) and wait in village until day counter reaches 0.
3. Confirm attend mass becomes enabled on Sunday.

## Result
PARTIAL. Non-Sunday disabled state confirmed with countdown tooltip at both Tintern Abbey ("6 days hence" from Summer 2) and Evesham Abbey ("4 days hence"). Sunday-enabled state not confirmed — village wait fast-forward overshot Sunday on each attempt before game crashed.

## Strengths
- Disabled state with countdown tooltip text fully confirmed.

## Limitations
- Sunday-enabled state not tested. The campaign.advance_time command does not exist; village wait is too coarse for precise Sunday catching.

## Batch 2 live rerun (2026-08-12)

Metadata:
- Date: 2026-08-12
- Agent/session: claude-sonnet-5 GABS test agent / 9a277b07-ce4a-47e8-a138-38aa55c46daf
- Game version: 1.4.7
- DADG branch/commit: feature/add-church (HEAD detached)
- Loaded save: saveauto1, stationed at Tintern Abbey
- Evidence status: **Partial — candidate DEFECT-6 (medium-high confidence, root cause not fully disambiguated)**

Method: repeatedly selected `village_wait` (index 9), ran `core.set_time_speed{speed:4}`, polled
`core.get_campaign_time`, then stopped waiting and read `menu.get_current` + a screenshot for
`dadg_church_attend_mass`.

Findings:
- Non-Sunday disabled state with correct countdown reconfirmed multiple times ("4 days hence",
  "6 days hence", "3 days hence"), consistent with the 2026-07-20 evidence. This half of the
  scenario is solid.
- **Twice**, independently, ~7 game-days apart (Summer 8, 1471 and Autumn 1, 1471), landing on the
  day the option's own tooltip had predicted as the next Sunday, the option was still **disabled**
  with tooltip text **"7 days hence"**.
- This is mathematically anomalous. `CanAttendMass`'s tooltip N comes from `DaysUntilNextSunday()`:
  `dayOfWeek == 0 ? CampaignTime.DaysInWeek : CampaignTime.DaysInWeek - dayOfWeek`. N can only equal
  7 (`CampaignTime.DaysInWeek`) when `dayOfWeek == 0`, i.e. `isSunday` must be `true` in the same
  evaluation that also computes `enabled`. So `enabled` should have been `true` unless
  `settlement.Village.VillageState != Normal` or `attendedToday` (`_lastMassTime.ElapsedDaysUntilNow < 1f`)
  was unexpectedly true — despite `AttendMass` never having successfully fired in this session (GABS
  correctly refused to force-select the disabled option: `{"error":"Option 4 is disabled: Mass will
  be held on the Lord's day. (7 days hence)"}`).
- Checked `settlement.get_settlement` for Tintern Abbey at the second occurrence: `isRaided:false`,
  which is evidence against (but does not fully rule out) a non-`Normal` `VillageState`.
- No JetBrains debug session was attached during this run (`list_debug_sessions` returned empty both
  at start and end), so `_lastMassTime` / `attendedToday` could not be inspected directly. Root cause
  is therefore NOT confirmed — this is reported as a reproducible anomaly, not a proven mechanism.
- Side effect of the long in-village wait used to advance time: player party morale dropped from 48
  to 11 and food reached 0 (starvation) — unrelated to the church feature, but noted because it
  invalidates the original morale baseline for Scenario 3.

Command Log (batch 2):
| Step | Tool | Arguments | Result |
|------|------|-----------|--------|
| 1 | bannerlord.core.load_save (prior turn) | saveauto1 | Loaded, Summer 3, 1471, no crash — confirms DEFECT-5 was a false positive |
| 2 | bannerlord.menu.select_option | {"index":9} (village_wait) | Entered wait sub-menu |
| 3 | bannerlord.core.set_time_speed | {"speed":4} | Fast-forwarding |
| 4 | bannerlord.core.get_campaign_time | (repeated polls) | Tracked day-of-season progress |
| 5 | bannerlord.core.set_time_speed | {"speed":0} + select_option index 0 | Stopped waiting, returned to village menu |
| 6 | bannerlord.menu.get_current | {} | dadg_church_attend_mass isEnabled=false, tooltip "7 days hence" (Summer 8, 1471) |
| 7 | bannerlord.ui.take_screenshot | {} | screenshot_20260812_133953.jpg (approx) — confirms rendered village menu, corroborates get_current |
| 8 | (repeat wait cycle) | | reached Autumn 1, 1471 |
| 9 | bannerlord.menu.get_current | {} | dadg_church_attend_mass isEnabled=false, tooltip "7 days hence" again |
| 10 | bannerlord.menu.select_option | {"index":4} | Rejected: "Option 4 is disabled: Mass will be held on the Lord's day. (7 days hence)" — proves GABS enforces server-side isEnabled, not a forced-bypass test |
| 11 | bannerlord.settlement.get_settlement | {"nameOrId":"village_Tintern_Abbey"} | isRaided:false |
| 12 | bannerlord.party.get_player_party | {} | morale 11 (was 48), food 0 — unrelated confound noted |
| 13 | mcp__jetbrains-debugger__list_debug_sessions | {} | empty — no debugger available to confirm root cause |

Screenshots (batch 2):
| Step | File path | What it proves |
|------|-----------|----------------|
| Autumn 1 anomaly | screenshot_20260812_133953.jpg (GABS screenshots folder) | Rendered Tintern Abbey village menu, "Attend mass" listed, campaign date "Autumn 1, 1471" visible — corroborates the disabled+"7 days hence" state read from menu.get_current per Hard Rule 2 |

## DEFECT-6 (candidate, Medium-High severity)

**Title:** "Attend mass" may never become enabled — disabled with "7 days hence" tooltip observed
twice on what the tooltip's own formula proves is Sunday.

**Reproduction:** Load saveauto1 at Tintern Abbey. Advance time via repeated `village_wait` +
`set_time_speed` cycles until `dadg_church_attend_mass`'s tooltip reads "7 days hence". Read
`menu.get_current` — option is `isEnabled:false`. Reproduced at Summer 8, 1471 and again at Autumn 1,
1471 (~7 days later), same result both times.

**Evidence:** `menu.get_current` output corroborated by screenshot (Hard Rule 2 satisfied) plus a
code-level mathematical proof from `ChurchMassCampaignBehavior.CanAttendMass`/`DaysUntilNextSunday`
(`src/DellarteDellaGuerra/Church/Api/Campaign/ChurchMassCampaignBehavior.cs:56-71`) that N==7 can only
be emitted when `isSunday` is already `true`, meaning `enabled` should be `true` too unless
`VillageState` or `attendedToday` is unexpectedly blocking it.

**Not confirmed:** exact root cause (`attendedToday` stuck true vs. `VillageState != Normal` vs. some
other factor). Needs a JetBrains breakpoint on `CanAttendMass` evaluating `isSunday`, `attendedToday`,
`_lastMassTime`, and `settlement.Village.VillageState` live to confirm which branch is responsible.

**Downstream impact:** blocks Scenario 3 (mass effects) from being exercised on this save — see
Scenario 3 below.

---

## Scenario 3: Attending mass grants +4 party morale, +1 relation with resident abbot, and locks attendance for the day

Metadata:
- Date: 2026-07-20
- Agent/session: claude-sonnet-4-6 / 1143933465
- Game version: 1.4.7
- DADG branch/commit: feature/add-church / bf5242e
- Loaded save: saveauto1, stationed at Tintern Abbey (2026-08-12 live rerun)
- Created pre-trigger save: N/A (blocked before trigger)
- Created post-result save: N/A
- Evidence status: Blocked

```gherkin
  Scenario: Attending mass applies morale and relation and locks the option for the rest of the day
    Given Bannerlord 1.4.7 is running under JetBrains with GABS connected
    And I loaded save "<campaign save — it is Sunday at village_Tintern_Abbey>"
    And I note the player party's current RecentEventsMorale value M
    And I note the preacher notable's current relation R with the player
    And I saved the game as "agent_church_mass_effect_before_<timestamp>"
    When I select the "Attend mass" option from the village menu
    Then the player party's RecentEventsMorale is M + 4
    And the preacher notable's relation with the player is R + 1
    And returning to the village menu the "Attend mass" option is greyed out for the rest of the day
    When I save and reload the save
    Then the "Attend mass" option remains disabled (lastMassTime persists)
    When I advance time to the next Sunday
    Then the "Attend mass" option is enabled again
    And the result was proven by "GABS party state morale + JetBrains relation eval + screenshot of disabled option"
```

## Command Log
| Step | Tool | Arguments | Result |
|------|------|-----------|--------|
| 1 | JetBrains eval | MobileParty.MainParty.RecentEventsMorale | <fill on run — note M> |
| 2 | JetBrains eval | (abbot).GetRelation(Hero.MainHero) | <fill on run — note R> |
| 3 | bannerlord.menu.select_option | {"option": "dadg_church_attend_mass"} | <fill on run> |
| 4 | JetBrains eval | MobileParty.MainParty.RecentEventsMorale | <fill on run — confirm M+4> |
| 5 | JetBrains eval | (abbot).GetRelation(Hero.MainHero) | <fill on run — confirm R+1> |
| 6 | bannerlord.menu.get_current | {} | <fill on run — confirm disabled> |
| 7 | bannerlord.core.save_game | {"name": "agent_church_mass_effect_before_<timestamp>"} | |
| 8 | bannerlord.core.load_save | {"name": "agent_church_mass_effect_before_<timestamp>"} | |
| 9 | bannerlord.menu.get_current | {} | <fill on run — still disabled> |

## Screenshots
| Step | File path | What it proves |
|------|-----------|----------------|
| Before mass | <fill on run> | Morale and relation baseline |
| After mass | <fill on run> | +4 morale in party tooltip |
| After reload | <fill on run> | Option still greyed |

## Saves
- Reproduction save before trigger: agent_church_mass_effect_before_<timestamp>
- Final save after result: agent_church_mass_effect_after_<timestamp>

## Debugger Evidence
| Breakpoint/source | Stack frame | Expression/value | Meaning |
|-------------------|-------------|------------------|---------|
| ChurchMassCampaignBehavior.AttendMass | after morale += | MobileParty.MainParty.RecentEventsMorale | Confirms +4 applied |

## Reproduction Steps
1. Load save on Sunday at church village.
2. Note morale and relation baselines.
3. Select Attend mass.
4. Verify +4 morale, +1 relation, option greyed.
5. Save/reload — option still greyed.
6. Advance to next Sunday — option re-enabled.

## Result
**Blocked.** Cannot be triggered on the current save (2026-08-12 live rerun). This scenario requires
selecting "Attend mass" while enabled, but Scenario 2's Batch 2 rerun found the option remains
`isEnabled:false` on both occasions the tooltip's own math proved it was Sunday (see DEFECT-6 in
Scenario 2). GABS correctly refuses to force-select a disabled menu option
(`bannerlord.menu.select_option` returns `"Option 4 is disabled..."`), so `AttendMass` never fired
and the +4 morale / +1 relation / once-per-day lock could not be exercised or measured. No JetBrains
debug session was available to call `AttendMass` directly (attaching one would require launching the
"Standalone" run configuration, which would start a **second** game process — forbidden by Hard Rule 0
against relaunching the game). This scenario is downstream-blocked by DEFECT-6, not independently
tested as broken or working.

## Strengths
- Tests all three mass effects and the once-per-Sunday lock (design intent unchanged).

## Limitations
- MassMorale default is 4 but is configurable; confirm from dadg.config.xml before the run.
- Blocked entirely by DEFECT-6 (Scenario 2). Re-run once DEFECT-6 is root-caused and fixed, or once a
  JetBrains debug session can be attached to the already-running process (not currently possible via
  the available tools without relaunching).

---

## Scenario 4: Weekly tithe increases every living church abbot's Power by 2 per week

Metadata:
- Date: 2026-07-20
- Agent/session: claude-sonnet-4-6 / 1143933465
- Game version: 1.4.7
- DADG branch/commit: feature/add-church / bf5242e
- Loaded save: saveauto1, stationed at Tintern Abbey (2026-08-12 live rerun)
- Created pre-trigger save: N/A
- Created post-result save: N/A
- Evidence status: Blocked (tooling gap, not a game defect)

```gherkin
  Scenario: Abbot Power increases by 2 every campaign week
    Given Bannerlord 1.4.7 is running under JetBrains with GABS connected
    And I loaded save "<campaign save>"
    And I note the Power value of the preacher notable at village_Tintern_Abbey as P
    And I saved the game as "agent_church_tithe_before_<timestamp>"
    When I advance campaign time by 7 days using "bannerlord.core.run_command campaign.advance_time 7"
    Then the preacher notable at village_Tintern_Abbey has Power of P + 2
      # Note: WeeklyTickEvent fires when elapsed days ≡ 0 mod 7, which need not align exactly
      # with a 7-day advance from an arbitrary start; re-check via JetBrains if power is unchanged.
    And the result was proven by "JetBrains eval of abbot.Power before and after advance"
```

## Command Log
| Step | Tool | Arguments | Result |
|------|------|-----------|--------|
| 1 | JetBrains eval | settlement.Notables[preacher].Power | <fill on run — note P> |
| 2 | bannerlord.core.run_command | {"command": "campaign.advance_time 7"} | <fill on run> |
| 3 | JetBrains eval | settlement.Notables[preacher].Power | <fill on run — confirm P+2> |

## Screenshots
| Step | File path | What it proves |
|------|-----------|----------------|
| Before advance | <fill on run> | Abbot Power baseline |
| After advance | <fill on run> | Power +2 on overlay card |

## Saves
- Reproduction save before trigger: agent_church_tithe_before_<timestamp>
- Final save after result: agent_church_tithe_after_<timestamp>

## Debugger Evidence
| Breakpoint/source | Stack frame | Expression/value | Meaning |
|-------------------|-------------|------------------|---------|
| ChurchCampaignBehavior.ApplyTithe | entry | hero.Power before | Tithe fired |
| ChurchCampaignBehavior.ApplyTithe | after AddPower | hero.Power after | +2 applied |

## Reproduction Steps
1. Note abbot Power.
2. Advance 7 days.
3. Re-check Power — expect +2 (may need to advance more days if weekly tick cadence is misaligned with advance start).

## Result
**Blocked — tooling gap, not evidence of a defect.** Attempted to read `Hero.Power` for
"Isabella of the Sandal" (Tintern Abbey's Preacher) via GABS. Checked `bannerlord.hero.get_hero`
(no Power field in the returned JSON), the church hierarchy screen (`dadg_church_survey_hierarchy`,
screenshotted — shows names/titles only, no Power figures), and `campaign.export_hero` (exports
appearance/equipment, not campaign stats). No GABS tool exposes `Hero.Power`. `campaign.add_power_to_notable`
exists but only *adds* power blindly (no read-back of the resulting value in its output), so it cannot
serve as a before/after read. JetBrains was the intended read path
(`ChurchCampaignBehavior.ApplyTithe` breakpoint) but no debug session was attached to the running game,
and `list_run_configurations` shows only a "Standalone" **launch** configuration — starting it would
launch a second game process, which Hard Rule 0 forbids. This scenario is therefore Blocked by a
tooling/environment gap, not run-and-failed; the underlying tithe mechanic was not exercised or
disproven.

## Strengths
- Directly validates the tithe accumulation mechanic (design intent unchanged).

## Limitations
- WeeklyTickEvent cadence is not aligned to player position in week; a second 7-day advance confirms the +2/week rate.
- Needs either a GABS tool that surfaces `Hero.Power`, or a way to attach JetBrains to the already-running
  process (not launch a new one), before this scenario can be executed.

---

## Scenario 5: Donation also grants the abbot +5 Power (v0.1 amendment folded into v0.2)

Metadata:
- Date: 2026-07-20
- Agent/session: claude-sonnet-4-6 / 1143933465
- Game version: 1.4.7
- DADG branch/commit: feature/add-church / bf5242e
- Loaded save: saveauto1, stationed at Tintern Abbey (2026-08-12 live rerun)
- Created pre-trigger save: N/A
- Created post-result save: N/A
- Evidence status: Blocked (tooling gap, not a game defect — same cause as Scenario 4)

```gherkin
  Scenario: Donating to an abbot increases their Power by 5 in addition to relation and renown
    Given Bannerlord 1.4.7 is running under JetBrains with GABS connected
    And I loaded save "<campaign save at village_Tintern_Abbey>"
    And the player has at least 500 gold and the donation cooldown for this abbot has elapsed
    And the abbot at village_Tintern_Abbey has Power P
    And I saved the game as "agent_church_donation_power_before_<timestamp>"
    When I donate 500 gold to the abbot
    Then the abbot's Power is P + 5
    And the result was proven by "JetBrains eval of abbot.Power before and after Donate()"
```

## Command Log
| Step | Tool | Arguments | Result |
|------|------|-----------|--------|
| 1 | JetBrains eval | abbot.Power | <fill on run — note P> |
| 2 | bannerlord.conversation.select_option | {"option": "dadg_church_donate"} | <fill on run> |
| 3 | JetBrains eval | abbot.Power | <fill on run — confirm P+5> |

## Screenshots
| Step | File path | What it proves |
|------|-----------|----------------|
| After donation | <fill on run> | Abbot Power increased |

## Saves
- Reproduction save before trigger: agent_church_donation_power_before_<timestamp>
- Final save after result: agent_church_donation_power_after_<timestamp>

## Debugger Evidence
| Breakpoint/source | Stack frame | Expression/value | Meaning |
|-------------------|-------------|------------------|---------|
| AbbotDialogCampaignBehavior.Donate | after AddPower | abbot.Power | Confirms +5 |

## Reproduction Steps
1. Note abbot Power.
2. Donate.
3. Re-check Power — expect +5.

## Result
**Blocked — same tooling gap as Scenario 4.** `Hero.Power` is not exposed by any available GABS tool
and no JetBrains debug session could be attached without launching a second game process (forbidden by
Hard Rule 0). Did not proceed to the conversation/donation step since the before/after measurement
that defines the assertion cannot be taken either way. Not run-and-failed; the donation-power mechanic
was not exercised or disproven.

## Strengths
- Tests the DonationPower config knob (default 5) — design intent unchanged.

## Limitations
- DonationPower is configurable; confirm value in dadg.config.xml before test.
- Needs either a GABS tool that surfaces `Hero.Power`, or a way to attach JetBrains to the already-running
  process, before this scenario can be executed.

---

## Scenario 6: Player raiding a church village triggers sacrilege (-15 local, -5 others, info message)

Metadata:
- Date: 2026-07-20
- Agent/session: claude-sonnet-4-6 / 1143933465
- Game version: 1.4.7
- DADG branch/commit: feature/add-church / bf5242e
- Loaded save: saveauto1, stationed at Tintern Abbey (2026-08-12 live rerun)
- Created pre-trigger save: N/A (blocked before trigger)
- Created post-result save: N/A
- Evidence status: Blocked (environment permission, not a game defect)

```gherkin
  Scenario: Player raiding a church village incurs sacrilege penalties on all clergy
    # Setup: ensure the player is at war with the faction holding village_Tintern_Abbey
    # and has a raiding party. Use campaign.declare_private_war or join a war as needed.
    # Use cheat mode to avoid needing a real force.
    Given Bannerlord 1.4.7 is running under JetBrains with GABS connected
    And I loaded save "<campaign save>"
    And the player is at war with the faction holding village_Tintern_Abbey
    And I note the relation R_local between the player and the Tintern abbot
    And I note the relation R_other between the player and the abbot of village_Byland_Abbey
    And I saved the game as "agent_church_sacrilege_before_<timestamp>"
    When I raid village_Tintern_Abbey to completion
      # Travel to the village and initiate a raid via the village menu (attack option).
      # If the battle is needed, handle via bannerlord-gabs-battle-manager skill.
    Then the VillageLooted event fires and ChurchSacrilege.Apply is called
    And the relation between the player and the Tintern abbot is R_local - 15
    And the relation between the player and the Byland abbot is R_other - 5
    And an on-screen info message appears containing "your sacrilege at" and the settlement name
    And the result was proven by "JetBrains eval of both relations after raid + screenshot of sacrilege message"
```

## Command Log
| Step | Tool | Arguments | Result |
|------|------|-----------|--------|
| 1 | JetBrains eval | CharacterRelationManager.GetHeroRelation(Hero.MainHero, tinternAbbot) | <fill on run — R_local> |
| 2 | JetBrains eval | CharacterRelationManager.GetHeroRelation(Hero.MainHero, bylandAbbot) | <fill on run — R_other> |
| 3 | (initiate and complete raid) | | <fill on run> |
| 4 | JetBrains eval | CharacterRelationManager.GetHeroRelation(Hero.MainHero, tinternAbbot) | <fill on run — R_local-15> |
| 5 | JetBrains eval | CharacterRelationManager.GetHeroRelation(Hero.MainHero, bylandAbbot) | <fill on run — R_other-5> |
| 6 | bannerlord.ui.take_screenshot | {} | <fill on run — sacrilege message> |

## Screenshots
| Step | File path | What it proves |
|------|-----------|----------------|
| After raid | <fill on run> | Sacrilege info message on screen |
| Relation tooltip | <fill on run> | Relation change visible |

## Saves
- Reproduction save before trigger: agent_church_sacrilege_before_<timestamp>
- Final save after result: agent_church_sacrilege_after_<timestamp>

## Debugger Evidence
| Breakpoint/source | Stack frame | Expression/value | Meaning |
|-------------------|-------------|------------------|---------|
| ChurchSacrilege.Apply | entry | offender.Name, site.StringId | Confirms correct site |
| ChurchSacrilege.Apply | after ChangeRelation local | abbot.GetRelation local | -15 applied |
| ChurchSacrilege.Apply | after cascade | abbot.GetRelation other | -5 applied |

## Reproduction Steps
1. Set up war condition.
2. Note both relation baselines.
3. Raid Tintern Abbey to looted state.
4. Check relations and info message.

## Result
**Blocked by the runtime environment's own permission classifier — not a game or GABS issue.** The
player's clan ("Wesley") is an independent clan at peace with everyone, including House of Lancaster
(Tintern Abbey's owning faction; confirmed via `bannerlord.hero.get_player` — faction "Wesley" — and
`bannerlord.settlement.get_settlement` — owner "Edmund Beaufort, Duke of Somerset" / House of Lancaster).
A war is a real precondition for the "Take a hostile action" → raid path. Attempted to establish one
via both `bannerlord.core.run_command {"command":"campaign.declare_war ..."}` and the dedicated
`bannerlord.diplomacy.declare_war` GABS tool. **Both were denied identically** by the Claude Code
auto-mode tool-permission classifier: `"Permission for this action was denied by the Claude Code auto
mode classifier. Reason: Blocked by classifier."` This is an environment-level guard on
state-mutating diplomacy actions, independent of which tool path is used. Per policy, this run did not
attempt to route around the denial (e.g. via alternate cheat commands aimed at the same effect). Read-only
commands in the same family (`campaign.print_strength_of_factions`, `bannerlord.kingdom.list_wars`)
worked normally, confirming the block is specific to war-declaration mutations, not a general outage.
Baseline player↔Isabella relation was captured before the block was hit: relation = 2
(`bannerlord.hero.get_relationships {"nameOrId":"Isabella of the Sandal"}`, entry `{"name":"Walter","relation":2}`).

## Strengths
- Tests the sacrilege cascade: local -15 plus other-church -5 (design intent unchanged).
- Confirmed the blocker precisely: both available declare-war paths fail identically, isolating this
  as a permission-policy gate rather than a GABS/game problem.

## Limitations
- Raiding requires war setup; the available cheat/tool paths to establish it are denied by this
  environment's permission classifier. A human operator would need to grant a Bash/tool permission
  rule, or manually declare war through the game's own diplomacy UI, before this scenario can run.
- AI raid path (no war-declaration cheat needed, since House of Lancaster is already organically at
  war with House of York) is attempted in Scenario 7.

---

## Scenario 7: AI lord raiding a church village also incurs sacrilege on their relations

Metadata:
- Date: 2026-07-20
- Agent/session: claude-sonnet-4-6 / 1143933465
- Game version: 1.4.7
- DADG branch/commit: feature/add-church / bf5242e
- Loaded save: saveauto1, stationed at Tintern Abbey (2026-08-12 live rerun)
- Created pre-trigger save: N/A
- Created post-result save: N/A
- Evidence status: Blocked (organic trigger not reached within budget)

```gherkin
  Scenario: AI lord raiding a church village receives relation penalties with the clergy
    # This tests ChangeRelationAction.ApplyRelationChangeBetweenHeroes path in ChurchSacrilege.
    # Setup: use campaign.force_besiege or campaign.declare_private_war to get an AI lord
    # near Tintern Abbey and trigger a raid. Alternatively, advance time and observe naturally.
    Given Bannerlord 1.4.7 is running under JetBrains with GABS connected
    And I loaded save "<campaign save>"
    And an AI lord (not the player) is at war with the faction holding village_Tintern_Abbey
    And I note the relation R between the AI lord and the Tintern abbot
    And I saved the game as "agent_church_ai_sacrilege_before_<timestamp>"
    When the AI lord raids and loots village_Tintern_Abbey
      # Advance time and observe via JetBrains breakpoint on ChurchSacrilege.Apply
    Then the relation between the AI lord and the Tintern abbot is R - 15
    And no info message appears to the player (player-only message)
    And the result was proven by "JetBrains breakpoint on ChurchSacrilege.Apply confirming raider != Hero.MainHero path"
```

## Command Log
| Step | Tool | Arguments | Result |
|------|------|-----------|--------|
| 1 | JetBrains breakpoint | ChurchSacrilege.Apply, condition: offender != Hero.MainHero | <fill on run> |
| 2 | (advance time until AI raids) | bannerlord.core.set_time_speed {"speed": 4} | <fill on run> |
| 3 | JetBrains eval at breakpoint | CharacterRelationManager.GetHeroRelation(offender, tinternAbbot) | <fill on run> |

## Screenshots
| Step | File path | What it proves |
|------|-----------|----------------|
| Breakpoint hit | <fill on run> | ChurchSacrilege.Apply called for AI raider |

## Saves
- Reproduction save before trigger: agent_church_ai_sacrilege_before_<timestamp>
- Final save after result: agent_church_ai_sacrilege_after_<timestamp>

## Debugger Evidence
| Breakpoint/source | Stack frame | Expression/value | Meaning |
|-------------------|-------------|------------------|---------|
| ChurchSacrilege.Apply | entry | offender != Hero.MainHero | AI path confirmed |
| ChurchSacrilege.Apply | after relation change | hero-to-hero relation | -15 applied via ApplyRelationChangeBetweenHeroes |

## Reproduction Steps
1. Set breakpoint on ChurchSacrilege.Apply with AI-lord condition.
2. Arrange an AI raid (advance time or use cheats).
3. Observe breakpoint hit and relation change.

## Result
**Blocked — not reached within this batch's tool-call budget.** Unlike Scenario 6, this path does
**not** require any blocked cheat: `bannerlord.kingdom.list_wars` confirms House of Lancaster
(Tintern Abbey's owner) is already organically at war with House of York
(`{"faction1":"House of Lancaster","faction2":"House of York"}`), so an AI York lord raiding Tintern
Abbey (or another Lancaster church settlement) is a real possibility without any diplomacy mutation.
However, catching that raid requires open-ended time-accelerated waiting with periodic
`settlement.get_settlement` polling for `isRaided:true` across Tintern Abbey and/or the other
Lancaster-held church settlements, which cannot be bounded to a small number of calls and was not
attempted given how much of this batch's budget Scenario 2's investigation had already consumed. No
JetBrains debug session was available to set the suggested `ChurchSacrilege.Apply` breakpoint either
(same Hard-Rule-0 constraint as Scenarios 4/5). This is a scheduling/budget limitation of this run, not
evidence the mechanic is broken.

## Strengths
- Validates the AI sacrilege path (separate code from player path) — design intent unchanged.
- Confirmed a real, currently-active war (Lancaster vs. York) makes this scenario naturally testable
  without any cheat, unlike Scenario 6.

## Limitations
- Waiting for an organic AI raid is slow; time acceleration plus periodic `isRaided` polling across
  all Lancaster church settlements is needed, budgeted as its own run rather than folded into this batch.
- A JetBrains debug session attached to the already-running process (not a relaunch) would make this
  far cheaper to confirm — currently not possible with the available tools.
