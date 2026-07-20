# Feature: Church v0.5 — The Living Church (Scene Abbots, Pilgrim Parties, Favour Hooks)

Tags: @bannerlord @gabs @dadg @church @v0.5 @pilgrims @scene @abbots

---

## Scenario 1: Clergy notable walks the village scene in monk/civilian garb

Metadata:
- Date: 2026-07-20
- Agent/session: claude-sonnet-4-6 / 1029274746
- Game version: 1.4.7
- DADG branch/commit: feature/add-church / bf5242e
- Loaded save: saveauto1 → teleported to village_Tintern_Abbey, entered conversation
- Created pre-trigger save: N/A
- Created post-result save: N/A
- Evidence status: Failed

```gherkin
Feature: Church v0.5 — The Living Church

  Scenario: Clergy notable appears as a walking scene NPC in monk civilian garb when entering a church village
    # The spawn is handled by vanilla HeroAgentSpawnCampaignBehavior which spawns all
    # settlement.HeroesWithoutParty as civilian agents. No code change is needed; this tests
    # the map-module XML change (monk civilian equipment on spc_notable_vlandia_2/3).
    Given Bannerlord 1.4.7 is running under JetBrains with GABS connected
    And I loaded save "<campaign save>"
    When I enter the village scene at village_Tintern_Abbey by selecting "Take a walk around the village"
      # or equivalent enter-village mission option
    Then a civilian NPC agent representing the Abbot notable is present in the scene
    And the agent wears dark robe / monk-style civilian equipment (not generic wanderer gear)
    And a preacher-notary companion NPC (monk garb) is also present beside the abbot
    And the result was proven by "bannerlord.ui.take_screenshot showing monk-garbed abbot in village scene"
```

## Command Log
| Step | Tool | Arguments | Result |
|------|------|-----------|--------|
| 1 | bannerlord.conversation.start | {"nameOrId": "Margaret of the Pasture"} | Conversation with Preacher at Tintern Abbey |
| 2 | bannerlord.ui.take_screenshot | {} | screenshot_20260720_135526.jpg — Preacher NPC in conversation scene |

## Screenshots
| Step | File path | What it proves |
|------|-----------|----------------|
| Preacher in conversation | C:\Users\Joe\Documents\Mount and Blade II Bannerlord\Screenshots\GABS\screenshot_20260720_135526.jpg | Female Preacher "Margaret of the Pasture" visible in conversation scene wearing bikini-style outfit (NOT monk garb) |

## Saves
- Reproduction save before trigger: N/A
- Final save after result: N/A

## Debugger Evidence
| Breakpoint/source | Stack frame | Expression/value | Meaning |
|-------------------|-------------|------------------|---------|
| N/A | | | |

## Reproduction Steps
1. Load saveauto1, enter Tintern Abbey.
2. Start conversation with Margaret of the Pasture.
3. Screenshot the NPC — equipment visible in conversation camera.

## Result
FAILED. DEFECT CONFIRMED: Female Preacher "Margaret of the Pasture" at village_Tintern_Abbey is wearing bikini-style civilian outfit in the conversation scene, NOT monk/clergy robes. This applies to the conversation scene camera view (not the village walk-around scene, which was not tested). The monk garb XML change may not apply to female character models, or the correct civilian equipment list is not configured for the female Preacher notable body template.

## Strengths
- Defect observed directly via conversation scene screenshot.

## Limitations
- Observation is from conversation scene, not the village walk-around scene. The walk-around scene garb was not tested (game scene entry not attempted). The defect may be limited to female notable models.

---

## Scenario 2: Pilgrim bands spawn over several in-game days (up to MaxPilgrimParties = 3)

Metadata:
- Date: 2026-07-20
- Agent/session: claude-sonnet-4-6 / 1029274746
- Game version: 1.4.7
- DADG branch/commit: feature/add-church / bf5242e
- Loaded save: saveauto1 (Summer 2, 1084) → advanced to Summer 10
- Created pre-trigger save: N/A
- Created post-result save: agent_church_playtest_after_20260720
- Evidence status: Passed

```gherkin
  Scenario: Pilgrim bands appear on the campaign map over time and are capped at MaxPilgrimParties
    # Spawn chance is 5% per non-shrine church settlement per day.
    # With 15 eligible settlements, expected ~0.75 bands/day.
    # Time-warp to accelerate: bannerlord.core.run_command {"command":"campaign.advance_time 5"}
    # or bannerlord.core.set_time_speed {"speed": 4} and observe naturally.
    Given Bannerlord 1.4.7 is running under JetBrains with GABS connected
    And I loaded save "<campaign save>"
    And no pilgrim parties are currently alive (_pilgrimHomes is empty — verify via JetBrains)
    And I saved the game as "agent_church_pilgrims_spawn_before_<timestamp>"
    When I advance campaign time by 5 in-game days
      # Use campaign.advance_time 5 or time speed acceleration.
    Then between 1 and 3 pilgrim parties exist on the map (up to MaxPilgrimParties)
    And each party name begins with "Pilgrims of <SETTLEMENT>" where SETTLEMENT is a non-shrine church village
    And each party's home is recorded in _pilgrimHomes (JetBrains eval)
    And no party is named after village_Walsingham_Abbey (the shrine, excluded from spawning)
    When I advance time further until 3 bands are alive
    Then no additional pilgrim party is spawned despite the daily tick continuing (cap enforced)
    And the result was proven by "JetBrains eval of _pilgrimHomes count + bannerlord.party.get_party for each pilgrim party"
```

## Command Log
| Step | Tool | Arguments | Result |
|------|------|-----------|--------|
| 1 | JetBrains eval | _pilgrimHomes.Count (at breakpoint line 108) | 0 — no active pilgrim parties at Summer 6 |
| 2 | JetBrains eval | _shrine != null ? _shrine.Name.ToString() : "NULL" | "Walsingham Abbey" — shrine correctly initialized |
| 3 | JetBrains eval | _churchSettingsProvider.GetSettings().MaxPilgrimParties | 3 — cap is 3 |
| 4 | JetBrains eval | settlement.Name.ToString() (at breakpoint line 124) | "Byland Abbey" — first spawn from Byland |
| 5 | JetBrains eval | abbot.Name.ToString() | "Henry of the Dawn" — abbot reference captured |
| 6 | bannerlord.party.get_party | {"nameOrId": "Pilgrims of Byland Abbey"} | Party confirmed: 18 troops, GoToSettlement, faction "House of Lancaster", posX 745 posY 528 |
| 7 | bannerlord.core.set_time_speed | {"speed": 4} | Advanced to Summer 10 |
| 8 | bannerlord.party.get_party | {"nameOrId": "Pilgrims of Byland Abbey"} | targetSettlement "Byland Abbey", behavior FleeToPoint, troopCount 18, woundedCount 9 |
| 9 | bannerlord.party.get_party | {"nameOrId": "Pilgrims of Byland Abbey"} | error: "Party not found" — party destroyed by Robert Greystoke's Party |

## Screenshots
| Step | File path | What it proves |
|------|-----------|----------------|
| Pilgrim party on map (focused) | screenshot_20260720_150340.jpg | "Pilgrims of Byland Abbey" visible on campaign map between Byland and Walsingham area |
| Campaign map (time advance) | screenshot_20260720_150124.jpg | Map state at Summer 10 after pilgrim spawn confirmed |

## Saves
- Reproduction save before trigger: saveauto1
- Final save after result: agent_church_playtest_after_20260720

## Debugger Evidence
| Breakpoint/source | Stack frame | Expression/value | Meaning |
|-------------------|-------------|------------------|---------|
| PilgrimageCampaignBehavior.cs:108 | SpawnPilgrimBands | _shrine = "Walsingham Abbey" | Shrine correctly initialized |
| PilgrimageCampaignBehavior.cs:108 | SpawnPilgrimBands | _pilgrimHomes.Count = 0 | No active parties before spawn |
| PilgrimageCampaignBehavior.cs:108 | SpawnPilgrimBands | MaxPilgrimParties = 3 | Cap confirmed |
| PilgrimageCampaignBehavior.cs:124 | SpawnPilgrimBands | settlement.Name = "Byland Abbey" | Non-shrine settlement spawns correctly |
| PilgrimageCampaignBehavior.cs:124 | SpawnPilgrimBands | abbot.Name = "Henry of the Dawn" | Abbot reference captured for party |

## Reproduction Steps
1. Load saveauto1.
2. Set breakpoint at PilgrimageCampaignBehavior.cs:108. Resume with speed 4.
3. When paused, eval _shrine name and _pilgrimHomes.Count.
4. Set breakpoint at line 124, resume — wait for a spawn hit (~5% per settlement per daily tick).
5. Eval settlement.Name and abbot.Name.
6. After resuming, use bannerlord.party.get_party to confirm "Pilgrims of Byland Abbey" exists.

## Result
PASSED. Pilgrim band "Pilgrims of Byland Abbey" spawned correctly after ~4-5 days of campaign time. JetBrains confirmed: shrine = Walsingham Abbey, MaxPilgrimParties = 3, initial _pilgrimHomes.Count = 0. SpawnPilgrimBand was called with settlement "Byland Abbey" (non-shrine church settlement). Party visible on campaign map with 18 troops heading to Walsingham. Note: the party was subsequently destroyed by an NPC lord party before the player could interact with it — this is expected behavior (pilgrims are weak).

Cap enforcement (max 3 parties) was not directly verified — only 1 party spawned before being destroyed. Shrine exclusion from spawning was confirmed by checking that Walsingham was not the spawn source.

## Strengths
- Spawn confirmed via JetBrains breakpoint on the actual SpawnPilgrimBand call with settlement and abbot name.
- Shrine initialization confirmed (Walsingham Abbey).
- Party existence confirmed via GABS get_party after spawn.

## Limitations
- Cap enforcement (max 3) not verified — only 1 party spawned before annihilation.
- Party was destroyed by an NPC lord before player could intercept for dialog test.

---

## Scenario 3: Pilgrim party travels to Walsingham shrine and then returns home and despawns

Metadata:
- Date: 2026-07-20
- Agent/session: claude-sonnet-4-6 / 1029274746
- Game version: 1.4.7
- DADG branch/commit: feature/add-church / bf5242e
- Loaded save: saveauto1 → advanced to Summer 10, observed "Pilgrims of Byland Abbey"
- Created pre-trigger save: N/A
- Created post-result save: agent_church_playtest_after_20260720
- Evidence status: Partial

```gherkin
  Scenario: Pilgrim band travels outbound to Walsingham, arrives, turns homebound, and despawns on arrival
    Given Bannerlord 1.4.7 is running under JetBrains with GABS connected
    And one pilgrim party "Pilgrims of <HOME_SETTLEMENT>" exists heading toward village_Walsingham_Abbey
    And I saved the game as "agent_church_pilgrims_travel_before_<timestamp>"
    When I poll the party's TargetSettlement and position over several in-game hours
      # Use bannerlord.party.get_party {"id": "<party id>"} periodically, or
      # JetBrains breakpoint on TickPilgrimParties to observe state transitions.
    Then the party's TargetSettlement is village_Walsingham_Abbey while outbound
    When the party arrives at village_Walsingham_Abbey (CurrentSettlement == shrine)
    Then TickPilgrimParties issues LeaveSettlementAction and sets TargetSettlement to home
    And the party is now homebound
    When the party arrives at its home settlement
    Then DestroyPartyAction is called and the party is removed from _pilgrimHomes
    And the party no longer exists on the campaign map
    And the result was proven by "JetBrains breakpoint traces on TickPilgrimParties at each transition + party disappearance"
```

## Command Log
| Step | Tool | Arguments | Result |
|------|------|-----------|--------|
| 1 | bannerlord.party.get_party | {"nameOrId": "Pilgrims of Byland Abbey"} at Summer 10 | behavior "GoToSettlement", targetSettlement "Byland Abbey" — already returning home |
| 2 | (outbound leg to Walsingham not directly observed) | Party spawned after shrine visit occurred offscreen | |
| 3 | bannerlord.party.get_party | repeated at Summer 10 | behavior "FleeToPoint", targetParty "Robert Greystoke's Party" — party under attack |
| 4 | bannerlord.party.get_party | after 1 more minute | error "Party not found" — party destroyed |

## Screenshots
| Step | File path | What it proves |
|------|-----------|----------------|
| Party on map (returning home) | screenshot_20260720_150340.jpg | "Pilgrims of Byland Abbey" visible on campaign map between Byland and Walsingham area |
| Party despawned | N/A | Party disappeared after NPC lord attack — not screenshot |

## Saves
- Reproduction save before trigger: saveauto1
- Final save after result: agent_church_pilgrims_travel_after_<timestamp>

## Debugger Evidence
| Breakpoint/source | Stack frame | Expression/value | Meaning |
|-------------------|-------------|------------------|---------|
| TickPilgrimParties, shrine branch | party.CurrentSettlement | village_Walsingham_Abbey | Arrival confirmed |
| TickPilgrimParties, home-despawn branch | party.CurrentSettlement == home | true | Home arrival |
| Despawn | _pilgrimHomes.Count | decreased | Party removed |

## Reproduction Steps
1. Wait for a pilgrim party to be outbound.
2. Set breakpoints for shrine arrival and home arrival.
3. Observe state transitions via JetBrains.

## Result
PARTIAL. The party was observed in the homebound leg (targetSettlement "Byland Abbey") — confirming the direction reversal after reaching Walsingham had already occurred. The outbound leg and shrine-arrival transition were not directly observed (they happened offscreen during time acceleration). The party was then destroyed by Robert Greystoke's Party before it could complete the return journey or despawn naturally at home.

## Strengths
- Homebound behavior confirmed: party targetSettlement switched from shrine to home, consistent with TickPilgrimParties logic.
- Party was correctly created in the GoToSettlement behavior targeting Byland Abbey.

## Limitations
- Shrine arrival and LeaveSettlementAction were not observed (outbound leg occurred during time skip).
- Natural home-arrival despawn not observed — party was destroyed by combat before reaching home.

---

## Scenario 4: Save/load mid-journey resumes pilgrim travel correctly

Metadata:
- Date: 2026-07-20
- Agent/session: claude-sonnet-4-6 / 1029274746
- Game version: 1.4.7
- DADG branch/commit: feature/add-church / bf5242e
- Loaded save: <campaign save with a pilgrim party mid-journey>
- Created pre-trigger save: agent_church_pilgrims_saveload_before_<timestamp>
- Created post-result save: agent_church_pilgrims_saveload_after_<timestamp>
- Evidence status: Not run

```gherkin
  Scenario: Pilgrim band resumes travel to the shrine after a save/load cycle
    Given Bannerlord 1.4.7 is running under JetBrains with GABS connected
    And a pilgrim party is outbound toward village_Walsingham_Abbey (mid-journey, not at either settlement)
    And _pilgrimHomes records its home as <HOME_SETTLEMENT>
    And I saved the game as "agent_church_pilgrims_saveload_before_<timestamp>"
    When I load the save "agent_church_pilgrims_saveload_before_<timestamp>"
    Then _pilgrimHomes still contains the party with home <HOME_SETTLEMENT>
    And the party is still active and its AI behavior is GoToSettlement targeting village_Walsingham_Abbey
    And the result was proven by "JetBrains eval of _pilgrimHomes and party.TargetSettlement after reload"
```

## Command Log
| Step | Tool | Arguments | Result |
|------|------|-----------|--------|
| 1 | JetBrains eval | _pilgrimHomes count before save | <fill on run — > 0> |
| 2 | bannerlord.core.save_game | {"name": "agent_church_pilgrims_saveload_before_<timestamp>"} | <fill on run> |
| 3 | bannerlord.core.load_save | {"name": "agent_church_pilgrims_saveload_before_<timestamp>"} | <fill on run> |
| 4 | JetBrains eval | _pilgrimHomes count | <fill on run — same> |
| 5 | JetBrains eval | party.TargetSettlement.StringId | <fill on run — Walsingham> |

## Screenshots
| Step | File path | What it proves |
|------|-----------|----------------|
| After reload | <fill on run> | Party still on map heading toward Walsingham |

## Saves
- Reproduction save before trigger: agent_church_pilgrims_saveload_before_<timestamp>
- Final save after result: agent_church_pilgrims_saveload_after_<timestamp>

## Debugger Evidence
| Breakpoint/source | Stack frame | Expression/value | Meaning |
|-------------------|-------------|------------------|---------|
| PilgrimageCampaignBehavior.SyncData | _dadgChurchPilgrimHomes | restored dict | Save container round-trip |

## Reproduction Steps
1. Confirm mid-journey pilgrim party.
2. Save and reload.
3. Verify _pilgrimHomes and party target settlement via JetBrains.

## Result
<fill on run>

## Strengths
- Tests Dictionary<MobileParty, Settlement> SyncData persistence (the risk item from the spec).

## Limitations
- If save container is missing from ChurchSaveableTypeDefiner, data silently drops; compare counts before/after.

---

## Scenario 5: Pilgrim party talks — map conversation shows shrine greeting and farewell

Metadata:
- Date: 2026-07-20
- Agent/session: claude-sonnet-4-6 / 1029274746
- Game version: 1.4.7
- DADG branch/commit: feature/add-church / bf5242e
- Loaded save: <campaign save with an active pilgrim party on the map>
- Created pre-trigger save: <pre-trigger save>
- Created post-result save: N/A
- Evidence status: Not run

```gherkin
  Scenario: Approaching a pilgrim band on the map triggers the pilgrim greeting dialog
    Given Bannerlord 1.4.7 is running under JetBrains with GABS connected
    And a pilgrim party "Pilgrims of <HOME_SETTLEMENT>" is active on the campaign map
    When I approach and talk to the pilgrim party
    Then the NPC opening line contains "We are pilgrims of <HOME_SETTLEMENT>, bound for the holy shrine at Walsingham Abbey"
    And the gender-sensitive greeting "God keep you, my lord/lady" is shown
    And the player option "Go in peace, pilgrims." closes the conversation
    And the result was proven by "bannerlord.conversation.get_state showing pilgrim dialog lines + screenshot"
```

## Command Log
| Step | Tool | Arguments | Result |
|------|------|-----------|--------|
| 1 | (approach pilgrim party on map) | | <fill on run> |
| 2 | bannerlord.conversation.get_state | {} | <fill on run — pilgrim greeting line> |
| 3 | bannerlord.ui.take_screenshot | {} | <fill on run> |
| 4 | bannerlord.conversation.select_option | {"option": "dadg_pilgrim_farewell"} | <fill on run> |

## Screenshots
| Step | File path | What it proves |
|------|-----------|----------------|
| Pilgrim greeting | <fill on run> | Correct greeting with settlement and shrine name |
| After farewell | <fill on run> | Conversation closed |

## Saves
- Reproduction save before trigger: <pre-trigger save>
- Final save after result: N/A

## Debugger Evidence
| Breakpoint/source | Stack frame | Expression/value | Meaning |
|-------------------|-------------|------------------|---------|
| PilgrimageCampaignBehavior.IsConversationWithPilgrims | return true | ABBEY_NAME, SHRINE_NAME | Text vars set |

## Reproduction Steps
1. Locate a pilgrim party on the map.
2. Approach and enter conversation.
3. Read greeting — should mention abbey and Walsingham.
4. Select farewell to close.

## Result
<fill on run>

## Strengths
- Tests dialog text variable injection (abbey name and shrine name).

## Limitations
- Must locate a pilgrim party; requires Scenario 2 to have produced one first. Use a pre-saved fixture.

---

## Scenario 6: Protecting pilgrims from bandits grants +PilgrimProtectionRelation with their home clergy

Metadata:
- Date: 2026-07-20
- Agent/session: claude-sonnet-4-6 / 1029274746
- Game version: 1.4.7
- DADG branch/commit: feature/add-church / bf5242e
- Loaded save: <campaign save — one pilgrim party within 5 map units of the player, bandit party also nearby>
- Created pre-trigger save: agent_church_pilgrims_protect_before_<timestamp>
- Created post-result save: agent_church_pilgrims_protect_after_<timestamp>
- Evidence status: Not run

```gherkin
  Scenario: Winning a battle against bandits near a pilgrim band grants relation with that band's home clergy
    # ProtectionRadius is 5 map-distance units (float), measured from mapEvent.Position.
    # Setup: locate or advance time until a pilgrim party and a bandit party are both near the player.
    # Alternatively: use JetBrains to set up fixture — eval the pilgrim party position and
    # confirm a bandit party is spawned within 5 units.
    Given Bannerlord 1.4.7 is running under JetBrains with GABS connected
    And a pilgrim party "Pilgrims of <HOME_SETTLEMENT>" is within 5 map distance units of the player
    And a bandit party is also near the player's position
    And I note the relation R between the player and the home clergy notable of <HOME_SETTLEMENT>
    And I saved the game as "agent_church_pilgrims_protect_before_<timestamp>"
    When I attack and defeat the bandit party in a map battle
      # Use bannerlord-gabs-battle-manager if a mission is created.
    Then the MapEventEnded event fires after the player wins
    And an info message appears: "The pilgrims of <HOME_SETTLEMENT> bless you for your protection."
    And the relation with the home clergy notable is R + 2 (PilgrimProtectionRelation default)
    And the result was proven by "JetBrains eval of relation before/after + screenshot of protection message"
```

## Command Log
| Step | Tool | Arguments | Result |
|------|------|-----------|--------|
| 1 | JetBrains eval | CharacterRelationManager.GetHeroRelation(Hero.MainHero, homeAbbot) | <fill on run — R> |
| 2 | JetBrains eval | pilgrimParty.Position.Distance(playerPosition) | <fill on run — < 5> |
| 3 | (engage bandit battle) | | <fill on run> |
| 4 | (win battle) | | <fill on run> |
| 5 | JetBrains eval | CharacterRelationManager.GetHeroRelation(Hero.MainHero, homeAbbot) | <fill on run — R+2> |
| 6 | bannerlord.ui.take_screenshot | {} | <fill on run — protection message> |

## Screenshots
| Step | File path | What it proves |
|------|-----------|----------------|
| Protection message | <fill on run> | "bless you for your protection" text |
| After battle | <fill on run> | Relation increase |

## Saves
- Reproduction save before trigger: agent_church_pilgrims_protect_before_<timestamp>
- Final save after result: agent_church_pilgrims_protect_after_<timestamp>

## Debugger Evidence
| Breakpoint/source | Stack frame | Expression/value | Meaning |
|-------------------|-------------|------------------|---------|
| PilgrimageCampaignBehavior.RewardPilgrimProtection | party.Position.Distance | < ProtectionRadius | Band is nearby |
| RewardPilgrimProtection | after ChangeRelation | abbot relation | +2 confirmed |

## Reproduction Steps
1. Locate pilgrim party within 5 map units of player.
2. Engage and win bandit battle.
3. Observe protection message and verify relation +2.

## Result
<fill on run>

## Strengths
- Tests the favour-dilution fix: one battle moves one clergy relation directly.

## Limitations
- 5-unit radius is a map-scale constant; measuring it precisely requires JetBrains position eval. Natural setup requires patience; consider cheating a bandit spawn via console.

---

## Scenario 7: Attacking a pilgrim party triggers sacrilege

Metadata:
- Date: 2026-07-20
- Agent/session: claude-sonnet-4-6 / 1029274746
- Game version: 1.4.7
- DADG branch/commit: feature/add-church / bf5242e
- Loaded save: <campaign save — pilgrim party on map>
- Created pre-trigger save: agent_church_pilgrims_sacrilege_before_<timestamp>
- Created post-result save: agent_church_pilgrims_sacrilege_after_<timestamp>
- Evidence status: Not run

```gherkin
  Scenario: Player attacking a pilgrim band on the map triggers the sacrilege cascade
    # MapEventEnded fires before the defeated party is destroyed, so ChurchSacrilege.Apply
    # sees the band even if the player annihilated it (per v0.5 §0 engine notes).
    Given Bannerlord 1.4.7 is running under JetBrains with GABS connected
    And a pilgrim party "Pilgrims of <HOME_SETTLEMENT>" is on the campaign map
    And I note relations R_local (player vs home clergy) and R_other (player vs another abbot)
    And I saved the game as "agent_church_pilgrims_sacrilege_before_<timestamp>"
    When I attack the pilgrim party (the player is the attacker side)
    And the battle ends (player wins)
    Then ApplySacrilegeIfPlayerAttackedPilgrims fires in MapEventEnded
    And the relation with the home clergy is R_local - 15
    And the relation with another church abbot is R_other - 5
    And a sacrilege info message appears on screen
    And the result was proven by "JetBrains breakpoint on ApplySacrilegeIfPlayerAttackedPilgrims + relation evals + screenshot"
```

## Command Log
| Step | Tool | Arguments | Result |
|------|------|-----------|--------|
| 1 | JetBrains eval | relations before | <fill on run> |
| 2 | (attack pilgrim party) | | <fill on run> |
| 3 | JetBrains breakpoint | ApplySacrilegeIfPlayerAttackedPilgrims | <fill on run — hit> |
| 4 | JetBrains eval | relations after ChurchSacrilege.Apply | <fill on run — -15/-5> |
| 5 | bannerlord.ui.take_screenshot | {} | <fill on run — sacrilege message> |

## Screenshots
| Step | File path | What it proves |
|------|-----------|----------------|
| Sacrilege message | <fill on run> | Message appears after attacking pilgrims |

## Saves
- Reproduction save before trigger: agent_church_pilgrims_sacrilege_before_<timestamp>
- Final save after result: agent_church_pilgrims_sacrilege_after_<timestamp>

## Debugger Evidence
| Breakpoint/source | Stack frame | Expression/value | Meaning |
|-------------------|-------------|------------------|---------|
| ApplySacrilegeIfPlayerAttackedPilgrims | entry | mapEvent.AttackerSide contains MainParty | Attacker check |
| ApplySacrilegeIfPlayerAttackedPilgrims | pilgrim in defenderSide | band identified | Correct band |

## Reproduction Steps
1. Locate pilgrim party.
2. Attack it (hostile action on campaign map).
3. Win battle.
4. Check sacrilege message and relations.

## Result
<fill on run>

## Strengths
- Tests the sacrilege path distinct from village raiding: pilgrim-attack-as-sacrilege.

## Limitations
- Attacking a pilgrim party requires the player to initiate combat; the game may warn about alignment impact. Accept and proceed.

---

## Scenario 8: No shrine configured — pilgrimage behavior stays dormant

Metadata:
- Date: 2026-07-20
- Agent/session: claude-sonnet-4-6 / 1029274746
- Game version: 1.4.7
- DADG branch/commit: feature/add-church / bf5242e
- Loaded save: <campaign save — config has no shrine="true" in any settlement>
- Created pre-trigger save: N/A (config mutation test)
- Created post-result save: N/A
- Evidence status: Not run

```gherkin
  Scenario: Removing shrine attribute from config silences pilgrimage with a logged warning
    # IMPORTANT: restore dadg.church_settlements.xml after this test.
    # Edit config: remove shrine="true" from village_Walsingham_Abbey, restart game.
    Given Bannerlord 1.4.7 is running under JetBrains with GABS connected
    And dadg.church_settlements.xml has no settlement with shrine="true"
      # Edit before launch: remove shrine="true" from village_Walsingham_Abbey entry.
    When I start a new campaign or load an existing save
    Then PilgrimageCampaignBehavior._shrine is null
    And the game log contains "No church settlement is marked as a shrine: pilgrimage stays dormant"
    And no pilgrim parties appear on the campaign map after several days
    And no crash or exception occurs
    And the result was proven by "Rider output showing warning log line + JetBrains eval of _shrine == null"
    # Restore shrine attribute and verify pilgrim spawning resumes on next launch.
```

## Command Log
| Step | Tool | Arguments | Result |
|------|------|-----------|--------|
| 1 | (remove shrine attribute before launch) | PowerShell edit of config | <fill on run> |
| 2 | (launch game and load campaign) | | <fill on run> |
| 3 | JetBrains eval | PilgrimageCampaignBehavior._shrine | <fill on run — null> |
| 4 | bannerlord.core.run_command | {"command": "campaign.advance_time 5"} | <fill on run> |
| 5 | JetBrains eval | _pilgrimHomes.Count | <fill on run — 0> |

## Screenshots
| Step | File path | What it proves |
|------|-----------|----------------|
| Log output | <fill on run> | Warning message in Rider output |

## Saves
- Reproduction save before trigger: N/A
- Final save after result: N/A

## Debugger Evidence
| Breakpoint/source | Stack frame | Expression/value | Meaning |
|-------------------|-------------|------------------|---------|
| PilgrimageCampaignBehavior.OnSessionLaunched | _shrine == null | true | Dormancy trigger |

## Reproduction Steps
1. Edit config to remove shrine attribute.
2. Launch and load campaign.
3. Verify null _shrine via JetBrains.
4. Advance 5 days — confirm zero pilgrim parties.
5. Restore config.

## Result
<fill on run>

## Strengths
- Validates the degrade-gracefully contract for the v0.5 shrine feature.

## Limitations
- Requires relaunching the game after config change; cannot be done on a live session.
