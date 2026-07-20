# Feature: Church v0.4 — Bishops and the Church's Favour

Tags: @bannerlord @gabs @dadg @church @v0.4 @bishops @favour

---

## Scenario 1: Cathedral clergy are addressed as "Bishop", not "Abbot" or "Prior"

Metadata:
- Date: 2026-07-20
- Agent/session: claude-sonnet-4-6 / 1029274746
- Game version: 1.4.7
- DADG branch/commit: feature/add-church / bf5242e
- Loaded save: saveauto1 → teleported to Ely Cathedral then Tintern Abbey
- Created pre-trigger save: N/A
- Created post-result save: N/A
- Evidence status: Passed

```gherkin
Feature: Church v0.4 — Bishops and the Church's Favour

  Scenario: Cathedral clergy notable is titled "Bishop" in dialog, abbey clergy is "Abbot"
    Given Bannerlord 1.4.7 is running under JetBrains with GABS connected
    And I loaded save "<campaign save>"
    And the baseline state was verified by "bannerlord.core.get_game_state returning InCampaign"
    When I enter conversation with the preacher notable at village_Ely_Cathedral
    And I select the donate option (if donation cooldown allows)
    Then the thanks reply text contains the word "Bishop"
    And the donate option text reads "donation to the cathedral"
    When I enter conversation with the preacher notable at village_Tintern_Abbey
    And I select the donate option (if donation cooldown allows)
    Then the thanks reply text contains the word "Abbot"
    And the donate option text reads "donation to the abbey"
    And the result was proven by "screenshot of donate thanks text at both settlement types"
```

## Command Log
| Step | Tool | Arguments | Result |
|------|------|-----------|--------|
| 1 | bannerlord.party.enter_settlement | {"settlementNameOrId": "village_Ely_Cathedral"} | Entered Ely Cathedral |
| 2 | bannerlord.conversation.start | {"nameOrId": "Henry of the Cavern"} | Started conversation with Bishop |
| 3 | bannerlord.conversation.get_state | {} | Hub text: "God keep you, my lord. What brings you to Ely Cathedral?" — 4 options incl. dadg_church_favour |
| 4 | bannerlord.conversation.select_option | {"index": 0} (dadg_church_donate) | Selected donation |
| 5 | bannerlord.conversation.get_state | {} | Reply: "God reward you, my lord. This Bishop will remember your generosity." |
| 6 | bannerlord.ui.take_screenshot | {} | screenshot_20260720_144959.jpg |
| 7 | bannerlord.party.enter_settlement | {"settlementNameOrId": "village_Tintern_Abbey"} | Entered Tintern Abbey |
| 8 | bannerlord.conversation.start | {"nameOrId": "Margaret of the Pasture"} | Started conversation with Abbot |
| 9 | bannerlord.conversation.get_state | {} | Hub: 3 options, NO dadg_church_favour option; donate text "donation to the abbey. (500 denars)" |
| 10 | bannerlord.conversation.select_option | {"index": 0} (dadg_church_donate) | Selected donation |
| 11 | bannerlord.conversation.get_state | {} | Reply: "God reward you, my lord. This Abbot will remember your generosity." |
| 12 | bannerlord.ui.take_screenshot | {} | screenshot_20260720_145126.jpg |

## Screenshots
| Step | File path | What it proves |
|------|-----------|----------------|
| Ely Cathedral donation reply | screenshot_20260720_144959.jpg | "This Bishop will remember your generosity." — Bishop title confirmed |
| Tintern Abbey donation reply | screenshot_20260720_145126.jpg | "This Abbot will remember your generosity." — Abbot title confirmed |

## Saves
- Reproduction save before trigger: saveauto1
- Final save after result: N/A

## Debugger Evidence
| Breakpoint/source | Stack frame | Expression/value | Meaning |
|-------------------|-------------|------------------|---------|
| bannerlord.conversation.get_state | text field | "This Bishop will remember your generosity." | Cathedral → Bishop confirmed |
| bannerlord.conversation.get_state | text field | "This Abbot will remember your generosity." | Abbey → Abbot confirmed |

## Reproduction Steps
1. Load saveauto1.
2. `bannerlord.party.enter_settlement {"settlementNameOrId": "village_Ely_Cathedral"}`.
3. `bannerlord.conversation.start {"nameOrId": "Henry of the Cavern"}`.
4. Select donate (index 0) — reply must say "This Bishop".
5. Go to Tintern Abbey, start conversation with Margaret of the Pasture.
6. Select donate — reply must say "This Abbot".

## Result
PASSED. The donation reply at Ely Cathedral (Cathedral) says "This Bishop will remember your generosity." and at Tintern Abbey (Abbey) says "This Abbot will remember your generosity." The clergy title is correctly differentiated by settlement type. Also confirmed: the donate option text says "donation to the cathedral" at Ely and "donation to the abbey" at Tintern.

Note: Female Preacher "Margaret of the Pasture" is still called "Abbot" not "Abbess" — gender-insensitive title defect (already recorded in v0.1 S1).

## Strengths
- Direct get_state confirmation of reply text at both settlement types.
- Both donation amounts are 500 denars (no tiered pricing).

## Limitations
- Donation cooldown was cleared because freshly loaded save — title in reply would be blocked if cooldown is active.

---

## Scenario 2: "How does the Church regard me?" question appears only at cathedrals

Metadata:
- Date: 2026-07-20
- Agent/session: claude-sonnet-4-6 / 1029274746
- Game version: 1.4.7
- DADG branch/commit: feature/add-church / bf5242e
- Loaded save: saveauto1 → Ely Cathedral then Tintern Abbey
- Created pre-trigger save: N/A
- Created post-result save: N/A
- Evidence status: Passed

```gherkin
  Scenario: Favour inquiry option is present at cathedral and absent at abbey/priory
    Given Bannerlord 1.4.7 is running under JetBrains with GABS connected
    And I loaded save "<campaign save>"
    When I enter conversation with the preacher notable at village_Ely_Cathedral
    Then the player option "How does the Church regard me, Your Grace?" is present in the hub
    When I enter conversation with the preacher notable at village_Tintern_Abbey
    Then the player option "How does the Church regard me, Your Grace?" is absent
    And the result was proven by "bannerlord.conversation.get_state option lists at both settlements + screenshots"
```

## Command Log
| Step | Tool | Arguments | Result |
|------|------|-----------|--------|
| 1 | bannerlord.conversation.start | {"nameOrId": "Henry of the Cavern"} at Ely Cathedral | Hub: 4 options — dadg_church_donate, dadg_church_blessing, dadg_church_favour, dadg_church_leave |
| 2 | bannerlord.conversation.get_state | {} | dadg_church_favour present: "How does the Church regard me, Your Grace?" |
| 3 | bannerlord.conversation.start | {"nameOrId": "Margaret of the Pasture"} at Tintern Abbey | Hub: 3 options — dadg_church_donate, dadg_church_blessing, dadg_church_leave |
| 4 | bannerlord.conversation.get_state | {} | dadg_church_favour ABSENT at Tintern Abbey |

## Screenshots
| Step | File path | What it proves |
|------|-----------|----------------|
| Cathedral hub | screenshot_20260720_144910.jpg | Favour option "How does the Church regard me, Your Grace?" visible as index 2 |
| Abbey hub | (no dedicated screenshot) | Confirmed via get_state: 3 options, no dadg_church_favour |

## Saves
- Reproduction save before trigger: saveauto1
- Final save after result: N/A

## Debugger Evidence
| Breakpoint/source | Stack frame | Expression/value | Meaning |
|-------------------|-------------|------------------|---------|
| bannerlord.conversation.get_state | options list | 4 options at Cathedral, 3 at Abbey | Favour option gated correctly |

## Reproduction Steps
1. Load saveauto1. Go to Ely Cathedral, start conversation with bishop.
2. Confirm dadg_church_favour at index 2.
3. Go to Tintern Abbey, start conversation with abbot.
4. Confirm dadg_church_favour absent (3 options only).

## Result
PASSED. "How does the Church regard me, Your Grace?" (dadg_church_favour) appears at Ely Cathedral (Cathedral) and is absent at Tintern Abbey (Abbey). The IsCathedral gate works correctly.

## Strengths
- Both positive and negative cases confirmed via get_state option enumeration.

## Limitations
- Only one cathedral and one abbey tested. Priory not tested (expected same as abbey).

---

## Scenario 3: Favour reply matches the ChurchFavourRank at each threshold

Metadata:
- Date: 2026-07-20
- Agent/session: claude-sonnet-4-6 / 1029274746
- Game version: 1.4.7
- DADG branch/commit: feature/add-church / bf5242e
- Loaded save: saveauto1 (Summer 2, 1084 — relations near 0)
- Created pre-trigger save: N/A
- Created post-result save: N/A
- Evidence status: Partial

```gherkin
  Scenario: Favour inquiry reply matches the computed rank at multiple thresholds
    # Rank thresholds (ChurchFavourPolicy):
    #   Reviled: avg <= -10 | IllRegarded: (-10, -2] | Indifferent: (-2, +2)
    #   Favoured: [+2, +10) | Beloved: >= +10
    # Manipulate via donations (+2 relation each) and sacrilege (-15/-5 cascade).
    # JetBrains eval: iterate all church settlements, sum CharacterRelationManager relations,
    # divide by count to confirm average.
    Given Bannerlord 1.4.7 is running under JetBrains with GABS connected
    And I loaded save "<fresh campaign save>"
    And the average player relation with all 16 clergy is ~0 (Indifferent rank)
    And I saved the game as "agent_church_favour_before_<timestamp>"
    When I ask "How does the Church regard me, Your Grace?" at village_Ely_Cathedral
    Then the bishop replies with the Indifferent line:
      "The Church knows little of you, my lord/lady. Works, not words, commend a soul."
    # Reach Favoured (+2 average): donate to ~15 clergy or attend mass multiple Sundays.
    # Fastest cheat: manipulate relations via JetBrains eval on each abbot.
    When I raise the average relation to >= +2 (Favoured threshold)
      # JetBrains: foreach abbot in all church settlements, CharacterRelationManager.SetHeroRelation(Hero.MainHero, abbot, 2)
    And I ask the favour question again
    Then the bishop replies with the Favoured line:
      "The Church counts you among her faithful sons/daughters."
    When I raise the average relation to >= +10 (Beloved threshold)
    And I ask the favour question again
    Then the bishop replies with the Beloved line:
      "All England's cloisters speak your name with love. You are a true friend of Holy Church."
    When I set the average relation to <= -10 (Reviled threshold)
    And I ask the favour question again
    Then the bishop replies with the Reviled line:
      "You stand in the shadow of anathema. Repent, before God and His Church."
    And the result was proven by "screenshots of each reply line + JetBrains average-relation eval"
```

## Command Log
| Step | Tool | Arguments | Result |
|------|------|-----------|--------|
| 1 | bannerlord.conversation.start | {"nameOrId": "Henry of the Cavern"} at Ely Cathedral | Hub open |
| 2 | bannerlord.conversation.select_option | {"index": 2} (dadg_church_favour) | "How does the Church regard me, Your Grace?" selected |
| 3 | bannerlord.conversation.get_state | {} | Reply: "The Church knows little of you, my lord. Works, not words, commend a soul." |
| 4 | (Favoured, Beloved, Reviled ranks) | NOT TESTED — JetBrains relation manipulation not completed | |

## Screenshots
| Step | File path | What it proves |
|------|-----------|----------------|
| Indifferent reply | screenshot_20260720_144910.jpg | Conversation hub at Ely Cathedral (taken before favour inquiry) |
| (Favoured/Beloved/Reviled) | Not taken | Other rank thresholds not tested |

## Saves
- Reproduction save before trigger: saveauto1
- Final save after result: N/A

## Debugger Evidence
| Breakpoint/source | Stack frame | Expression/value | Meaning |
|-------------------|-------------|------------------|---------|
| bannerlord.conversation.get_state | text field | "The Church knows little of you, my lord. Works, not words, commend a soul." | Indifferent rank text confirmed |

## Reproduction Steps
1. Load saveauto1. Go to Ely Cathedral, start conversation with Henry of the Cavern.
2. Select favour inquiry (index 2) — confirm Indifferent reply text.
3. Use JetBrains to set relations to +2 on all clergy, re-ask — expect Favoured reply.
4. Repeat for +10 (Beloved) and -10 (Reviled).

## Result
PARTIAL. Indifferent rank reply confirmed: "The Church knows little of you, my lord. Works, not words, commend a soul." The other 4 rank thresholds (IllRegarded, Favoured, Beloved, Reviled) were not tested — JetBrains relation manipulation was not executed in this run.

## Strengths
- Indifferent rank text confirmed live in a fresh campaign.

## Limitations
- Only the Indifferent rank tested. The 4 other rank thresholds require JetBrains relation manipulation or significant gameplay time.

---

## Scenario 4: Bishop blessing grants +5 morale and +1 renown when rank >= Favoured

Metadata:
- Date: 2026-07-20
- Agent/session: claude-sonnet-4-6 / 1029274746
- Game version: 1.4.7
- DADG branch/commit: feature/add-church / bf5242e
- Loaded save: <campaign save — player at Favoured rank>
- Created pre-trigger save: agent_church_blessing_before_<timestamp>
- Created post-result save: agent_church_blessing_after_<timestamp>
- Evidence status: Not run

```gherkin
  Scenario: Bishop grants blessing to Favoured player with +5 morale, +1 renown, 7-day cooldown
    Given Bannerlord 1.4.7 is running under JetBrains with GABS connected
    And I loaded save "<campaign save at village_Ely_Cathedral>"
    And the average relation with all clergy is >= +2 (Favoured rank)
    And no bishop blessing has been given in the last 7 days (_lastBishopBlessingTime == Never or > 7 days)
    And I note the player party's RecentEventsMorale as M and renown as V
    And I saved the game as "agent_church_blessing_before_<timestamp>"
    When I enter conversation with the bishop at village_Ely_Cathedral
    And I select "Grant me your blessing, Your Grace."
    Then the bishop replies: "Kneel, then. May God make you strong in battle and merciful in victory."
    And the player party's RecentEventsMorale is M + 5
    And the player's renown is V + 1
    And the blessing option is no longer visible (7-day cooldown)
    And the result was proven by "JetBrains eval of morale and renown + screenshot of reply + missing option"
```

## Command Log
| Step | Tool | Arguments | Result |
|------|------|-----------|--------|
| 1 | JetBrains eval | MobileParty.MainParty.RecentEventsMorale | <fill on run — M> |
| 2 | JetBrains eval | Hero.MainHero.Renown | <fill on run — V> |
| 3 | bannerlord.conversation.select_option | {"option": "dadg_church_bishop_blessing"} | <fill on run> |
| 4 | JetBrains eval | MobileParty.MainParty.RecentEventsMorale | <fill on run — M+5> |
| 5 | JetBrains eval | Hero.MainHero.Renown | <fill on run — V+1> |
| 6 | bannerlord.conversation.get_state | {} | <fill on run — blessing option absent> |

## Screenshots
| Step | File path | What it proves |
|------|-----------|----------------|
| Blessing reply | <fill on run> | Full blessing text |
| After blessing | <fill on run> | Option absent from hub |

## Saves
- Reproduction save before trigger: agent_church_blessing_before_<timestamp>
- Final save after result: agent_church_blessing_after_<timestamp>

## Debugger Evidence
| Breakpoint/source | Stack frame | Expression/value | Meaning |
|-------------------|-------------|------------------|---------|
| AbbotDialogCampaignBehavior.GrantBishopBlessing | after morale += | RecentEventsMorale | +5 confirmed |
| AbbotDialogCampaignBehavior.GrantBishopBlessing | _lastBishopBlessingTime | CampaignTime.Now | Cooldown set |

## Reproduction Steps
1. Set Favoured rank via JetBrains if needed.
2. Enter bishop conversation.
3. Select blessing option.
4. Check morale (+5), renown (+1), option absent.

## Result
<fill on run>

## Strengths
- Tests all three blessing effects and the cooldown gate in one scenario.

## Limitations
- BlessingMorale is configurable (default 5); confirm from dadg.config.xml before the run.

---

## Scenario 5: Blessing option is absent when rank < Favoured (IllRegarded / Indifferent)

Metadata:
- Date: 2026-07-20
- Agent/session: claude-sonnet-4-6 / 1029274746
- Game version: 1.4.7
- DADG branch/commit: feature/add-church / bf5242e
- Loaded save: <campaign save — player at Indifferent or IllRegarded rank>
- Created pre-trigger save: <pre-trigger save>
- Created post-result save: N/A
- Evidence status: Not run

```gherkin
  Scenario: Blessing option is absent when favour rank is below Favoured
    Given Bannerlord 1.4.7 is running under JetBrains with GABS connected
    And the average relation with all clergy is < +2 (Indifferent or IllRegarded)
    When I enter conversation with the bishop at village_Ely_Cathedral
    Then the option "Grant me your blessing, Your Grace." is not shown in the hub
    And the result was proven by "bannerlord.conversation.get_state showing absent blessing option + screenshot"
```

## Command Log
| Step | Tool | Arguments | Result |
|------|------|-----------|--------|
| 1 | (enter conversation with bishop) | | <fill on run> |
| 2 | bannerlord.conversation.get_state | {} | <fill on run — blessing absent> |

## Screenshots
| Step | File path | What it proves |
|------|-----------|----------------|
| Bishop hub | <fill on run> | Blessing option absent |

## Saves
- Reproduction save before trigger: <pre-trigger save>
- Final save after result: N/A

## Debugger Evidence
| Breakpoint/source | Stack frame | Expression/value | Meaning |
|-------------------|-------------|------------------|---------|
| AbbotDialogCampaignBehavior.CanRequestBishopBlessing | return false | BishopBlessingPolicy → NotFavoured | Gate confirmed |

## Reproduction Steps
1. Ensure rank below Favoured.
2. Open bishop conversation.
3. Confirm no blessing option.

## Result
<fill on run>

## Strengths
- Validates the rank gate in BishopBlessingPolicy.

## Limitations
- Simple negative test; mainly a regression guard.

---

## Scenario 6: Blessing cooldown persists across save/load and the option returns after 7 days

Metadata:
- Date: 2026-07-20
- Agent/session: claude-sonnet-4-6 / 1029274746
- Game version: 1.4.7
- DADG branch/commit: feature/add-church / bf5242e
- Loaded save: <campaign save — blessing just received>
- Created pre-trigger save: agent_church_blessing_cd_before_<timestamp>
- Created post-result save: agent_church_blessing_cd_after_<timestamp>
- Evidence status: Not run

```gherkin
  Scenario: Blessing cooldown survives save/load and resets after 7 days
    Given Bannerlord 1.4.7 is running under JetBrains with GABS connected
    And I just received a bishop's blessing in the current session
    And I saved the game as "agent_church_blessing_cd_before_<timestamp>"
    When I reload the save
    Then the blessing option is still absent at the bishop (cooldown persisted)
    When I advance campaign time by 7 days
    And I re-enter conversation with the bishop
    Then the blessing option is present again
    And the result was proven by "bannerlord.conversation.get_state before and after 7-day advance"
```

## Command Log
| Step | Tool | Arguments | Result |
|------|------|-----------|--------|
| 1 | bannerlord.core.save_game | {"name": "agent_church_blessing_cd_before_<timestamp>"} | <fill on run> |
| 2 | bannerlord.core.load_save | {"name": "agent_church_blessing_cd_before_<timestamp>"} | <fill on run> |
| 3 | bannerlord.conversation.get_state | {} | <fill on run — absent> |
| 4 | bannerlord.core.run_command | {"command": "campaign.advance_time 7"} | <fill on run> |
| 5 | bannerlord.conversation.get_state | {} | <fill on run — present> |

## Screenshots
| Step | File path | What it proves |
|------|-----------|----------------|
| After reload | <fill on run> | Option absent |
| After 7 days | <fill on run> | Option present |

## Saves
- Reproduction save before trigger: agent_church_blessing_cd_before_<timestamp>
- Final save after result: agent_church_blessing_cd_after_<timestamp>

## Debugger Evidence
| Breakpoint/source | Stack frame | Expression/value | Meaning |
|-------------------|-------------|------------------|---------|
| AbbotDialogCampaignBehavior.SyncData | _dadgChurchLastBishopBlessingTime | value after reload | Cooldown persisted |

## Reproduction Steps
1. Receive blessing.
2. Save and reload — option absent.
3. Advance 7 days — option present.

## Result
<fill on run>

## Strengths
- Tests _lastBishopBlessingTime SyncData persistence (key: "_dadgChurchLastBishopBlessingTime").

## Limitations
- cooldown is 7 days fixed (not configurable); doc and code agree.

---

## Scenario 7: Sacrilege (raid) drops favour and changes the bishop's reply

Metadata:
- Date: 2026-07-20
- Agent/session: claude-sonnet-4-6 / 1029274746
- Game version: 1.4.7
- DADG branch/commit: feature/add-church / bf5242e
- Loaded save: <campaign save at Favoured rank>
- Created pre-trigger save: agent_church_favour_drop_before_<timestamp>
- Created post-result save: agent_church_favour_drop_after_<timestamp>
- Evidence status: Not run

```gherkin
  Scenario: Raiding a church village drops favour rank and changes the bishop's reply
    Given Bannerlord 1.4.7 is running under JetBrains with GABS connected
    And the player is at Favoured rank (average relation >= +2)
    And I saved the game as "agent_church_favour_drop_before_<timestamp>"
    When I raid village_Tintern_Abbey to completion
      # -15 local relation, -5 to all 15 others = average drops by (15 + 5*15)/16 ≈ -5.6
    And I ask the favour question at village_Ely_Cathedral
    Then the bishop no longer replies with the Favoured line
    And the reply is IllRegarded or Reviled depending on the new average
    And the result was proven by "screenshot of changed favour reply + JetBrains average relation eval"
```

## Command Log
| Step | Tool | Arguments | Result |
|------|------|-----------|--------|
| 1 | JetBrains eval | average relation | <fill on run — >= +2> |
| 2 | (raid Tintern Abbey) | | <fill on run> |
| 3 | JetBrains eval | average relation after raid | <fill on run — dropped> |
| 4 | bannerlord.conversation.select_option | {"option": "dadg_church_favour"} | <fill on run> |
| 5 | bannerlord.ui.take_screenshot | {} | <fill on run — changed reply> |

## Screenshots
| Step | File path | What it proves |
|------|-----------|----------------|
| Before raid reply | <fill on run> | Favoured text |
| After raid reply | <fill on run> | IllRegarded/Reviled text |

## Saves
- Reproduction save before trigger: agent_church_favour_drop_before_<timestamp>
- Final save after result: agent_church_favour_drop_after_<timestamp>

## Debugger Evidence
| Breakpoint/source | Stack frame | Expression/value | Meaning |
|-------------------|-------------|------------------|---------|
| AbbotDialogCampaignBehavior.GetFavourRank | averageRelation | new value after raid | Rank recalculated live |

## Reproduction Steps
1. Confirm Favoured rank.
2. Raid a church village.
3. Ask favour question — expect degraded rank.

## Result
<fill on run>

## Strengths
- Tests that favour is computed on-demand from real relations, not cached state.

## Limitations
- War setup required for raid; use cheats for speed.
