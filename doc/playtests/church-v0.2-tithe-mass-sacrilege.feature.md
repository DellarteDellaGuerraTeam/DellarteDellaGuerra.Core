# Feature: Church v0.2 — Sunday Mass, Weekly Tithe, and Sacrilege

Tags: @bannerlord @gabs @dadg @church @v0.2 @mass @tithe @sacrilege

---

## Scenario 1: "Attend mass" option is visible at a church village and hidden at a non-church village

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
| 1 | (travel to village_Tintern_Abbey) | | <fill on run> |
| 2 | bannerlord.menu.get_current | {} | <fill on run — see Attend mass> |
| 3 | (travel to non-church village) | | <fill on run> |
| 4 | bannerlord.menu.get_current | {} | <fill on run — no Attend mass> |

## Screenshots
| Step | File path | What it proves |
|------|-----------|----------------|
| Church village menu | <fill on run> | "Attend mass" present |
| Non-church village menu | <fill on run> | "Attend mass" absent |

## Saves
- Reproduction save before trigger: <pre-trigger save>
- Final save after result: N/A

## Debugger Evidence
| Breakpoint/source | Stack frame | Expression/value | Meaning |
|-------------------|-------------|------------------|---------|

## Reproduction Steps
1. Travel to village_Tintern_Abbey, open village menu.
2. Screenshot or read menu options.
3. Travel to an ordinary village, repeat.

## Result
<fill on run>

## Strengths
- Confirms visibility is gated by IsChurchSettlement, not globally added.

## Limitations
- Relies on menu option list from GABS; option text may vary if localization keys don't resolve.

---

## Scenario 2: Mass is enabled on Sunday (day-of-week 0) and disabled on other days with countdown tooltip

Metadata:
- Date: <fill on run>
- Agent/session: <fill on run>
- Game version: 1.4.7
- DADG branch/commit: church-v0.1-monasteries / <fill on run>
- Loaded save: <campaign save at village_Tintern_Abbey — non-Sunday>
- Created pre-trigger save: agent_church_mass_sunday_before_<timestamp>
- Created post-result save: agent_church_mass_sunday_after_<timestamp>
- Evidence status: Not run

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
| 1 | JetBrains eval | CampaignTime.Now.GetDayOfWeek | <fill on run> |
| 2 | bannerlord.menu.get_current | {} | <fill on run — disabled with tooltip> |
| 3 | bannerlord.core.run_command | {"command": "campaign.advance_time N"} | <fill on run> |
| 4 | bannerlord.menu.get_current | {} | <fill on run — enabled> |

## Screenshots
| Step | File path | What it proves |
|------|-----------|----------------|
| Weekday menu | <fill on run> | Option greyed with countdown tooltip |
| Sunday menu | <fill on run> | Option enabled |

## Saves
- Reproduction save before trigger: agent_church_mass_sunday_before_<timestamp>
- Final save after result: agent_church_mass_sunday_after_<timestamp>

## Debugger Evidence
| Breakpoint/source | Stack frame | Expression/value | Meaning |
|-------------------|-------------|------------------|---------|
| ChurchMassCampaignBehavior.CanAttendMass | local isSunday | value | Day-of-week check |

## Reproduction Steps
1. Confirm not Sunday via JetBrains eval.
2. Open village menu at church settlement — option greyed.
3. Advance to Sunday — option enabled.

## Result
<fill on run>

## Strengths
- Tests the day-of-week gate and countdown tooltip text.

## Limitations
- campaign.advance_time granularity may overshoot Sunday if days-per-week differ in fast-forward mode.

---

## Scenario 3: Attending mass grants +4 party morale, +1 relation with resident abbot, and locks attendance for the day

Metadata:
- Date: <fill on run>
- Agent/session: <fill on run>
- Game version: 1.4.7
- DADG branch/commit: church-v0.1-monasteries / <fill on run>
- Loaded save: <campaign save at village_Tintern_Abbey on Sunday>
- Created pre-trigger save: agent_church_mass_effect_before_<timestamp>
- Created post-result save: agent_church_mass_effect_after_<timestamp>
- Evidence status: Not run

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
<fill on run>

## Strengths
- Tests all three mass effects and the once-per-Sunday lock.

## Limitations
- MassMorale default is 4 but is configurable; confirm from dadg.config.xml before the run.

---

## Scenario 4: Weekly tithe increases every living church abbot's Power by 2 per week

Metadata:
- Date: <fill on run>
- Agent/session: <fill on run>
- Game version: 1.4.7
- DADG branch/commit: church-v0.1-monasteries / <fill on run>
- Loaded save: <campaign save>
- Created pre-trigger save: agent_church_tithe_before_<timestamp>
- Created post-result save: agent_church_tithe_after_<timestamp>
- Evidence status: Not run

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
<fill on run>

## Strengths
- Directly validates the tithe accumulation mechanic.

## Limitations
- WeeklyTickEvent cadence is not aligned to player position in week; a second 7-day advance confirms the +2/week rate.

---

## Scenario 5: Donation also grants the abbot +5 Power (v0.1 amendment folded into v0.2)

Metadata:
- Date: <fill on run>
- Agent/session: <fill on run>
- Game version: 1.4.7
- DADG branch/commit: church-v0.1-monasteries / <fill on run>
- Loaded save: <campaign save at village_Tintern_Abbey — donation cooldown elapsed>
- Created pre-trigger save: agent_church_donation_power_before_<timestamp>
- Created post-result save: agent_church_donation_power_after_<timestamp>
- Evidence status: Not run

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
<fill on run>

## Strengths
- Tests the DonationPower config knob (default 5).

## Limitations
- DonationPower is configurable; confirm value in dadg.config.xml before test.

---

## Scenario 6: Player raiding a church village triggers sacrilege (-15 local, -5 others, info message)

Metadata:
- Date: <fill on run>
- Agent/session: <fill on run>
- Game version: 1.4.7
- DADG branch/commit: church-v0.1-monasteries / <fill on run>
- Loaded save: <campaign save — player at war with faction that owns village_Tintern_Abbey>
- Created pre-trigger save: agent_church_sacrilege_before_<timestamp>
- Created post-result save: agent_church_sacrilege_after_<timestamp>
- Evidence status: Not run

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
<fill on run>

## Strengths
- Tests the sacrilege cascade: local -15 plus other-church -5.

## Limitations
- Raiding requires war setup; cheats recommended for fixture speed. AI raid path tested in Scenario 7.

---

## Scenario 7: AI lord raiding a church village also incurs sacrilege on their relations

Metadata:
- Date: <fill on run>
- Agent/session: <fill on run>
- Game version: 1.4.7
- DADG branch/commit: church-v0.1-monasteries / <fill on run>
- Loaded save: <campaign save — enemy AI lord near village_Tintern_Abbey>
- Created pre-trigger save: agent_church_ai_sacrilege_before_<timestamp>
- Created post-result save: agent_church_ai_sacrilege_after_<timestamp>
- Evidence status: Not run

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
<fill on run>

## Strengths
- Validates the AI sacrilege path (separate code from player path).

## Limitations
- Waiting for an organic AI raid is slow; time acceleration is needed.
