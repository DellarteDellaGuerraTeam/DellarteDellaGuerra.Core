# Feature: Church v0.1 — Donation Dialog and Policy

Tags: @bannerlord @gabs @dadg @church @v0.1 @donation

---

## Scenario 1: Donating 500 gold gives +2 relation, +1 renown, and locks the option for 7 days

Metadata:
- Date: <fill on run>
- Agent/session: <fill on run>
- Game version: 1.4.7
- DADG branch/commit: church-v0.1-monasteries / <fill on run>
- Loaded save: <campaign save at Tintern Abbey with >=500 gold>
- Created pre-trigger save: agent_church_donation_before_<timestamp>
- Created post-result save: agent_church_donation_after_<timestamp>
- Evidence status: Not run

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
| 1 | bannerlord.party.get_player_party | {} | <fill on run — note gold> |
| 2 | bannerlord.settlement.get_settlement | {"id":"village_Tintern_Abbey"} | <fill on run — note abbot relation> |
| 3 | (enter village and conversation) | | |
| 4 | bannerlord.conversation.get_state | {} | <fill on run> |
| 5 | bannerlord.conversation.select_option | {"option": "dadg_church_donate"} | <fill on run> |
| 6 | bannerlord.party.get_player_party | {} | <fill on run — confirm gold -500> |
| 7 | JetBrains eval | abbot.GetRelation(Hero.MainHero) | <fill on run — confirm +2> |

## Screenshots
| Step | File path | What it proves |
|------|-----------|----------------|
| Donate option visible | <fill on run> | Option text with 500 denar cost |
| After donation | <fill on run> | Thanks reply with Abbot title |
| Party state | <fill on run> | Gold reduced |

## Saves
- Reproduction save before trigger: agent_church_donation_before_<timestamp>
- Final save after result: agent_church_donation_after_<timestamp>

## Debugger Evidence
| Breakpoint/source | Stack frame | Expression/value | Meaning |
|-------------------|-------------|------------------|---------|
| AbbotDialogCampaignBehavior.Donate | after GiveGoldAction | Hero.MainHero.Gold | Confirms 500 deducted |
| AbbotDialogCampaignBehavior.Donate | after ChangeRelationAction | abbot relation | Confirms +2 |

## Reproduction Steps
1. Load save at village_Tintern_Abbey with >=500 gold.
2. Enter village, talk to Preacher notable.
3. Select donate option.
4. Observe reply text, then query gold and relation.

## Result
<fill on run>

## Strengths
- Tests all three donation effects (gold, relation, renown) in one action.

## Limitations
- Renown delta is +1 (small); confirm via hero tooltip or JetBrains eval of Hero.MainHero.Renown.

---

## Scenario 2: Donation option is hidden when the player has fewer than 500 gold

Metadata:
- Date: <fill on run>
- Agent/session: <fill on run>
- Game version: 1.4.7
- DADG branch/commit: church-v0.1-monasteries / <fill on run>
- Loaded save: <campaign save at village_Tintern_Abbey with <500 gold>
- Created pre-trigger save: <pre-trigger save>
- Created post-result save: N/A
- Evidence status: Not run

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
| 1 | (drain gold below 500) | | <fill on run> |
| 2 | bannerlord.conversation.get_state | {} | <fill on run> |
| 3 | bannerlord.ui.take_screenshot | {} | <fill on run> |

## Screenshots
| Step | File path | What it proves |
|------|-----------|----------------|
| Conversation hub | <fill on run> | Donate option absent |

## Saves
- Reproduction save before trigger: <pre-trigger save>
- Final save after result: N/A

## Debugger Evidence
| Breakpoint/source | Stack frame | Expression/value | Meaning |
|-------------------|-------------|------------------|---------|
| AbbotDialogCampaignBehavior.CanDonate | return false path | Hero.MainHero.Gold | Confirms gold check |

## Reproduction Steps
1. Ensure player has <500 gold (spend or use cheat mode to reduce).
2. Enter conversation at a church settlement.
3. Verify donate option absent.

## Result
<fill on run>

## Strengths
- Confirms policy gate (InsufficientGold) prevents the option appearing.

## Limitations
- Manual gold management; use cheats in debug mode.

---

## Scenario 3: Donation option is hidden for 7 days after donating (cooldown)

Metadata:
- Date: <fill on run>
- Agent/session: <fill on run>
- Game version: 1.4.7
- DADG branch/commit: church-v0.1-monasteries / <fill on run>
- Loaded save: <campaign save post-donation>
- Created pre-trigger save: agent_church_cooldown_before_<timestamp>
- Created post-result save: agent_church_cooldown_after_<timestamp>
- Evidence status: Not run

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
| 1 | bannerlord.core.save_game | {"name": "agent_church_cooldown_before_<timestamp>"} | <fill on run> |
| 2 | bannerlord.core.load_save | {"name": "agent_church_cooldown_before_<timestamp>"} | <fill on run> |
| 3 | bannerlord.conversation.get_state | {} | <fill on run — confirm absent> |
| 4 | bannerlord.core.run_command | {"command": "campaign.advance_time 7"} | <fill on run> |
| 5 | bannerlord.conversation.get_state | {} | <fill on run — confirm present> |

## Screenshots
| Step | File path | What it proves |
|------|-----------|----------------|
| Immediately after donation | <fill on run> | Donate option absent |
| After 7-day advance | <fill on run> | Donate option present |

## Saves
- Reproduction save before trigger: agent_church_cooldown_before_<timestamp>
- Final save after result: agent_church_cooldown_after_<timestamp>

## Debugger Evidence
| Breakpoint/source | Stack frame | Expression/value | Meaning |
|-------------------|-------------|------------------|---------|
| AbbotDialogCampaignBehavior.CanDonate | local daysSince | daysSinceLastDonation value | Confirms cooldown calculation |

## Reproduction Steps
1. Donate, immediately try to donate again — option absent.
2. Save and reload — option still absent.
3. Advance 7 days — option present.

## Result
<fill on run>

## Strengths
- Tests both cooldown enforcement and save/load persistence of _lastDonationTimes.

## Limitations
- campaign.advance_time may advance weeks — confirm exact days elapsed via JetBrains eval.

---

## Scenario 4: "Bless me, Father" flavor option is always available and returns to hub

Metadata:
- Date: <fill on run>
- Agent/session: <fill on run>
- Game version: 1.4.7
- DADG branch/commit: church-v0.1-monasteries / <fill on run>
- Loaded save: <any campaign save near a church settlement>
- Created pre-trigger save: <pre-trigger save>
- Created post-result save: N/A
- Evidence status: Not run

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
| 1 | bannerlord.conversation.get_state | {} | <fill on run> |
| 2 | bannerlord.conversation.select_option | {"option": "dadg_church_blessing"} | <fill on run> |
| 3 | bannerlord.conversation.get_state | {} | <fill on run — confirm hub> |

## Screenshots
| Step | File path | What it proves |
|------|-----------|----------------|
| Blessing reply | <fill on run> | Flavor text shown, no stats changed |
| Back at hub | <fill on run> | Hub options present |

## Saves
- Reproduction save before trigger: <pre-trigger save>
- Final save after result: N/A

## Debugger Evidence
| Breakpoint/source | Stack frame | Expression/value | Meaning |
|-------------------|-------------|------------------|---------|

## Reproduction Steps
1. Enter conversation with any church notable.
2. Select "Bless me, Father."
3. Read reply, confirm no effect and return to hub.

## Result
<fill on run>

## Strengths
- Confirms flavor option has no accidental side effects.

## Limitations
- Pure UI observation.

---

## Scenario 5: "I must be on my way." closes the conversation

Metadata:
- Date: <fill on run>
- Agent/session: <fill on run>
- Game version: 1.4.7
- DADG branch/commit: church-v0.1-monasteries / <fill on run>
- Loaded save: <any campaign save near a church settlement>
- Created pre-trigger save: N/A
- Created post-result save: N/A
- Evidence status: Not run

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
| 1 | bannerlord.conversation.select_option | {"option": "dadg_church_leave"} | <fill on run> |
| 2 | bannerlord.menu.get_current | {} | <fill on run — confirm village> |

## Screenshots
| Step | File path | What it proves |
|------|-----------|----------------|
| After close | <fill on run> | Village menu or map visible |

## Saves
- Reproduction save before trigger: N/A
- Final save after result: N/A

## Debugger Evidence
| Breakpoint/source | Stack frame | Expression/value | Meaning |
|-------------------|-------------|------------------|---------|

## Reproduction Steps
1. Enter conversation with any church notable.
2. Select "I must be on my way." option.
3. Confirm conversation closed.

## Result
<fill on run>

## Strengths
- Basic smoke test for dialog exit.

## Limitations
- Trivial; include mainly as regression guard.
