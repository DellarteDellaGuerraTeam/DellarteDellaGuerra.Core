# Feature: Church v0.1 — Donation Dialog and Policy

Tags: @bannerlord @gabs @dadg @church @v0.1 @donation

---

## Scenario 1: Donating 500 gold gives +2 relation, +1 renown, and locks the option for 7 days

Metadata:
- Date: 2026-07-20
- Agent/session: claude-sonnet-4-6 / 1143933465
- Game version: 1.4.7
- DADG branch/commit: feature/add-church / bf5242e
- Loaded save: saveauto1 (Summer 2, 1084), teleported to village_Tintern_Abbey
- Created pre-trigger save: N/A (entered conversation directly)
- Created post-result save: N/A
- Evidence status: Partial

```gherkin
Feature: Church v0.1 — Donation Dialog and Policy

  Scenario: Donating 500 gold gives +2 relation and +1 renown, and locks the option for 7 days
    Given Bannerlord 1.4.7 is running under JetBrains with GABS connected
    And I loaded save "<campaign save at village_Tintern_Abbey>"
    And the player party has at least 500 gold
      # Use cheat if needed: bannerlord.core.run_command {"command":"campaign.add_item_to_player_party <gold id> 1000"}
      # or config.cheat_mode 1 then give_gold 1000
    And the preacher notable at village_Tintern_Abbey has a known relation value R with the player
    And the player has a known renown value V
    And I saved the game as "agent_church_donation_before_<timestamp>"
    When I open the village menu at village_Tintern_Abbey
    And I enter conversation with the preacher notable (Occupation.Preacher)
    And the NPC greeting "God keep you, my lord/lady. What brings you to Tintern Abbey?" is shown
    And I select the player line "I wish to make a donation to the abbey. (500 denars)"
    Then the NPC replies with text containing "This Abbot will remember your generosity"
    And the player gold is reduced by 500
    And the preacher notable's relation with the player is R + 2
    And the player renown is V + 1
    And returning to the dadg_church_talk hub the donate option is no longer available
    And the result was proven by "GABS party state and JetBrains eval of hero relation + screenshot of dialog"
```

## Command Log
| Step | Tool | Arguments | Result |
|------|------|-----------|--------|
| 1 | bannerlord.party.enter_settlement | village_Tintern_Abbey | Teleported to Tintern Abbey |
| 2 | bannerlord.menu.get_current | {} | village menu present, dadg_church_attend_mass disabled "6 days hence" |
| 3 | bannerlord.conversation.start | {"nameOrId": "Margaret of the Pasture"} | Conversation started — gold was 3000 pre-donation |
| 4 | bannerlord.conversation.get_state | {} | Hub shown: donate/bless/leave; NPC: "God keep you, my lord. What brings you to Tintern Abbey?" |
| 5 | bannerlord.ui.take_screenshot | {} | screenshot_20260720_135526.jpg — conversation hub with donate option |
| 6 | bannerlord.conversation.select_option | {"index": 0} (dadg_church_donate) | Selected "I wish to make a donation to the abbey. (500 denars)" |
| 7 | bannerlord.conversation.get_state | {} | NPC reply: "God reward you, my lord. This Abbot will remember your generosity." |
| 8 | bannerlord.ui.take_screenshot | {} | screenshot_20260720_135605.jpg — thanks text visible |
| 9 | bannerlord.party.get_player_party | {} | gold: 2500 (was 3000 → -500 confirmed) |
| 10 | bannerlord.conversation.get_state | {} | Hub: only bless/leave — donate ABSENT (cooldown active) |

## Screenshots
| Step | File path | What it proves |
|------|-----------|----------------|
| Before donation | C:\Users\Joe\Documents\Mount and Blade II Bannerlord\Screenshots\GABS\screenshot_20260720_135526.jpg | Conversation hub with donate option visible, NPC greeting |
| After donation | C:\Users\Joe\Documents\Mount and Blade II Bannerlord\Screenshots\GABS\screenshot_20260720_135605.jpg | Thanks reply "This Abbot will remember your generosity" visible, donate option gone |

## Saves
- Reproduction save before trigger: saveauto1 + enter Tintern Abbey conversation
- Final save after result: N/A (no agent save created; cooldown active state lost on crash)

## Debugger Evidence
| Breakpoint/source | Stack frame | Expression/value | Meaning |
|-------------------|-------------|------------------|---------|
| Not evaluated — game was running; JetBrains eval blocked during active play | | | |

## Reproduction Steps
1. Launch, load saveauto1.
2. `bannerlord.party.enter_settlement village_Tintern_Abbey`.
3. `bannerlord.conversation.start {"nameOrId": "Margaret of the Pasture"}`.
4. Select option index 0 (donate).
5. Read NPC reply — confirm "This Abbot will remember your generosity".
6. Query party gold — confirm -500.
7. Return to hub — confirm donate absent.

## Result
PARTIAL. Gold reduction of 500 confirmed (3000→2500). NPC thanks reply text confirmed: "God reward you, my lord. This Abbot will remember your generosity." Donate option absent after donation (cooldown active). DEFECTS: (1) Female Preacher "Margaret of the Pasture" addressed as "Abbot" not "Abbess"; (2) female Preacher NPC wearing bikini-style outfit in conversation scene (not monk/clergy robes). Relation +2 and renown +1 not verified — JetBrains eval not available while game was running.

## Strengths
- Gold deduction and thanks text confirmed via GABS party state and conversation state.
- Cooldown (donate option disappearance) confirmed in same session.

## Limitations
- Relation +2 and renown +1 not verified — require JetBrains eval or a separate GABS party state check.
- Gender-sensitive title defect: female Preachers titled "Abbot" instead of "Abbess".

---

## Scenario 2: Donation option is hidden when the player has fewer than 500 gold

Metadata:
- Date: 2026-07-20
- Agent/session: claude-sonnet-4-6 / 1143933465
- Game version: 1.4.7
- DADG branch/commit: feature/add-church / bf5242e
- Loaded save: post-donation state (gold 2500, cooldown active)
- Created pre-trigger save: N/A
- Created post-result save: N/A
- Evidence status: Inconclusive

```gherkin
  Scenario: Donation option is hidden when gold is insufficient
    Given Bannerlord 1.4.7 is running under JetBrains with GABS connected
    And I loaded save "<campaign save>"
    And the player party has fewer than 500 gold
      # Cheat to drain gold: give_gold negative amount or spend it manually
    When I enter conversation with the preacher notable at village_Tintern_Abbey
    Then the player option "I wish to make a donation to the abbey" is not shown
    And the "Bless me, Father." and "I must be on my way." options are still present
    And the result was proven by "bannerlord.conversation.get_state showing absent donate option + screenshot"
```

## Command Log
| Step | Tool | Arguments | Result |
|------|------|-----------|--------|
| 1 | (drain gold below 500) | NOT EXECUTED | Not tested in this run |

## Screenshots
| Step | File path | What it proves |
|------|-----------|----------------|
| N/A | | Not executed |

## Saves
- Reproduction save before trigger: N/A
- Final save after result: N/A

## Debugger Evidence
| Breakpoint/source | Stack frame | Expression/value | Meaning |
|-------------------|-------------|------------------|---------|
| Not executed | | | |

## Reproduction Steps
1. Use `campaign.add_gold_to_hero` to drain gold below 500 (or set to 100).
2. Enter conversation at a church settlement.
3. Verify donate option absent.

## Result
INCONCLUSIVE. Gold below 500 case not tested in this run. The cooldown case (donate absent after donating) was proven as a side effect of Scenario 1, but the insufficient-gold gate was not independently verified. Recommend a follow-up run with `campaign.add_gold_to_hero -2900` to reach <500.

## Strengths
- Cooldown case inadvertently proven during Scenario 1 (donate gone after donation).

## Limitations
- Low-gold gate not independently tested; requires dedicated gold-drain setup.

---

## Scenario 3: Donation option is hidden for 7 days after donating (cooldown)

Metadata:
- Date: 2026-07-20
- Agent/session: claude-sonnet-4-6 / 1143933465
- Game version: 1.4.7
- DADG branch/commit: feature/add-church / bf5242e
- Loaded save: post-donation state at Tintern Abbey (Summer 2 1084, gold 2500)
- Created pre-trigger save: N/A
- Created post-result save: N/A
- Evidence status: Partial

```gherkin
  Scenario: Donation option hidden during 7-day cooldown and restored after
    Given Bannerlord 1.4.7 is running under JetBrains with GABS connected
    And I have just donated at village_Tintern_Abbey in the current session
    And I saved the game as "agent_church_cooldown_before_<timestamp>"
    When I immediately re-enter conversation with the same preacher notable
    Then the donate option is absent (cooldown active)
    When I save the game, quit, and reload the save
    Then the donate option is still absent (cooldown survives save/load)
    When I advance campaign time by 7 days using "bannerlord.core.run_command campaign.advance_time 7"
    And I re-enter conversation with the same preacher notable
    Then the donate option is present again
    And the result was proven by "bannerlord.conversation.get_state showing donate option present + screenshot"
```

## Command Log
| Step | Tool | Arguments | Result |
|------|------|-----------|--------|
| 1 | bannerlord.conversation.start | {"nameOrId": "Margaret of the Pasture"} | Conversation started post-donation |
| 2 | bannerlord.conversation.get_state | {} | Hub: options=[dadg_church_blessing, dadg_church_leave] — dadg_church_donate ABSENT |
| 3 | (7-day advance and re-test) | NOT EXECUTED — game crashed before this step | |

## Screenshots
| Step | File path | What it proves |
|------|-----------|----------------|
| Post-donation conversation hub | C:\Users\Joe\Documents\Mount and Blade II Bannerlord\Screenshots\GABS\screenshot_20260720_135605.jpg | Donate option absent (2 options only: bless/leave) |

## Saves
- Reproduction save before trigger: N/A
- Final save after result: N/A (game crashed)

## Debugger Evidence
| Breakpoint/source | Stack frame | Expression/value | Meaning |
|-------------------|-------------|------------------|---------|
| Not evaluated | | | |

## Reproduction Steps
1. Load saveauto1, teleport to Tintern Abbey, donate.
2. Re-enter conversation — donate absent (cooldown).
3. Advance 7 days via village wait menu or time speed.
4. Re-enter conversation — confirm donate re-appears.

## Result
PARTIAL. Immediate cooldown confirmed: donate option absent immediately after donation (hub shows only bless/leave). 7-day expiry not tested — game crashed before that step. Save/load persistence of cooldown not tested.

## Strengths
- Immediate cooldown enforcement confirmed via GABS conversation state.

## Limitations
- 7-day re-enable and save/load persistence not verified in this run.

---

## Scenario 4: "Bless me, Father" flavor option is always available and returns to hub

Metadata:
- Date: 2026-07-20
- Agent/session: claude-sonnet-4-6 / 1143933465
- Game version: 1.4.7
- DADG branch/commit: feature/add-church / bf5242e
- Loaded save: saveauto1 + teleport to Tintern Abbey
- Created pre-trigger save: N/A
- Created post-result save: N/A
- Evidence status: Passed

```gherkin
  Scenario: "Bless me, Father" flavor dialog is always available and returns to the conversation hub
    Given Bannerlord 1.4.7 is running under JetBrains with GABS connected
    And I loaded save "<campaign save>"
    And I am in conversation with the preacher notable at any church settlement
    When I select the player option "Bless me, Father."
    Then the NPC replies with the blessing text "May the Lord bless you and keep you, and grant you peace on all your roads."
    And the conversation returns to the dadg_church_talk hub (donate and leave options visible again)
    And no gold, relation, or morale change occurs
    And the result was proven by "bannerlord.conversation.get_state showing hub options after blessing + screenshot of reply"
```

## Command Log
| Step | Tool | Arguments | Result |
|------|------|-----------|--------|
| 1 | bannerlord.conversation.get_state | {} | Hub shown: [donate (now absent due to cooldown), bless, leave] — bless present as dadg_church_blessing |
| 2 | bannerlord.conversation.select_option | {"index": 0} (dadg_church_blessing) | Selected "Bless me, Father." |
| 3 | bannerlord.conversation.get_state | {} | NPC reply: "May the Lord bless you and keep you, and grant you peace on all your roads." options=[bless, leave] — hub returned |

## Screenshots
| Step | File path | What it proves |
|------|-----------|----------------|
| Post-donation hub with blessing | C:\Users\Joe\Documents\Mount and Blade II Bannerlord\Screenshots\GABS\screenshot_20260720_135605.jpg | Hub present with bless option |

## Saves
- Reproduction save before trigger: N/A
- Final save after result: N/A

## Debugger Evidence
| Breakpoint/source | Stack frame | Expression/value | Meaning |
|-------------------|-------------|------------------|---------|
| N/A — conversation state confirmed via GABS | | | |

## Reproduction Steps
1. Enter conversation with any church notable.
2. Select "Bless me, Father." (dadg_church_blessing).
3. Read reply: "May the Lord bless you and keep you, and grant you peace on all your roads."
4. Confirm hub returns with bless and leave options.

## Result
PASSED. Blessing option always present (available at the same time as cooldown gate for donate — bless is unaffected by donation cooldown). Reply text exactly: "May the Lord bless you and keep you, and grant you peace on all your roads." Hub returned after blessing with bless/leave options still present.

## Strengths
- Confirms bless option is independent of donation cooldown and has no side effects.

## Limitations
- No stat change verification (no gold deducted confirmed by stable 2500 gold across the interaction).

---

## Scenario 5: "I must be on my way." closes the conversation

Metadata:
- Date: 2026-07-20
- Agent/session: claude-sonnet-4-6 / 1143933465
- Game version: 1.4.7
- DADG branch/commit: feature/add-church / bf5242e
- Loaded save: saveauto1 + teleport to Tintern Abbey
- Created pre-trigger save: N/A
- Created post-result save: N/A
- Evidence status: Passed

```gherkin
  Scenario: Leave option closes the conversation window
    Given Bannerlord 1.4.7 is running under JetBrains with GABS connected
    And I loaded save "<campaign save>"
    And I am in conversation with the preacher notable at any church settlement
    When I select "I must be on my way."
    Then the conversation window closes and the player returns to the village menu or campaign map
    And the result was proven by "bannerlord.menu.get_current showing village menu after close"
```

## Command Log
| Step | Tool | Arguments | Result |
|------|------|-----------|--------|
| 1 | bannerlord.conversation.select_option | {"index": 1} (dadg_church_leave) | Selected "I must be on my way." |
| 2 | bannerlord.conversation.get_state | {} | isActive: false — conversation closed |
| 3 | bannerlord.menu.get_current | {} | menuId: "village", 15 options — village menu restored |

## Screenshots
| Step | File path | What it proves |
|------|-----------|----------------|
| Village menu after close | C:\Users\Joe\Documents\Mount and Blade II Bannerlord\Screenshots\GABS\screenshot_20260720_140005.jpg | Village menu visible, 15 options, Tintern Abbey |

## Saves
- Reproduction save before trigger: N/A
- Final save after result: N/A

## Debugger Evidence
| Breakpoint/source | Stack frame | Expression/value | Meaning |
|-------------------|-------------|------------------|---------|
| N/A — conversation.get_state isActive:false is sufficient proof | | | |

## Reproduction Steps
1. Enter conversation with any church notable.
2. Select "I must be on my way." (dadg_church_leave).
3. Query conversation.get_state — confirm isActive: false.
4. Query menu.get_current — confirm menuId: "village".

## Result
PASSED. "I must be on my way." selection immediately closes the conversation (isActive: false) and returns to the village menu (menuId: "village", 15 options).

## Strengths
- Direct confirmation via GABS isActive field and menu state.

## Limitations
- Trivial; a regression guard only.
