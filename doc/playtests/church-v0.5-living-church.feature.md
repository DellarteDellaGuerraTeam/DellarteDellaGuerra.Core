# Feature: Church v0.5 — The Living Church (Scene Abbots, Pilgrim Parties, Favour Hooks)

Tags: @bannerlord @gabs @dadg @church @v0.5 @pilgrims @scene @abbots

---

## Scenario 1: Clergy notable walks the village scene in monk/civilian garb

Metadata:
- Date: <fill on run>
- Agent/session: <fill on run>
- Game version: 1.4.7
- DADG branch/commit: church-v0.1-monasteries / <fill on run>
- Loaded save: <campaign save at village_Tintern_Abbey>
- Created pre-trigger save: <pre-trigger save>
- Created post-result save: N/A
- Evidence status: Not run

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
| 1 | (enter village_Tintern_Abbey scene) | | <fill on run> |
| 2 | bannerlord.ui.take_screenshot | {} | <fill on run — monk NPC visible> |

## Screenshots
| Step | File path | What it proves |
|------|-----------|----------------|
| Abbot in scene | <fill on run> | Monk equipment on notable NPC |

## Saves
- Reproduction save before trigger: <pre-trigger save>
- Final save after result: N/A

## Debugger Evidence
| Breakpoint/source | Stack frame | Expression/value | Meaning |
|-------------------|-------------|------------------|---------|
| HeroAgentSpawnCampaignBehavior | spawn call | agent equipment | Civilian equipment set applied |

## Reproduction Steps
1. Travel to church village.
2. Enter the village scene.
3. Screenshot the abbot NPC appearance.

## Result
<fill on run>

## Strengths
- Validates the map-module XML monk garb change.

## Limitations
- Screenshot is the primary proof; equipment identification requires visual inspection. NRE risk from preacher_notary is mitigated but should be confirmed (no crash on scene entry).

---

## Scenario 2: Pilgrim bands spawn over several in-game days (up to MaxPilgrimParties = 3)

Metadata:
- Date: <fill on run>
- Agent/session: <fill on run>
- Game version: 1.4.7
- DADG branch/commit: church-v0.1-monasteries / <fill on run>
- Loaded save: <campaign save — zero active pilgrim parties>
- Created pre-trigger save: agent_church_pilgrims_spawn_before_<timestamp>
- Created post-result save: agent_church_pilgrims_spawn_after_<timestamp>
- Evidence status: Not run

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
| 1 | JetBrains eval | _pilgrimHomes.Count | <fill on run — 0> |
| 2 | bannerlord.core.run_command | {"command": "campaign.advance_time 5"} | <fill on run> |
| 3 | JetBrains eval | _pilgrimHomes.Count | <fill on run — 1 to 3> |
| 4 | JetBrains eval | _pilgrimHomes.Keys (party names) | <fill on run> |
| 5 | (advance until cap reached) | | <fill on run> |
| 6 | JetBrains eval | _pilgrimHomes.Count after more days | <fill on run — stays <=3> |

## Screenshots
| Step | File path | What it proves |
|------|-----------|----------------|
| Pilgrim party on map | <fill on run> | Named "Pilgrims of <Settlement>" on campaign map |
| Cap enforced | <fill on run> | Count stays at 3 |

## Saves
- Reproduction save before trigger: agent_church_pilgrims_spawn_before_<timestamp>
- Final save after result: agent_church_pilgrims_spawn_after_<timestamp>

## Debugger Evidence
| Breakpoint/source | Stack frame | Expression/value | Meaning |
|-------------------|-------------|------------------|---------|
| PilgrimageCampaignBehavior.SpawnPilgrimBands | PilgrimagePolicy.ShouldSpawn call | activeParties >= maxParties | Cap check |
| PilgrimageCampaignBehavior.SpawnPilgrimBand | after CreateCustomParty | party.Name | Name format verified |

## Reproduction Steps
1. Confirm zero active pilgrim parties.
2. Advance 5 days.
3. Query _pilgrimHomes count and party names.
4. Continue advancing until cap (3) is reached; confirm no further spawns.

## Result
<fill on run>

## Strengths
- Tests spawn probability, cap enforcement, and shrine exclusion from spawning in one run.

## Limitations
- 5% daily chance per settlement means some runs may produce 0 bands in 5 days; advance more days or set a JetBrains breakpoint on SpawnPilgrimBand.

---

## Scenario 3: Pilgrim party travels to Walsingham shrine and then returns home and despawns

Metadata:
- Date: <fill on run>
- Agent/session: <fill on run>
- Game version: 1.4.7
- DADG branch/commit: church-v0.1-monasteries / <fill on run>
- Loaded save: <campaign save with one active pilgrim party heading to Walsingham>
- Created pre-trigger save: agent_church_pilgrims_travel_before_<timestamp>
- Created post-result save: agent_church_pilgrims_travel_after_<timestamp>
- Evidence status: Not run

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
| 1 | JetBrains breakpoint | PilgrimageCampaignBehavior.TickPilgrimParties, shrine-arrival branch | <fill on run> |
| 2 | JetBrains eval at breakpoint | party.CurrentSettlement == shrine | <fill on run — true> |
| 3 | JetBrains: resume, watch TargetSettlement | | <fill on run — now home> |
| 4 | JetBrains breakpoint | Despawn call | <fill on run — party removed> |
| 5 | JetBrains eval | _pilgrimHomes.ContainsKey(party) | <fill on run — false> |

## Screenshots
| Step | File path | What it proves |
|------|-----------|----------------|
| Outbound party on map | <fill on run> | Party en route to Walsingham |
| After despawn | <fill on run> | Party absent from map |

## Saves
- Reproduction save before trigger: agent_church_pilgrims_travel_before_<timestamp>
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
<fill on run>

## Strengths
- Tests the full travel lifecycle: outbound, shrine stay (one tick to leave), homebound, despawn.

## Limitations
- Travel time can be several in-game days; use time acceleration. Breakpoint-driven approach avoids needing to watch the map for hours.

---

## Scenario 4: Save/load mid-journey resumes pilgrim travel correctly

Metadata:
- Date: <fill on run>
- Agent/session: <fill on run>
- Game version: 1.4.7
- DADG branch/commit: church-v0.1-monasteries / <fill on run>
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
- Date: <fill on run>
- Agent/session: <fill on run>
- Game version: 1.4.7
- DADG branch/commit: church-v0.1-monasteries / <fill on run>
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
- Date: <fill on run>
- Agent/session: <fill on run>
- Game version: 1.4.7
- DADG branch/commit: church-v0.1-monasteries / <fill on run>
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
- Date: <fill on run>
- Agent/session: <fill on run>
- Game version: 1.4.7
- DADG branch/commit: church-v0.1-monasteries / <fill on run>
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
- Date: <fill on run>
- Agent/session: <fill on run>
- Game version: 1.4.7
- DADG branch/commit: church-v0.1-monasteries / <fill on run>
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
