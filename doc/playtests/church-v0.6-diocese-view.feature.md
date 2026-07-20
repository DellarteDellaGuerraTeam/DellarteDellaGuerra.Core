# Feature: Church v0.6 — Diocese Structure and the Church Hierarchy View

Tags: @bannerlord @gabs @dadg @church @v0.6 @diocese @hierarchy @ui

---

## Scenario 1: "Survey the Church in England" option appears at church settlements and is absent elsewhere

Metadata:
- Date: <fill on run>
- Agent/session: <fill on run>
- Game version: 1.4.7
- DADG branch/commit: church-v0.1-monasteries / <fill on run>
- Loaded save: <campaign save>
- Created pre-trigger save: <pre-trigger save>
- Created post-result save: N/A
- Evidence status: Not run

```gherkin
Feature: Church v0.6 — Diocese Structure and Church Hierarchy View

  Scenario: Survey option present at church village and absent at non-church village
    Given Bannerlord 1.4.7 is running under JetBrains with GABS connected
    And I loaded save "<campaign save>"
    And the baseline state was verified by "bannerlord.core.get_game_state returning InCampaign"
    And the game is running from a module-root checkout (not a nested worktree — GUI prefab must be present)
      # NOTE: this scenario requires the GUI/Prefabs/Church/ChurchHierarchyScreen.xml to be
      # accessible at <module root>/GUI/Prefabs/Church/. Running from a worktree will result
      # in a blank screen or crash on LoadMovie. Verify deployment before this scenario.
    When I open the village menu at village_Tintern_Abbey
    Then the option "Survey the Church in England" (id: dadg_church_survey_hierarchy) is present
    When I open the village menu at a non-church village
    Then the option "Survey the Church in England" is absent
    And the result was proven by "bannerlord.menu.get_current option lists at both villages + screenshots"
```

## Command Log
| Step | Tool | Arguments | Result |
|------|------|-----------|--------|
| 1 | (travel to village_Tintern_Abbey) | | <fill on run> |
| 2 | bannerlord.menu.get_current | {} | <fill on run — survey option present> |
| 3 | (travel to non-church village) | | <fill on run> |
| 4 | bannerlord.menu.get_current | {} | <fill on run — survey option absent> |

## Screenshots
| Step | File path | What it proves |
|------|-----------|----------------|
| Church village menu | <fill on run> | "Survey the Church in England" visible |
| Non-church village menu | <fill on run> | Option absent |

## Saves
- Reproduction save before trigger: <pre-trigger save>
- Final save after result: N/A

## Debugger Evidence
| Breakpoint/source | Stack frame | Expression/value | Meaning |
|-------------------|-------------|------------------|---------|
| ChurchHierarchyMenuBehavior.CanSurveyChurch | IsChurchSettlement check | true/false | Gating confirmed |
| ChurchHierarchyMenuBehavior.CanSurveyChurch | _hasDioceses | true (config valid) | Diocese data loaded |

## Reproduction Steps
1. Travel to church village — survey option present.
2. Travel to ordinary village — option absent.

## Result
<fill on run>

## Strengths
- Validates both positive and negative visibility conditions for the menu option.

## Limitations
- Module-root deployment required for the screen to open without crashing; confirm before proceeding to Scenario 2.

---

## Scenario 2: The hierarchy screen shows 3 sees with correctly indented member rows and live holder names

Metadata:
- Date: <fill on run>
- Agent/session: <fill on run>
- Game version: 1.4.7
- DADG branch/commit: church-v0.1-monasteries / <fill on run>
- Loaded save: <campaign save — all 16 clergy alive>
- Created pre-trigger save: agent_church_diocese_view_before_<timestamp>
- Created post-result save: N/A
- Evidence status: Not run

```gherkin
  Scenario: Opening the hierarchy screen shows 3 sees with indented members and live bishop/abbot names
    # IMPORTANT: This scenario requires the GUI prefab to be deployed at
    # <module root>/GUI/Prefabs/Church/ChurchHierarchyScreen.xml.
    # Running from a worktree without this file will produce a blank screen or crash.
    # See v0.6 doc §7 (GUI prefab deployment caveat).
    Given Bannerlord 1.4.7 is running under JetBrains with GABS connected
    And I loaded save "<campaign save>"
    And all 16 church settlements have living clergy notables
    And the game is running from a module-root checkout with GUI prefab deployed
    And I saved the game as "agent_church_diocese_view_before_<timestamp>"
    When I open the village menu at any church settlement
    And I select "Survey the Church in England"
    Then the ChurchHierarchyScreen opens (ScreenManager.TopScreen is ChurchHierarchyScreen)
    And the screen displays exactly 3 See-level rows (see_ely, see_llandaff, see_st_asaph)
    And each See row has its diocese name displayed (e.g. "See of Ely")
    And beneath each See the member settlements are listed with indented rows (IndentMargin = 45f per depth level)
    And each row shows the holder line "Bishop <NAME> of Ely Cathedral" / "Abbot <NAME> of Battle Abbey" etc.
    And the cathedral does not appear as a separate child row — the See row itself carries the seat, and its holder line shows the bishop
    And the child row order is: Abbeys before Priories within each diocese
    And the result was proven by "bannerlord.ui.take_screenshot showing 3-see hierarchy with named holders"

    # Diocese membership expected from dadg.church_settlements.xml:
    # See of Ely (4 members): village_Ely_Cathedral (Cathedral), village_Walsingham_Abbey,
    #   village_Battle_Abbey, village_Evesham_Abbey
    # See of Llandaff (5 members): village_Llandaff_Cathedral (Cathedral), village_Tintern_Abbey,
    #   village_Whitland_Abbey, village_Malmesbury_Abbey, village_Buckfast_Abbey
    # See of St Asaph (7 members): village_St_Asaph_Cathedral (Cathedral), village_Hexham_Abbey,
    #   village_Byland_Abbey, village_Rievaulx_Abbey, village_Lanercost_Priory,
    #   village_Lindisfarne_Priory, village_Finchale_Priory
```

## Command Log
| Step | Tool | Arguments | Result |
|------|------|-----------|--------|
| 1 | bannerlord.menu.select_option | {"option": "dadg_church_survey_hierarchy"} | <fill on run> |
| 2 | bannerlord.ui.get_screen | {} | <fill on run — ChurchHierarchyScreen> |
| 3 | bannerlord.ui.take_screenshot | {} | <fill on run — hierarchy view> |
| 4 | (count See rows and member rows) | | <fill on run> |

## Screenshots
| Step | File path | What it proves |
|------|-----------|----------------|
| Hierarchy screen | <fill on run> | 3 sees, indented members, live names |

## Saves
- Reproduction save before trigger: agent_church_diocese_view_before_<timestamp>
- Final save after result: N/A

## Debugger Evidence
| Breakpoint/source | Stack frame | Expression/value | Meaning |
|-------------------|-------------|------------------|---------|
| ChurchHierarchyScreenVM ctor | Nodes.Count | expected total rows (3 see rows + 13 non-cathedral members = 16) | All rows built |
| ChurchNodeVM.BuildHolderLine | cleric != null | true | Live holder resolved |

## Reproduction Steps
1. Confirm module-root deployment.
2. Open hierarchy screen from any church village.
3. Screenshot and count sees and members.
4. Verify holder names match living clergy.

## Result
<fill on run>

## Strengths
- End-to-end test of the v0.6 domain, provider, use case, and UI chain in one scenario.

## Limitations
- GUI prefab deployment is a known risk (v0.6 doc §7); if the screen is blank or crashes, revert to the module-root deployment path. The worktree GUI/ folder does not auto-deploy.

---

## Scenario 3: Hierarchy screen shows "(vacant)" after a clergy notable dies

Metadata:
- Date: <fill on run>
- Agent/session: <fill on run>
- Game version: 1.4.7
- DADG branch/commit: church-v0.1-monasteries / <fill on run>
- Loaded save: <campaign save — all clergy alive>
- Created pre-trigger save: agent_church_vacancy_before_<timestamp>
- Created post-result save: agent_church_vacancy_after_<timestamp>
- Evidence status: Not run

```gherkin
  Scenario: After a clergy notable dies the hierarchy screen shows "(vacant)" for that seat
    Given Bannerlord 1.4.7 is running under JetBrains with GABS connected
    And I loaded save "<campaign save>"
    And all clergy are alive and named in the hierarchy screen
    And I saved the game as "agent_church_vacancy_before_<timestamp>"
    # Kill a clergy notable via JetBrains:
    # Set breakpoint on ChurchCampaignBehavior.SpawnMissingAbbots, then eval
    # settlement.Notables.First(n => n.IsPreacher).IsAlive = false (this marks the hero dead).
    When I kill the preacher notable at village_Tintern_Abbey via JetBrains debugger
    And I open the hierarchy screen again (or close and reopen it — it resolves holders live)
    Then the row for village_Tintern_Abbey shows "(vacant)" in the holder line
    And all other rows still show their live clergy names
    And the result was proven by "screenshot of hierarchy screen with (vacant) row for Tintern Abbey"
    # Note: the daily respawn will eventually fill the vacancy; this tests the moment before respawn.
```

## Command Log
| Step | Tool | Arguments | Result |
|------|------|-----------|--------|
| 1 | JetBrains eval | tinternAbbot.IsAlive = false | <fill on run> |
| 2 | (open hierarchy screen) | bannerlord.menu.select_option {"option":"dadg_church_survey_hierarchy"} | <fill on run> |
| 3 | bannerlord.ui.take_screenshot | {} | <fill on run — vacant row> |

## Screenshots
| Step | File path | What it proves |
|------|-----------|----------------|
| Hierarchy with vacant | <fill on run> | "(vacant)" text in Tintern row |

## Saves
- Reproduction save before trigger: agent_church_vacancy_before_<timestamp>
- Final save after result: agent_church_vacancy_after_<timestamp>

## Debugger Evidence
| Breakpoint/source | Stack frame | Expression/value | Meaning |
|-------------------|-------------|------------------|---------|
| ChurchNodeVM.BuildHolderLine | cleric == null branch | return "(vacant)" | Vacancy detection |

## Reproduction Steps
1. Load save with all clergy alive.
2. Kill Tintern abbot via JetBrains eval.
3. Open hierarchy screen — see "(vacant)" for that row.

## Result
<fill on run>

## Strengths
- Tests the live holder resolution and vacancy fallback in ChurchNodeVM.

## Limitations
- JetBrains kill may not fire all vanilla death events; observe for side effects. The daily respawn may replace the abbot before the screen is opened, so move quickly.

---

## Scenario 4: Esc and the Close/Done button both pop the hierarchy screen cleanly

Metadata:
- Date: <fill on run>
- Agent/session: <fill on run>
- Game version: 1.4.7
- DADG branch/commit: church-v0.1-monasteries / <fill on run>
- Loaded save: <campaign save>
- Created pre-trigger save: <pre-trigger save>
- Created post-result save: N/A
- Evidence status: Not run

```gherkin
  Scenario: Hierarchy screen closes cleanly via Esc key and via the Done/Close button
    Given Bannerlord 1.4.7 is running under JetBrains with GABS connected
    And I loaded save "<campaign save>"
    And the ChurchHierarchyScreen is currently open
    When I press Esc (GenericPanelGameKeyCategory close hotkey)
    Then the screen is popped from ScreenManager
    And the game returns to the village menu or campaign state without freeze or error
    When I reopen the hierarchy screen
    And I click the "Done" or "Close" button on the screen
    Then the screen is popped cleanly again
    And no NullReferenceException or unhandled exception occurs (check JetBrains CLR exception tab)
    And the result was proven by "bannerlord.ui.get_screen showing previous screen after close + screenshot + no CLR exception"
```

## Command Log
| Step | Tool | Arguments | Result |
|------|------|-----------|--------|
| 1 | (open hierarchy screen) | | <fill on run> |
| 2 | (press Esc via GABS or manual) | bannerlord.ui.call_viewmodel_method or key event | <fill on run> |
| 3 | bannerlord.ui.get_screen | {} | <fill on run — previous screen> |
| 4 | (reopen and click Close button) | bannerlord.ui.click_widget {"widget":"close_button"} | <fill on run> |
| 5 | bannerlord.ui.get_screen | {} | <fill on run — previous screen again> |

## Screenshots
| Step | File path | What it proves |
|------|-----------|----------------|
| After Esc close | <fill on run> | Village menu or map visible |
| After button close | <fill on run> | Same |

## Saves
- Reproduction save before trigger: <pre-trigger save>
- Final save after result: N/A

## Debugger Evidence
| Breakpoint/source | Stack frame | Expression/value | Meaning |
|-------------------|-------------|------------------|---------|
| ChurchHierarchyScreen.OnFrameTick | IsHotKeyReleased("Exit") == true → Close() | called | Esc path fires |
| ChurchHierarchyScreenVM.ExecuteClose | ScreenManager.PopScreen | called | Button path fires |

## Reproduction Steps
1. Open hierarchy screen.
2. Press Esc — confirm return to previous state.
3. Reopen screen, click Close — confirm same.

## Result
<fill on run>

## Strengths
- Guards against screen-stack leaks and the blank/frozen state that can occur if PopScreen is not called on both paths.

## Limitations
- GABS click_widget may not reliably target the close button by widget name; try call_viewmodel_method with the close command if click fails.

---

## Scenario 5: Hierarchy screen option is hidden when diocese config is empty or invalid

Metadata:
- Date: <fill on run>
- Agent/session: <fill on run>
- Game version: 1.4.7
- DADG branch/commit: church-v0.1-monasteries / <fill on run>
- Loaded save: <any campaign save>
- Created pre-trigger save: N/A (config mutation test)
- Created post-result save: N/A
- Evidence status: Not run

```gherkin
  Scenario: Survey option is hidden when the diocese config yields no roots (empty or invalid config)
    # IMPORTANT: restore dadg.church_settlements.xml after this test.
    # Edit config: remove all Diocese elements or make the XML malformed,
    # then relaunch the game.
    Given Bannerlord 1.4.7 is running under JetBrains with GABS connected
    And dadg.church_settlements.xml contains no Diocese elements (or is invalid)
    And the game was relaunched after the config edit
    When I open the village menu at a settlement that was previously a church settlement
    Then the "Survey the Church in England" option is absent (BuildChurchMapUseCase returns empty roots)
    And the game log contains a warning about invalid or missing diocese config
    And no crash or exception occurs
    And the result was proven by "bannerlord.menu.get_current showing absent survey option + log warning"
    # After test: restore dadg.church_settlements.xml.
```

## Command Log
| Step | Tool | Arguments | Result |
|------|------|-----------|--------|
| 1 | (edit config to remove Diocese elements before launch) | PowerShell | <fill on run> |
| 2 | (launch and load campaign) | | <fill on run> |
| 3 | bannerlord.menu.get_current | {} at church village | <fill on run — survey option absent> |

## Screenshots
| Step | File path | What it proves |
|------|-----------|----------------|
| Menu without survey option | <fill on run> | Option hidden |

## Saves
- Reproduction save before trigger: N/A
- Final save after result: N/A

## Debugger Evidence
| Breakpoint/source | Stack frame | Expression/value | Meaning |
|-------------------|-------------|------------------|---------|
| ChurchHierarchyMenuBehavior.CanSurveyChurch | _hasDioceses | false | Empty map |
| BuildChurchMapUseCase.Execute | Roots.Count | 0 | No dioceses |

## Reproduction Steps
1. Edit config to remove Diocese elements.
2. Relaunch game.
3. Open village menu at former church village — survey option absent.
4. Restore config.

## Result
<fill on run>

## Strengths
- Validates the config format-break degrade-gracefully contract: empty diocese map hides the option.

## Limitations
- Requires relaunching the game; also note that removing Diocese elements also breaks the nested provider, which will leave the church settlement list empty (all church features inactive). Test in isolation.

---

## Scenario 6: Diocese seat is correctly derived as the single Cathedral in each diocese

Metadata:
- Date: <fill on run>
- Agent/session: <fill on run>
- Game version: 1.4.7
- DADG branch/commit: church-v0.1-monasteries / <fill on run>
- Loaded save: N/A (unit/static test — check via JetBrains on a live session)
- Created pre-trigger save: N/A
- Created post-result save: N/A
- Evidence status: Not run

```gherkin
  Scenario: BuildChurchMapUseCase derives the correct cathedral seat for each see
    # This is best verified via JetBrains eval in a live session or via unit tests.
    # BuildChurchMapUseCaseTests cover this in code; this scenario validates it at runtime.
    Given Bannerlord 1.4.7 is running under JetBrains with GABS connected
    And a campaign is running with the nested dadg.church_settlements.xml loaded
    When I evaluate "BuildChurchMapUseCase.Execute().Roots" in JetBrains
    Then the See of Ely root has SettlementId == "village_Ely_Cathedral"
    And the See of Llandaff root has SettlementId == "village_Llandaff_Cathedral"
    And the See of St Asaph root has SettlementId == "village_St_Asaph_Cathedral"
    And each root's Children list contains all non-cathedral members of its diocese
    And Abbeys appear before Priories in each children list
    And the result was proven by "JetBrains eval of ChurchMap.Roots[n].SettlementId and Children order"
```

## Command Log
| Step | Tool | Arguments | Result |
|------|------|-----------|--------|
| 1 | JetBrains eval | ChurchUiServices.BuildChurchMap.Execute().Roots.Count | <fill on run — 3> |
| 2 | JetBrains eval | Roots[0].SettlementId | <fill on run — village_Ely_Cathedral or similar> |
| 3 | JetBrains eval | Roots[0].Children.Select(c => c.Rank) | <fill on run — Abbeys then Priories> |

## Screenshots
| Step | File path | What it proves |
|------|-----------|----------------|
| JetBrains eval | <fill on run> | Root settlement ids and children order |

## Saves
- Reproduction save before trigger: N/A
- Final save after result: N/A

## Debugger Evidence
| Breakpoint/source | Stack frame | Expression/value | Meaning |
|-------------------|-------------|------------------|---------|
| BuildChurchMapUseCase.Execute | return | Roots[0..2].SettlementId | Seat derivation |
| BuildChurchMapUseCase | Children ordering | Abbey before Priory | Order confirmed |

## Reproduction Steps
1. Load campaign.
2. Evaluate ChurchUiServices.BuildChurchMap.Execute() in JetBrains.
3. Inspect root settlement ids and children order.

## Result
<fill on run>

## Strengths
- Directly validates the use-case output structure at runtime, complementing the unit tests.

## Limitations
- ChurchUiServices may not be accessible without navigating the static locator; use the DI container instance if needed.

---

## Scenario 7: Hierarchy screen cannot be opened twice concurrently (double-push guard)

Metadata:
- Date: <fill on run>
- Agent/session: <fill on run>
- Game version: 1.4.7
- DADG branch/commit: church-v0.1-monasteries / <fill on run>
- Loaded save: <campaign save>
- Created pre-trigger save: <pre-trigger save>
- Created post-result save: N/A
- Evidence status: Not run

```gherkin
  Scenario: Selecting Survey the Church while the screen is already open does not push a second screen
    Given Bannerlord 1.4.7 is running under JetBrains with GABS connected
    And the ChurchHierarchyScreen is currently open
    When I select "Survey the Church in England" again (e.g. via bannerlord.menu.select_option)
    Then ScreenManager.TopScreen is still ChurchHierarchyScreen (not a second instance)
    And no crash or visual glitch occurs
    And the result was proven by "bannerlord.ui.get_screen confirming single instance"
```

## Command Log
| Step | Tool | Arguments | Result |
|------|------|-----------|--------|
| 1 | (open hierarchy screen) | | <fill on run> |
| 2 | bannerlord.menu.select_option | {"option": "dadg_church_survey_hierarchy"} | <fill on run — no-op expected> |
| 3 | bannerlord.ui.get_screen | {} | <fill on run — still ChurchHierarchyScreen, one instance> |

## Screenshots
| Step | File path | What it proves |
|------|-----------|----------------|
| After second open attempt | <fill on run> | Single screen instance |

## Saves
- Reproduction save before trigger: <pre-trigger save>
- Final save after result: N/A

## Debugger Evidence
| Breakpoint/source | Stack frame | Expression/value | Meaning |
|-------------------|-------------|------------------|---------|
| ChurchHierarchyMenuBehavior.SurveyChurch | TopScreen is ChurchHierarchyScreen | true | Guard check fires |

## Reproduction Steps
1. Open hierarchy screen.
2. Trigger the survey option again.
3. Confirm ScreenManager still has only one hierarchy screen.

## Result
<fill on run>

## Strengths
- Tests the `ScreenManager.TopScreen is not ChurchHierarchyScreen` guard in SurveyChurch.

## Limitations
- The menu option may not be accessible while the screen is open; may need GABS call_viewmodel_method to simulate a second open trigger.
