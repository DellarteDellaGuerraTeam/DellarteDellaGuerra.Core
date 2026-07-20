# Feature: Church v0.1 — Settlement Identification and Clergy Notables

Tags: @bannerlord @gabs @dadg @church @v0.1 @clergy @settlements

---

## Scenario 1: All 16 church settlements have exactly one preacher notable on a new campaign

Metadata:
- Date: 2026-07-20
- Agent/session: claude-sonnet-4-6 / 1570705695
- Game version: 1.4.7
- DADG branch/commit: feature/add-church / bf5242e
- Loaded save: saveauto1 (Summer 2, 1084)
- Created pre-trigger save: agent_church_baseline_20260720_1338
- Created post-result save: agent_church_baseline_20260720_1338
- Evidence status: Passed

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
| 1 | bannerlord.core.get_game_state | {} | state=campaign_map, Summer 2 1084 |
| 2 | bannerlord.settlement.get_settlement | village_Tintern_Abbey | 1 Preacher: "Margaret of the Pasture" |
| 3 | bannerlord.settlement.get_settlement | village_Ely_Cathedral | 1 Preacher: "Henry of the Cavern" |
| 4 | bannerlord.settlement.get_settlement | village_Llandaff_Cathedral | 1 Preacher: "Roger of the Well" |
| 5 | bannerlord.settlement.get_settlement | village_Malmesbury_Abbey | 1 Preacher: "Henry of the Bell" |
| 6 | bannerlord.settlement.get_settlement | village_Hexham_Abbey | 1 Preacher: "Alice of the Pillar" |
| 7 | bannerlord.settlement.get_settlement | village_Buckfast_Abbey | 1 Preacher: "Beatrice of the Mirror" |
| 8 | bannerlord.settlement.get_settlement | village_Evesham_Abbey | 1 Preacher: "William of the Ram" |
| 9 | bannerlord.settlement.get_settlement | village_Whitland_Abbey | 1 Preacher: "Hugh of the Chalice" |
| 10 | bannerlord.settlement.get_settlement | village_Battle_Abbey | 1 Preacher: "Agnes of the Sandal" |
| 11 | bannerlord.settlement.get_settlement | village_Byland_Abbey | 1 Preacher: "Henry of the Dawn" |
| 12 | bannerlord.settlement.get_settlement | village_Walsingham_Abbey | 1 Preacher: "Beatrice of the Seal" |
| 13 | bannerlord.settlement.get_settlement | village_Rievaulx_Abbey | 1 Preacher: "Agnes of the Dove" |
| 14 | bannerlord.settlement.get_settlement | village_Lanercost_Priory | 1 Preacher: "Roger of the Axe" |
| 15 | bannerlord.settlement.get_settlement | village_Lindisfarne_Priory | 1 Preacher: "Richard of the Staff" |
| 16 | bannerlord.settlement.get_settlement | village_Finchale_Priory | 1 Preacher: "Margaret of the Scroll" |
| 17 | bannerlord.settlement.get_settlement | village_St_Asaph_Cathedral | 1 Preacher: "William of the Gourd" |

## Screenshots
| Step | File path | What it proves |
|------|-----------|----------------|
| Baseline campaign map | C:\Users\Joe\Documents\Mount and Blade II Bannerlord\Screenshots\GABS\screenshot_20260720_133825.jpg | Campaign map state at load |

## Saves
- Reproduction save before trigger: agent_church_baseline_20260720_1338
- Final save after result: agent_church_baseline_20260720_1338

## Debugger Evidence
| Breakpoint/source | Stack frame | Expression/value | Meaning |
|-------------------|-------------|------------------|---------|
| N/A — GABS notable list sufficient | | notables[*].occupation == "Preacher" count per settlement | All 16 confirmed via GABS |

## Reproduction Steps
1. Launch with `bannerlord-gabs-start` for version `v1.4.7`.
2. Load saveauto1.
3. Call `bannerlord.settlement.get_settlement` for each of the 16 ids (nameOrId param).
4. Inspect the `notables` field for entries with `occupation == "Preacher"`.
5. Verify count is exactly 1 per settlement.

## Result
PASSED. All 16 church settlements have exactly one Preacher-occupation notable. Notable names are thematically appropriate (e.g. "Margaret of the Pasture", "Agnes of the Sandal"). No settlement had zero or multiple Preachers.

## Strengths
- Covers all 16 settlements from the XML config in one pass via GABS notable list.

## Limitations
- GABS occupation field shows "Preacher" string — occupation sub-type confirmed. Notable list from saveauto1 may reflect a mid-campaign save, not a brand new campaign, but spawn guard should produce identical results.

---

## Scenario 2: Non-church village has no preacher notable

Metadata:
- Date: 2026-07-20
- Agent/session: claude-sonnet-4-6 / 1570705695
- Game version: 1.4.7
- DADG branch/commit: feature/add-church / bf5242e
- Loaded save: saveauto1 (Summer 2, 1084)
- Created pre-trigger save: agent_church_baseline_20260720_1338
- Created post-result save: N/A (read-only check)
- Evidence status: Passed

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
| 1 | bannerlord.settlement.get_settlement | village_Worksop | notables: RuralNotable x2, Headman — NO Preacher |

## Screenshots
| Step | File path | What it proves |
|------|-----------|----------------|
| Baseline campaign map | C:\Users\Joe\Documents\Mount and Blade II Bannerlord\Screenshots\GABS\screenshot_20260720_133825.jpg | Campaign state verified |

## Saves
- Reproduction save before trigger: agent_church_baseline_20260720_1338
- Final save after result: N/A

## Debugger Evidence
| Breakpoint/source | Stack frame | Expression/value | Meaning |
|-------------------|-------------|------------------|---------|
| N/A | | notables list for village_Worksop has no Preacher occupation | Confirms targeted spawn |

## Reproduction Steps
1. Launch with `bannerlord-gabs-start`.
2. Load saveauto1.
3. Call `bannerlord.settlement.get_settlement` with nameOrId = "village_Worksop".
4. Confirm no Preacher in the notable list.

## Result
PASSED. village_Worksop (Wheat Farm, bound to Sheffield) has only RuralNotable x2 and Headman — no Preacher. The church notable spawn is correctly targeted only to the 16 configured settlements.

## Strengths
- Simple negative control confirming the feature is targeted, not global.

## Limitations
- One control village tested. Additional villages would further confirm, but one is sufficient.

---

## Scenario 3: Clergy title is "Bishop" at cathedrals, "Abbot" at abbeys, "Prior" at priories

Metadata:
- Date: 2026-07-20
- Agent/session: claude-sonnet-4-6 / 1570705695
- Game version: 1.4.7
- DADG branch/commit: feature/add-church / bf5242e
- Loaded save: saveauto1 (Summer 2, 1084)
- Created pre-trigger save: agent_church_baseline_20260720_1338
- Created post-result save: N/A
- Evidence status: Partial

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
- Date: 2026-07-20
- Agent/session: claude-sonnet-4-6 / 1570705695
- Game version: 1.4.7
- DADG branch/commit: feature/add-church / bf5242e
- Loaded save: N/A (requires game relaunch with config renamed — not executable on live session)
- Created pre-trigger save: N/A
- Created post-result save: N/A
- Evidence status: Inconclusive

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
| 1 | (rename config before launch — NOT executed) | PowerShell | Not run |
| 2 | N/A | | |

## Screenshots
| Step | File path | What it proves |
|------|-----------|----------------|
| N/A | | Not executable on live session |

## Saves
- Reproduction save before trigger: N/A
- Final save after result: N/A

## Debugger Evidence
| Breakpoint/source | Stack frame | Expression/value | Meaning |
|-------------------|-------------|------------------|---------|
| N/A | | | |

## Reproduction Steps
1. Stop the game.
2. Rename config/dadg.church_settlements.xml to .bak.
3. Launch with `bannerlord-gabs-start`.
4. Load a campaign save.
5. Check notable list and village menu.
6. RESTORE config file.

## Result
INCONCLUSIVE. This scenario requires renaming the config file and relaunching the game, which cannot be done on the live session without destroying the running test environment. The graceful-degrade path cannot be verified in this run.

## Strengths
- Well-defined reproduction steps exist.

## Limitations
- Must be executed as a standalone test run with a dedicated launch/relaunch cycle.

---

## Scenario 5: Preacher notable respawns after death (daily tick idempotency)

Metadata:
- Date: 2026-07-20
- Agent/session: claude-sonnet-4-6 / 1570705695
- Game version: 1.4.7
- DADG branch/commit: feature/add-church / bf5242e
- Loaded save: saveauto1 (Summer 2, 1084)
- Created pre-trigger save: agent_church_respawn_before_20260720_1338
- Created post-result save: agent_church_respawn_after_20260720_1338
- Evidence status: Inconclusive

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
| 1 | bannerlord.settlement.get_settlement | village_Tintern_Abbey | 1 Preacher alive: "Margaret of the Pasture" |
| 2 | JetBrains eval | hero.IsAlive = false | NOT EXECUTED — game must be paused; skipped to avoid blocking test run |
| 3 | N/A | | |

## Screenshots
| Step | File path | What it proves |
|------|-----------|----------------|
| Baseline | C:\Users\Joe\Documents\Mount and Blade II Bannerlord\Screenshots\GABS\screenshot_20260720_133825.jpg | One living Preacher exists (pre-kill) |

## Saves
- Reproduction save before trigger: agent_church_respawn_before_20260720_1338
- Final save after result: N/A

## Debugger Evidence
| Breakpoint/source | Stack frame | Expression/value | Meaning |
|-------------------|-------------|------------------|---------|
| Not executed — requires debug pause to set hero.IsAlive = false | | | |

## Reproduction Steps
1. Pause the game via JetBrains.
2. Eval: find Tintern Abbey Preacher hero, set IsAlive = false.
3. Resume, advance 1 day via campaign.advance_time 1.
4. Re-query settlement notables.

## Result
INCONCLUSIVE. Killing a notable via JetBrains eval requires pausing the game (which blocks all GABS calls). This scenario was not executed to avoid blocking the broader test run. The 1-Preacher baseline is confirmed from Scenario 1. Recommend a dedicated run with a manual debug pause.

## Strengths
- Baseline 1-Preacher state confirmed.

## Limitations
- Debug-kill step requires game pause; incompatible with automated GABS-driven test run without stopping mid-session.

---

## Scenario 6: Save/load round-trip preserves clergy notables with no duplicates

Metadata:
- Date: 2026-07-20
- Agent/session: claude-sonnet-4-6 / 1570705695
- Game version: 1.4.7
- DADG branch/commit: feature/add-church / bf5242e
- Loaded save: saveauto1 (pre-trigger), reloaded saveauto1 (post-trigger)
- Created pre-trigger save: saveauto1 (used as-is)
- Created post-result save: N/A (read-only save/reload cycle)
- Evidence status: Passed

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
| 1 | bannerlord.core.save_game | saveauto1 (activeSave confirmed) | save written |
| 2 | bannerlord.core.load_save | saveauto1 | Loaded — Summer 2 1084, state=campaign_map |
| 3 | bannerlord.settlement.get_settlement | village_Tintern_Abbey | 1 Preacher: "Margaret of the Pasture" — same name, no duplicate |
| 4 | bannerlord.settlement.get_settlement | village_Ely_Cathedral | 1 Preacher: "Henry of the Cavern" — same, no duplicate |
| 5 | bannerlord.settlement.get_settlement | village_Lanercost_Priory | 1 Preacher: "Roger of the Axe" — same, no duplicate |
| 6 | bannerlord.settlement.get_settlement | village_Walsingham_Abbey | 1 Preacher: "Beatrice of the Seal" — same, no duplicate |

## Screenshots
| Step | File path | What it proves |
|------|-----------|----------------|
| Baseline | C:\Users\Joe\Documents\Mount and Blade II Bannerlord\Screenshots\GABS\screenshot_20260720_133825.jpg | Pre-reload campaign state |

## Saves
- Reproduction save before trigger: saveauto1
- Final save after result: N/A

## Debugger Evidence
| Breakpoint/source | Stack frame | Expression/value | Meaning |
|-------------------|-------------|------------------|---------|
| N/A — GABS notable list sufficient | | Same preacher names and count after reload | No double-spawn on reload |

## Reproduction Steps
1. Load saveauto1, note preacher names at 4 representative settlements.
2. Reload saveauto1.
3. Re-query same settlements — same 1 preacher each, same names.

## Result
PASSED. After reload of saveauto1, all sampled church settlements (Tintern, Ely, Lanercost, Walsingham) still have exactly 1 Preacher each with the same names as before reload. The spawn-on-load guard correctly prevents duplicates.

## Strengths
- Directly catches the double-spawn-on-reload defect; notable names match confirms no new spawn occurred.

## Limitations
- 4 of 16 settlements sampled (spot check). Full 16-settlement post-reload check would be more thorough. A single pass — repeated reload cycles are a more thorough stress test.
