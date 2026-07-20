# Feature: Church v0.1 — Settlement Identification and Clergy Notables

Tags: @bannerlord @gabs @dadg @church @v0.1 @clergy @settlements

---

## Scenario 1: All 16 church settlements have exactly one preacher notable on a new campaign

Metadata:
- Date: <fill on run>
- Agent/session: <fill on run>
- Game version: 1.4.7
- DADG branch/commit: church-v0.1-monasteries / <fill on run>
- Loaded save: <new campaign save — settle near start, no manual travel needed>
- Created pre-trigger save: <pre-trigger save>
- Created post-result save: <post-result save>
- Evidence status: Not run

```gherkin
Feature: Church v0.1 — Settlement Identification and Clergy Notables

  Scenario: All 16 church settlements have exactly one preacher notable on a new campaign
    Given Bannerlord 1.4.7 is running under JetBrains with GABS connected
    And I loaded save "<new campaign save>"
    And the baseline state was verified by "bannerlord.core.get_game_state returning InCampaign"
    When I call "bannerlord.settlement.get_settlement" for each of the 16 church settlement ids:
      | village_Malmesbury_Abbey | village_Tintern_Abbey | village_Hexham_Abbey |
      | village_Buckfast_Abbey | village_Evesham_Abbey | village_Whitland_Abbey |
      | village_Battle_Abbey | village_Byland_Abbey | village_Walsingham_Abbey |
      | village_Rievaulx_Abbey | village_Lanercost_Priory | village_Lindisfarne_Priory |
      | village_Finchale_Priory | village_Ely_Cathedral | village_Llandaff_Cathedral |
      | village_St_Asaph_Cathedral |
    Then each settlement's notable list contains exactly one hero with Occupation.Preacher
    And no settlement contains two or more preacher notables
    And the result was proven by "bannerlord.settlement.get_settlement notable list output showing one Preacher per settlement"
```

## Command Log
| Step | Tool | Arguments | Result |
|------|------|-----------|--------|
| 1 | bannerlord.core.get_game_state | {} | <fill on run> |
| 2 | bannerlord.settlement.get_settlement | {"id": "village_Tintern_Abbey"} | <fill on run> |
| ... | repeat for all 16 | | |

## Screenshots
| Step | File path | What it proves |
|------|-----------|----------------|
| After load | <fill on run> | Baseline campaign state |
| After check | <fill on run> | Settlement notable list showing one Preacher |

## Saves
- Reproduction save before trigger: <pre-trigger save>
- Final save after result: <post-result save>

## Debugger Evidence
| Breakpoint/source | Stack frame | Expression/value | Meaning |
|-------------------|-------------|------------------|---------|
| (optional) ChurchCampaignBehavior.SpawnMissingAbbots | top | settlement.Notables.Count | Confirms spawn ran |

## Reproduction Steps
1. Launch with `bannerlord-gabs-start` for version `v1.4.7`.
2. Load the new campaign save.
3. Call `bannerlord.settlement.get_settlement` for each of the 16 ids in the table above.
4. Inspect the `notables` field of each response for an entry with `occupation == "Preacher"`.
5. Verify count is exactly 1 per settlement.

## Result
<fill on run>

## Strengths
- Covers all 16 settlements from the XML config in one pass.

## Limitations
- Notable list from GABS may not distinguish Preacher sub-type; use JetBrains `settlement.Notables` eval if needed.

---

## Scenario 2: Non-church village has no preacher notable

Metadata:
- Date: <fill on run>
- Agent/session: <fill on run>
- Game version: 1.4.7
- DADG branch/commit: church-v0.1-monasteries / <fill on run>
- Loaded save: <same new campaign save as Scenario 1>
- Created pre-trigger save: <pre-trigger save>
- Created post-result save: N/A (read-only check)
- Evidence status: Not run

```gherkin
  Scenario: Non-church village has no preacher notable
    Given Bannerlord 1.4.7 is running under JetBrains with GABS connected
    And I loaded save "<new campaign save>"
    And the baseline state was verified by "bannerlord.core.get_game_state returning InCampaign"
    When I call "bannerlord.settlement.get_settlement" for a control village not in dadg.church_settlements.xml
      # Suggested control: Axminster (ordinary village bound to Launceston area, not in the 16)
      # Use bannerlord.core.run_command {"command":"campaign.get_settlement Axminster"} to confirm id
    Then that settlement's notable list contains no hero with Occupation.Preacher
    And the result was proven by "bannerlord.settlement.get_settlement notable list showing zero Preachers"
```

## Command Log
| Step | Tool | Arguments | Result |
|------|------|-----------|--------|
| 1 | bannerlord.settlement.get_settlement | {"id": "<control village id>"} | <fill on run> |

## Screenshots
| Step | File path | What it proves |
|------|-----------|----------------|
| After check | <fill on run> | Notable list with no Preacher entry |

## Saves
- Reproduction save before trigger: <pre-trigger save>
- Final save after result: N/A

## Debugger Evidence
| Breakpoint/source | Stack frame | Expression/value | Meaning |
|-------------------|-------------|------------------|---------|

## Reproduction Steps
1. Launch with `bannerlord-gabs-start` for version `v1.4.7`.
2. Load the new campaign save.
3. Identify a non-church village id (query via `campaign.show_settlements` or `campaign.get_settlement`).
4. Call `bannerlord.settlement.get_settlement` for that id.
5. Confirm no Preacher in the notable list.

## Result
<fill on run>

## Strengths
- Simple negative control confirming the feature is targeted, not global.

## Limitations
- Must pick a village that genuinely has no vanilla preacher template.

---

## Scenario 3: Clergy title is "Bishop" at cathedrals, "Abbot" at abbeys, "Prior" at priories

Metadata:
- Date: <fill on run>
- Agent/session: <fill on run>
- Game version: 1.4.7
- DADG branch/commit: church-v0.1-monasteries / <fill on run>
- Loaded save: <new campaign save>
- Created pre-trigger save: <pre-trigger save>
- Created post-result save: N/A
- Evidence status: Not run

```gherkin
  Scenario: Clergy title is correct for each settlement kind
    Given Bannerlord 1.4.7 is running under JetBrains with GABS connected
    And I loaded save "<new campaign save>"
    And the baseline state was verified by "bannerlord.core.get_game_state returning InCampaign"
    # Travel to or open dialog at one representative of each kind.
    # Use cheat: bannerlord.core.run_command {"command":"campaign.focus_mobile_party <player party id>"}
    # then bannerlord.core.run_command {"command":"campaign.capture_settlement village_Ely_Cathedral"}
    # or simply travel to the settlements in sequence.
    When I open the village menu at "village_Ely_Cathedral" (Cathedral kind) and enter conversation with the clergy notable
    Then the NPC greeting says "What brings you to Ely Cathedral?" and the donate option reads "donation to the cathedral"
    And the thanks reply contains the word "Bishop"
    When I open the village menu at "village_Tintern_Abbey" (Abbey kind) and enter conversation with the clergy notable
    Then the donate option reads "donation to the abbey"
    And the thanks reply contains the word "Abbot"
    When I open the village menu at "village_Lanercost_Priory" (Priory kind) and enter conversation with the clergy notable
    Then the donate option reads "donation to the priory"
    And the thanks reply contains the word "Prior"
    And the result was proven by "bannerlord.ui.take_screenshot showing dialog text at each settlement type"
```

## Command Log
| Step | Tool | Arguments | Result |
|------|------|-----------|--------|
| 1 | Travel to village_Ely_Cathedral | (map travel or cheat) | <fill on run> |
| 2 | bannerlord.menu.get_current | {} | <fill on run> |
| 3 | bannerlord.menu.select_option | {"option": "talk to notable"} | <fill on run> |
| 4 | bannerlord.conversation.get_state | {} | <fill on run> |
| 5 | bannerlord.ui.take_screenshot | {} | <fill on run> |

## Screenshots
| Step | File path | What it proves |
|------|-----------|----------------|
| Cathedral dialog | <fill on run> | "Bishop" in thanks text |
| Abbey dialog | <fill on run> | "Abbot" in thanks text |
| Priory dialog | <fill on run> | "Prior" in thanks text |

## Saves
- Reproduction save before trigger: <pre-trigger save>
- Final save after result: N/A

## Debugger Evidence
| Breakpoint/source | Stack frame | Expression/value | Meaning |
|-------------------|-------------|------------------|---------|
| AbbotDialogCampaignBehavior.CanDonate | local `settlement` | `_churchSettlements.GetClergyTitle(settlement).ToString()` | Confirms title mapping |

## Reproduction Steps
1. Launch with `bannerlord-gabs-start`.
2. Load save and travel to village_Ely_Cathedral.
3. Enter village, talk to preacher notable.
4. Screenshot the donate option and thanks text.
5. Repeat for village_Tintern_Abbey and village_Lanercost_Priory.

## Result
<fill on run>

## Strengths
- Tests all three kind-to-title mappings from a single run.

## Limitations
- Dialog text observation requires screenshot or manual read; GABS conversation state may not expose full text.

---

## Scenario 4: Invalid or missing dadg.church_settlements.xml leaves the church inactive with a warning

Metadata:
- Date: <fill on run>
- Agent/session: <fill on run>
- Game version: 1.4.7
- DADG branch/commit: church-v0.1-monasteries / <fill on run>
- Loaded save: <any campaign save>
- Created pre-trigger save: N/A (config mutation, not a save test)
- Created post-result save: N/A
- Evidence status: Not run

```gherkin
  Scenario: Invalid church settlements config deactivates church features with a logged warning
    # IMPORTANT: restore dadg.church_settlements.xml from git after this scenario.
    Given Bannerlord 1.4.7 is running under JetBrains with GABS connected
    And I have renamed "config/dadg.church_settlements.xml" so it cannot be found
    # Rename via PowerShell before launching the game:
    # Rename-Item "...\config\dadg.church_settlements.xml" "dadg.church_settlements.xml.bak"
    And I started a new campaign or loaded an existing save
    Then no preacher notables are spawned in any of the 16 church settlements
    And the game log contains a warning matching "No church settlements" or "invalid" (check rgl_log.txt or Rider output)
    And the village menu at village_Tintern_Abbey shows no "Attend mass", "Claim sanctuary", or "Survey the Church" options
    And the result was proven by "GABS settlement notable list showing zero Preachers and screenshot of village menu"
    # After test: restore the config file and verify spawn resumes.
```

## Command Log
| Step | Tool | Arguments | Result |
|------|------|-----------|--------|
| 1 | (Before launch) Rename config file | PowerShell | <fill on run> |
| 2 | bannerlord.settlement.get_settlement | {"id": "village_Tintern_Abbey"} | <fill on run> |
| 3 | bannerlord.menu.get_current | {} at village | <fill on run> |

## Screenshots
| Step | File path | What it proves |
|------|-----------|----------------|
| Village menu | <fill on run> | Absence of church options |

## Saves
- Reproduction save before trigger: N/A
- Final save after result: N/A

## Debugger Evidence
| Breakpoint/source | Stack frame | Expression/value | Meaning |
|-------------------|-------------|------------------|---------|
| ChurchSettlementsXmlProvider (warn branch) | stack | logged warning message | Config missing path hit |

## Reproduction Steps
1. Rename config file before launching.
2. Launch with `bannerlord-gabs-start`.
3. Start or load campaign.
4. Check notable list and village menu.
5. RESTORE config file before any further testing.

## Result
<fill on run>

## Strengths
- Validates the degrade-gracefully contract.

## Limitations
- Requires stopping and restarting the game; cannot be done on a live session without relaunching.

---

## Scenario 5: Preacher notable respawns after death (daily tick idempotency)

Metadata:
- Date: <fill on run>
- Agent/session: <fill on run>
- Game version: 1.4.7
- DADG branch/commit: church-v0.1-monasteries / <fill on run>
- Loaded save: <campaign save with clergy alive>
- Created pre-trigger save: agent_church_respawn_before_<timestamp>
- Created post-result save: agent_church_respawn_after_<timestamp>
- Evidence status: Not run

```gherkin
  Scenario: Dead preacher notable is respawned by the daily tick
    Given Bannerlord 1.4.7 is running under JetBrains with GABS connected
    And I loaded save "<campaign save>"
    And "village_Tintern_Abbey" has exactly one living preacher notable
    And I saved the game as "agent_church_respawn_before_<timestamp>"
    # Kill the abbot via debugger: set a breakpoint in SpawnMissingAbbots, evaluate
    # settlement.Notables[preacherIndex].IsAlive = false or use mission.toggleDisableDying
    # then campaign.advance_time 1 to trigger the daily tick.
    When I kill the preacher notable at "village_Tintern_Abbey" via JetBrains debugger evaluation
    And I advance campaign time by 1 day using "bannerlord.core.run_command campaign.advance_time 1"
    Then "village_Tintern_Abbey" has a new living preacher notable
    And there is still exactly one preacher notable (no duplicate from the daily tick)
    And the result was proven by "bannerlord.settlement.get_settlement notable list showing one living Preacher after advance"
```

## Command Log
| Step | Tool | Arguments | Result |
|------|------|-----------|--------|
| 1 | bannerlord.settlement.get_settlement | {"id": "village_Tintern_Abbey"} | <fill on run> |
| 2 | JetBrains: kill notable | eval hero.IsAlive = false | <fill on run> |
| 3 | bannerlord.core.run_command | {"command": "campaign.advance_time 1"} | <fill on run> |
| 4 | bannerlord.settlement.get_settlement | {"id": "village_Tintern_Abbey"} | <fill on run> |

## Screenshots
| Step | File path | What it proves |
|------|-----------|----------------|
| Before kill | <fill on run> | One living Preacher |
| After advance | <fill on run> | New living Preacher |

## Saves
- Reproduction save before trigger: agent_church_respawn_before_<timestamp>
- Final save after result: agent_church_respawn_after_<timestamp>

## Debugger Evidence
| Breakpoint/source | Stack frame | Expression/value | Meaning |
|-------------------|-------------|------------------|---------|
| ChurchCampaignBehavior.SpawnMissingAbbots | entry | settlement.StringId | Confirms daily tick fired for the settlement |

## Reproduction Steps
1. Launch and load save.
2. Set breakpoint or eval to kill Tintern Abbot's hero.
3. Advance time 1 day.
4. Re-query settlement notables.

## Result
<fill on run>

## Strengths
- Directly tests the idempotent spawn guard.

## Limitations
- Killing a notable via debugger eval may not trigger all vanilla death events; observe for side effects.

---

## Scenario 6: Save/load round-trip preserves clergy notables with no duplicates

Metadata:
- Date: <fill on run>
- Agent/session: <fill on run>
- Game version: 1.4.7
- DADG branch/commit: church-v0.1-monasteries / <fill on run>
- Loaded save: <campaign save>
- Created pre-trigger save: agent_church_saveload_before_<timestamp>
- Created post-result save: agent_church_saveload_after_<timestamp>
- Evidence status: Not run

```gherkin
  Scenario: Save and reload does not duplicate preacher notables
    Given Bannerlord 1.4.7 is running under JetBrains with GABS connected
    And I loaded save "<campaign save>"
    And all 16 church settlements each have exactly one living preacher notable
    And I saved the game as "agent_church_saveload_before_<timestamp>"
    When I reload that save immediately using "bannerlord.core.load_save"
    Then each of the 16 church settlements still has exactly one preacher notable
    And no church settlement has two or more preacher notables
    And the result was proven by "bannerlord.settlement.get_settlement notable lists after reload"
```

## Command Log
| Step | Tool | Arguments | Result |
|------|------|-----------|--------|
| 1 | bannerlord.core.save_game | {"name": "agent_church_saveload_before_<timestamp>"} | <fill on run> |
| 2 | bannerlord.core.load_save | {"name": "agent_church_saveload_before_<timestamp>"} | <fill on run> |
| 3 | bannerlord.settlement.get_settlement | {"id": "village_Tintern_Abbey"} | <fill on run> |
| 4 | (repeat for all 16) | | |

## Screenshots
| Step | File path | What it proves |
|------|-----------|----------------|
| After reload | <fill on run> | One Preacher per settlement, no duplicates |

## Saves
- Reproduction save before trigger: agent_church_saveload_before_<timestamp>
- Final save after result: agent_church_saveload_after_<timestamp>

## Debugger Evidence
| Breakpoint/source | Stack frame | Expression/value | Meaning |
|-------------------|-------------|------------------|---------|

## Reproduction Steps
1. Load campaign, confirm 1 preacher per settlement.
2. Save, reload, re-confirm.

## Result
<fill on run>

## Strengths
- Catches the double-spawn-on-reload defect.

## Limitations
- A single pass; repeated reload cycles are a more thorough stress test.
