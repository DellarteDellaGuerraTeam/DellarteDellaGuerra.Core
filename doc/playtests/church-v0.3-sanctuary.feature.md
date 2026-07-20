# Feature: Church v0.3 — Player Sanctuary, Fugitive Sanctuary, and Violation

Tags: @bannerlord @gabs @dadg @church @v0.3 @sanctuary

---

## Scenario 1: Player claims sanctuary — time passes under progress bar, raid does not eject player

Metadata:
- Date: 2026-07-20
- Agent/session: claude-sonnet-4-6 / 1143933465
- Game version: 1.4.7
- DADG branch/commit: feature/add-church / bf5242e
- Loaded save: saveauto1 (Summer 2, 1084) → teleported to Tintern Abbey, time advanced to Autumn 1 1084
- Created pre-trigger save: N/A
- Created post-result save: N/A
- Evidence status: Partial

```gherkin
Feature: Church v0.3 — Player Sanctuary, Fugitive Sanctuary, and Violation

  Scenario: Player claims sanctuary and a raid during the wait does not eject them
    Given Bannerlord 1.4.7 is running under JetBrains with GABS connected
    And I loaded save "<campaign save at village_Tintern_Abbey>"
    And village_Tintern_Abbey is in Normal state (not under raid or siege)
    And I saved the game as "agent_church_sanctuary_player_before_<timestamp>"
    When I open the village menu at village_Tintern_Abbey
    And I select the option "Claim sanctuary" (id: dadg_church_claim_sanctuary)
    Then the game switches to the "dadg_church_sanctuary" wait menu
    And the menu text contains "None may lay hands on you here" and shows days remaining
    And a progress bar is visible and reflects elapsed time
    And the "Leave the sanctuary" option is present
    # To test raid: use a JetBrains breakpoint to simulate IsUnderRaid = true, or
    # advance time until a natural raid occurs. The info message path is the proof.
    When village_Tintern_Abbey comes under raid while the player is in sanctuary
      # Trigger via: bannerlord.core.run_command {"command":"campaign.create_siege <raiding party> village_Tintern_Abbey"}
      # or set a JetBrains breakpoint on SanctuaryWaitTick and eval settlement.IsUnderRaid = true
    Then a log message appears containing "Raiders torch" and the village name
    And the player is not ejected to the village menu or campaign map
    And the result was proven by "bannerlord.menu.get_current showing dadg_church_sanctuary + screenshot of raid message"
```

## Command Log
| Step | Tool | Arguments | Result |
|------|------|-----------|--------|
| 1 | bannerlord.menu.select_option | {"index": 2} (dadg_church_claim_sanctuary) at Tintern Abbey | Selected "Claim sanctuary" |
| 2 | bannerlord.menu.get_current | {} | menuId: "dadg_church_sanctuary", text: "You have claimed sanctuary within the walls of Tintern Abbey. None may lay hands on you here, by law of God and man. (40 days of grace remain)", option: "Leave the sanctuary" |
| 3 | bannerlord.ui.take_screenshot | {} | screenshot_20260720_140516.jpg — sanctuary menu visible with text |
| 4 | (raid test) | NOT EXECUTED — no war state established | |

## Screenshots
| Step | File path | What it proves |
|------|-----------|----------------|
| Sanctuary menu | C:\Users\Joe\Documents\Mount and Blade II Bannerlord\Screenshots\GABS\screenshot_20260720_140516.jpg | Sanctuary wait menu with "40 days of grace remain" text visible on campaign map |

## Saves
- Reproduction save before trigger: N/A
- Final save after result: N/A (game crashed after this session)

## Debugger Evidence
| Breakpoint/source | Stack frame | Expression/value | Meaning |
|-------------------|-------------|------------------|---------|
| N/A | | | |

## Reproduction Steps
1. Enter Tintern Abbey village menu.
2. Select "Claim sanctuary" (dadg_church_claim_sanctuary, index 2).
3. Confirm menuId: "dadg_church_sanctuary" and text contains "40 days of grace remain".
4. For raid test: establish war state, trigger raid on Tintern Abbey, confirm sanctuary menu persists.

## Result
PARTIAL. Sanctuary claimed successfully at Tintern Abbey: menu "dadg_church_sanctuary" shown with text "You have claimed sanctuary within the walls of Tintern Abbey. None may lay hands on you here, by law of God and man. (40 days of grace remain)." Only option: "Leave the sanctuary." Raid-while-sanctuary test not executed (no war state). Note: there was an initial confusing sequence where the hierarchy screen appeared when claim_sanctuary was triggered (due to stacked menu operations) — on the second clean attempt the correct sanctuary menu appeared immediately.

RELATED DEFECT (found in later test): `dadg_church_claim_sanctuary` also appears at non-church villages Romford and Watford — the option is not filtered to church settlements (see v0.2 S1 and v0.6 S1). This means claim sanctuary can be triggered at any village.

## Strengths
- Core sanctuary activation confirmed at a church settlement: correct menu ID, exact text with 40-day grace period.

## Limitations
- Raid-while-in-sanctuary not tested; progress bar visual not confirmed (screenshot shows campaign map not village scene).
- Non-church filtering defect confirmed in separate run (see v0.2 S1 findings).

---

## Scenario 2: Progress bar and days-remaining survive a save/load round-trip

Metadata:
- Date: 2026-07-20
- Agent/session: claude-sonnet-4-6 / 1143933465
- Game version: 1.4.7
- DADG branch/commit: feature/add-church / bf5242e
- Loaded save: N/A — not tested
- Created pre-trigger save: N/A
- Created post-result save: N/A
- Evidence status: Not run

```gherkin
  Scenario: Sanctuary progress persists across a save/load cycle
    Given Bannerlord 1.4.7 is running under JetBrains with GABS connected
    And I am in the sanctuary wait menu at village_Tintern_Abbey with ~10 days elapsed
    And the progress bar shows approximately 25% (10/40 days)
    And I saved the game as "agent_church_sanctuary_saveload_before_<timestamp>"
    When I load the save "agent_church_sanctuary_saveload_before_<timestamp>"
    Then the game returns to the sanctuary wait menu (not the village menu)
    And the progress bar still shows approximately 25% elapsed
    And the days-remaining in the menu text matches the original value
    And the result was proven by "bannerlord.menu.get_current returning dadg_church_sanctuary + screenshot of progress bar after reload"
```

## Command Log
| Step | Tool | Arguments | Result |
|------|------|-----------|--------|
| 1 | (enter sanctuary and wait ~10 days) | campaign.advance_time 10 | <fill on run> |
| 2 | bannerlord.core.save_game | {"name": "agent_church_sanctuary_saveload_before_<timestamp>"} | <fill on run> |
| 3 | bannerlord.core.load_save | {"name": "agent_church_sanctuary_saveload_before_<timestamp>"} | <fill on run> |
| 4 | bannerlord.menu.get_current | {} | <fill on run — confirm sanctuary menu> |
| 5 | bannerlord.ui.take_screenshot | {} | <fill on run — progress bar> |

## Screenshots
| Step | File path | What it proves |
|------|-----------|----------------|
| Before save | <fill on run> | Progress bar at ~25% |
| After reload | <fill on run> | Progress bar still ~25% |

## Saves
- Reproduction save before trigger: agent_church_sanctuary_saveload_before_<timestamp>
- Final save after result: agent_church_sanctuary_saveload_after_<timestamp>

## Debugger Evidence
| Breakpoint/source | Stack frame | Expression/value | Meaning |
|-------------------|-------------|------------------|---------|
| SanctuaryCampaignBehavior.SyncData | _playerSanctuaryStart | value after load | Start time preserved |

## Reproduction Steps
1. Claim sanctuary, advance ~10 days.
2. Save, reload.
3. Verify sanctuary menu and matching progress bar.

## Result
<fill on run>

## Strengths
- Tests _playerSanctuaryStart SyncData persistence.

## Limitations
- Progress bar percentage is a visual estimate; also check days-left text in menu.

---

## Scenario 3: 40-day sanctuary expires and ejects player back to the village menu

Metadata:
- Date: 2026-07-20
- Agent/session: claude-sonnet-4-6 / 1143933465
- Game version: 1.4.7
- DADG branch/commit: feature/add-church / bf5242e
- Loaded save: N/A — not tested
- Created pre-trigger save: N/A
- Created post-result save: N/A
- Evidence status: Not run

```gherkin
  Scenario: Sanctuary expires after 40 campaign days and the player is ejected
    Given Bannerlord 1.4.7 is running under JetBrains with GABS connected
    And I am in the sanctuary wait menu at village_Tintern_Abbey with ~35 days elapsed
    And I saved the game as "agent_church_sanctuary_expire_before_<timestamp>"
    When I advance campaign time by 6 more days (total elapsed > 40 days)
    Then an info message appears containing "Your forty days of sanctuary are spent"
    And the game switches back to the village menu
    And the "Claim sanctuary" option is available again (can re-enter if desired)
    And the result was proven by "bannerlord.menu.get_current showing village menu + screenshot of expiry message"
```

## Command Log
| Step | Tool | Arguments | Result |
|------|------|-----------|--------|
| 1 | (enter sanctuary and advance 35 days) | campaign.advance_time 35 | <fill on run> |
| 2 | bannerlord.core.save_game | {"name": "agent_church_sanctuary_expire_before_<timestamp>"} | <fill on run> |
| 3 | bannerlord.core.run_command | {"command": "campaign.advance_time 6"} | <fill on run> |
| 4 | bannerlord.menu.get_current | {} | <fill on run — confirm village menu> |

## Screenshots
| Step | File path | What it proves |
|------|-----------|----------------|
| Expiry message | <fill on run> | "forty days of sanctuary are spent" text |
| After expiry | <fill on run> | Village menu restored |

## Saves
- Reproduction save before trigger: agent_church_sanctuary_expire_before_<timestamp>
- Final save after result: agent_church_sanctuary_expire_after_<timestamp>

## Debugger Evidence
| Breakpoint/source | Stack frame | Expression/value | Meaning |
|-------------------|-------------|------------------|---------|
| SanctuaryCampaignBehavior.SanctuaryWaitTick | expiry branch | SanctuaryPolicy.Evaluate result | Expired outcome |

## Reproduction Steps
1. Claim sanctuary, advance 35 days.
2. Advance 6 more days.
3. Confirm expiry message and village menu.

## Result
<fill on run>

## Strengths
- Tests the 40-day cap defined in SanctuaryPolicy.PlayerSanctuaryDays.

## Limitations
- campaign.advance_time may overshoot; JetBrains breakpoint in SanctuaryWaitTick confirms exact trigger.

---

## Scenario 4: Early "Leave the sanctuary" option exits to village menu

Metadata:
- Date: 2026-07-20
- Agent/session: claude-sonnet-4-6 / 1143933465
- Game version: 1.4.7
- DADG branch/commit: feature/add-church / bf5242e
- Loaded save: saveauto1 → sanctuary claimed at Tintern Abbey
- Created pre-trigger save: N/A
- Created post-result save: N/A
- Evidence status: Passed

```gherkin
  Scenario: Player can leave sanctuary early via the "Leave the sanctuary" option
    Given Bannerlord 1.4.7 is running under JetBrains with GABS connected
    And I am in the sanctuary wait menu at village_Tintern_Abbey
    When I select the "Leave the sanctuary" option (isLeave: true)
    Then the game switches back to the village menu
    And _playerSanctuaryStart is reset (the option is available again)
    And the result was proven by "bannerlord.menu.get_current showing village menu + screenshot"
```

## Command Log
| Step | Tool | Arguments | Result |
|------|------|-----------|--------|
| 1 | bannerlord.menu.select_option | {"index": 0} (dadg_church_sanctuary_leave) | Selected "Leave the sanctuary" |
| 2 | bannerlord.menu.get_current | {} | menuId: "village", 15 options — village menu restored |

## Screenshots
| Step | File path | What it proves |
|------|-----------|----------------|
| After leave | C:\Users\Joe\Documents\Mount and Blade II Bannerlord\Screenshots\GABS\screenshot_20260720_140005.jpg | Village menu after leaving sanctuary (15 options present) |

## Saves
- Reproduction save before trigger: N/A
- Final save after result: N/A

## Debugger Evidence
| Breakpoint/source | Stack frame | Expression/value | Meaning |
|-------------------|-------------|------------------|---------|
| N/A — menu state sufficient | | | |

## Reproduction Steps
1. Claim sanctuary at any church village.
2. Select "Leave the sanctuary" (index 0 from dadg_church_sanctuary menu).
3. Confirm menuId returns to "village".

## Result
PASSED. "Leave the sanctuary" selection immediately returns to the village menu (menuId: "village", 15 options). Claim sanctuary re-appeared as an option at index 2, confirming state was reset.

## Strengths
- Direct confirmation via GABS menu state.

## Limitations
- _playerSanctuaryStart reset not verified via JetBrains (game running).

---

## Scenario 5: Defeated AI lord appears in the nearest church settlement and stays for up to 20 days

Metadata:
- Date: 2026-07-20
- Agent/session: claude-sonnet-4-6 / 1143933465
- Game version: 1.4.7
- DADG branch/commit: feature/add-church / bf5242e
- Loaded save: N/A — not tested
- Created pre-trigger save: N/A
- Created post-result save: N/A
- Evidence status: Not run

```gherkin
  Scenario: Defeated lord takes sanctuary at nearest church settlement and is held for up to 20 days
    # Setup: place an enemy lord party near a church settlement, then defeat them.
    # Use campaign.focus_mobile_party to locate, then attack via the campaign map.
    # Or use campaign.capture_lord <lord_name> then release them at a church settlement.
    Given Bannerlord 1.4.7 is running under JetBrains with GABS connected
    And I loaded save "<campaign save>"
    And an enemy AI lord (not prisoner) exists with a mobile party
    And I saved the game as "agent_church_fugitive_before_<timestamp>"
    When I defeat the AI lord's party in a map battle near village_Tintern_Abbey
      # Engage via campaign map; handle battle with bannerlord-gabs-battle-manager if needed.
    Then the MobilePartyDestroyed event fires for that lord's party
    And the lord appears in the notable/hero list of the nearest church settlement
    And if the player had previously met the lord, an info message reads "<LORD_NAME> has taken sanctuary at <MONASTERY_NAME>"
    And 1 day later the lord is still in the same settlement (daily tick re-asserts placement)
    And 20 days later the lord is no longer tagged (sanctuary expired)
    And the result was proven by "JetBrains eval of _fugitiveSanctuaries dict + bannerlord.settlement.get_settlement hero list"
```

## Command Log
| Step | Tool | Arguments | Result |
|------|------|-----------|--------|
| 1 | (locate enemy lord) | campaign.focus_mobile_party | <fill on run> |
| 2 | (engage in battle) | | <fill on run> |
| 3 | bannerlord.settlement.get_settlement | {"id": "village_Tintern_Abbey"} | <fill on run — check lord present> |
| 4 | JetBrains eval | _fugitiveSanctuaries.ContainsKey(lord) | <fill on run — true> |
| 5 | bannerlord.core.run_command | {"command": "campaign.advance_time 1"} | <fill on run> |
| 6 | bannerlord.settlement.get_settlement | {"id": "village_Tintern_Abbey"} | <fill on run — lord still present> |
| 7 | bannerlord.core.run_command | {"command": "campaign.advance_time 20"} | <fill on run> |
| 8 | JetBrains eval | _fugitiveSanctuaries.ContainsKey(lord) | <fill on run — false> |

## Screenshots
| Step | File path | What it proves |
|------|-----------|----------------|
| After defeat | <fill on run> | Sanctuary message on screen |
| Next day | <fill on run> | Lord still at settlement |
| After 20 days | <fill on run> | Lord untagged, gone from dict |

## Saves
- Reproduction save before trigger: agent_church_fugitive_before_<timestamp>
- Final save after result: agent_church_fugitive_after_<timestamp>

## Debugger Evidence
| Breakpoint/source | Stack frame | Expression/value | Meaning |
|-------------------|-------------|------------------|---------|
| SanctuaryCampaignBehavior.OnMobilePartyDestroyed | entry | mobileParty.IsLordParty | Correct event path |
| SanctuaryCampaignBehavior.OnDailyTickHero | re-place branch | hero.CurrentSettlement != monastery | Daily re-assertion |

## Reproduction Steps
1. Defeat enemy lord near Tintern Abbey.
2. Check settlement hero list and dict.
3. Advance 1 day — lord still there.
4. Advance 20 days — lord no longer in dict.

## Result
<fill on run>

## Strengths
- Tests the full fugitive lifecycle: placement, holding, and 20-day expiry.

## Limitations
- Natural clan respawn (lord gets a new party) will end sanctuary early; this is intended behavior, not a defect.

---

## Scenario 6: Fugitive sanctuary tag survives a save/load round-trip

Metadata:
- Date: 2026-07-20
- Agent/session: claude-sonnet-4-6 / 1143933465
- Game version: 1.4.7
- DADG branch/commit: feature/add-church / bf5242e
- Loaded save: N/A — not tested
- Created pre-trigger save: N/A
- Created post-result save: N/A
- Evidence status: Not run

```gherkin
  Scenario: Fugitive sanctuary tag persists across save and reload
    Given Bannerlord 1.4.7 is running under JetBrains with GABS connected
    And an AI lord is tagged in _fugitiveSanctuaries at village_Tintern_Abbey
    And I saved the game as "agent_church_fugitive_saveload_before_<timestamp>"
    When I load the save "agent_church_fugitive_saveload_before_<timestamp>"
    Then the lord is still tagged in _fugitiveSanctuaries (JetBrains eval)
    And the lord's CurrentSettlement is still village_Tintern_Abbey
    And the result was proven by "JetBrains eval of _fugitiveSanctuaries after reload + settlement hero list"
```

## Command Log
| Step | Tool | Arguments | Result |
|------|------|-----------|--------|
| 1 | JetBrains eval | _fugitiveSanctuaries count | <fill on run — > 0> |
| 2 | bannerlord.core.save_game | {"name": "agent_church_fugitive_saveload_before_<timestamp>"} | <fill on run> |
| 3 | bannerlord.core.load_save | {"name": "agent_church_fugitive_saveload_before_<timestamp>"} | <fill on run> |
| 4 | JetBrains eval | _fugitiveSanctuaries count | <fill on run — still > 0> |
| 5 | bannerlord.settlement.get_settlement | {"id": "village_Tintern_Abbey"} | <fill on run — lord present> |

## Screenshots
| Step | File path | What it proves |
|------|-----------|----------------|
| After reload | <fill on run> | Lord still in settlement |

## Saves
- Reproduction save before trigger: agent_church_fugitive_saveload_before_<timestamp>
- Final save after result: agent_church_fugitive_saveload_after_<timestamp>

## Debugger Evidence
| Breakpoint/source | Stack frame | Expression/value | Meaning |
|-------------------|-------------|------------------|---------|
| SanctuaryCampaignBehavior.SyncData | _dadgChurchFugitiveSanctuaries | dict restored | Container round-trip |

## Reproduction Steps
1. Establish fugitive sanctuary.
2. Save and reload.
3. Verify dict and settlement state via JetBrains.

## Result
<fill on run>

## Strengths
- Tests the custom Dictionary<Hero,Settlement> save container (ChurchSaveableTypeDefiner).

## Limitations
- If the container registration is missing, save will silently drop the data; compare dict count before/after.

---

## Scenario 7: Player can drag a war-enemy fugitive from the cloister (sacrilege applies)

Metadata:
- Date: 2026-07-20
- Agent/session: claude-sonnet-4-6 / 1143933465
- Game version: 1.4.7
- DADG branch/commit: feature/add-church / bf5242e
- Loaded save: N/A — not tested; observed that dadg_church_drag_fugitive option was present in village menu but with blank name ("Drag  from the cloister") indicating no current fugitive
- Created pre-trigger save: N/A
- Created post-result save: N/A
- Evidence status: Not run

```gherkin
  Scenario: Dragging a war-enemy fugitive from the cloister adds them as prisoner and triggers sacrilege
    Given Bannerlord 1.4.7 is running under JetBrains with GABS connected
    And a fugitive lord of an enemy faction is in sanctuary at village_Tintern_Abbey
    And the player is at war with that lord's faction
    And I note relations R_local (player vs Tintern abbot) and R_other (player vs Byland abbot)
    And I saved the game as "agent_church_drag_before_<timestamp>"
    When I open the village menu at village_Tintern_Abbey
    Then the option "Drag <LORD_NAME> from the cloister" is visible and enabled (at war)
    When I select that option
    Then TakePrisonerAction adds the lord to the player party as a prisoner
    And ChurchSacrilege.Apply fires: relation with Tintern abbot is R_local - 15
    And relation with Byland abbot is R_other - 5
    And a sacrilege info message appears on screen
    And the lord is no longer tagged in _fugitiveSanctuaries
    And the result was proven by "GABS party prisoner list + JetBrains relation eval + screenshot of sacrilege message"
```

## Command Log
| Step | Tool | Arguments | Result |
|------|------|-----------|--------|
| 1 | bannerlord.menu.get_current | {} | <fill on run — drag option visible> |
| 2 | JetBrains eval | CharacterRelationManager.GetHeroRelation(Hero.MainHero, tinternAbbot) | <fill on run — R_local> |
| 3 | bannerlord.menu.select_option | {"option": "dadg_church_drag_fugitive"} | <fill on run> |
| 4 | bannerlord.party.get_player_party | {} | <fill on run — lord as prisoner> |
| 5 | JetBrains eval | CharacterRelationManager.GetHeroRelation(Hero.MainHero, tinternAbbot) | <fill on run — R_local-15> |
| 6 | JetBrains eval | _fugitiveSanctuaries.ContainsKey(lord) | <fill on run — false> |

## Screenshots
| Step | File path | What it proves |
|------|-----------|----------------|
| Drag option visible | <fill on run> | Enabled option with lord's name |
| After drag | <fill on run> | Sacrilege message + prisoner |

## Saves
- Reproduction save before trigger: agent_church_drag_before_<timestamp>
- Final save after result: agent_church_drag_after_<timestamp>

## Debugger Evidence
| Breakpoint/source | Stack frame | Expression/value | Meaning |
|-------------------|-------------|------------------|---------|
| SanctuaryCampaignBehavior.DragFugitive | after TakePrisoner | lord.IsPrisoner | Confirmed captured |
| ChurchSacrilege.Apply | after cascade | both relations | -15/-5 applied |

## Reproduction Steps
1. Ensure fugitive in sanctuary and player at war with that faction.
2. Open village menu — drag option enabled.
3. Select it — lord captured, sacrilege applied.

## Result
<fill on run>

## Strengths
- Tests the core strategic risk/reward of violating sanctuary.

## Limitations
- Requires coordination of war state, fugitive state, and presence at the right settlement.

---

## Scenario 8: Drag option is greyed when not at war with the fugitive's faction

Metadata:
- Date: 2026-07-20
- Agent/session: claude-sonnet-4-6 / 1143933465
- Game version: 1.4.7
- DADG branch/commit: feature/add-church / bf5242e
- Loaded save: N/A — not tested
- Created pre-trigger save: N/A
- Created post-result save: N/A
- Evidence status: Not run

```gherkin
  Scenario: Drag option is greyed with tooltip when not at war with the fugitive
    Given Bannerlord 1.4.7 is running under JetBrains with GABS connected
    And a fugitive lord is in sanctuary at village_Tintern_Abbey
    And the player is NOT at war with that lord's faction
    When I open the village menu at village_Tintern_Abbey
    Then the option "Drag <LORD_NAME> from the cloister" is visible but greyed out (disabled)
    And the tooltip reads "You are not at war with <LORD_NAME>"
    And the result was proven by "screenshot of greyed drag option with tooltip"
```

## Command Log
| Step | Tool | Arguments | Result |
|------|------|-----------|--------|
| 1 | bannerlord.menu.get_current | {} | <fill on run — drag option greyed> |
| 2 | bannerlord.ui.take_screenshot | {} | <fill on run> |

## Screenshots
| Step | File path | What it proves |
|------|-----------|----------------|
| Greyed drag option | <fill on run> | Tooltip with peace condition |

## Saves
- Reproduction save before trigger: <pre-trigger save>
- Final save after result: N/A

## Debugger Evidence
| Breakpoint/source | Stack frame | Expression/value | Meaning |
|-------------------|-------------|------------------|---------|
| SanctuaryCampaignBehavior.CanDragFugitive | enabled = false branch | IsAtWarWithPlayer(target) | Returns false |

## Reproduction Steps
1. Establish fugitive at church settlement.
2. Ensure no war with fugitive faction.
3. Open village menu and observe greyed option.

## Result
<fill on run>

## Strengths
- Confirms war gate prevents accidental sanctuary violation.

## Limitations
- Peace condition may be hard to reproduce if the campaign is already at war with most factions.
