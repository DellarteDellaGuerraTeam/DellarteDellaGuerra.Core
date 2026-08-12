# Same-kingdom private-war siege defenders — runtime fixtures.
#
# These are NOT executed by an automated runner (Bannerlord has no in-process BDD harness). They are
# Gherkin documentation of the manual / MCP-driven playtests that verify who defends a fortification
# in a same-kingdom private-war siege. Companions:
#   - doc/feudal-private-wars-design.md  §4.1 (defender-population consolidation status note), §15 #3
#   - doc/feudal-private-wars-manual-test-guide.md  (console-command playbook)
#
# Steps written as `# eval:` are evaluated in the JetBrains debugger (evaluate_expression) against the
# live campaign; plain When/Then steps are dev-console (`Alt + ~`) commands or on-screen observations.
# Clan ids look like `clan_york`; settlement ids like `dadg_Pontefract_castle`.
#
# The shared defender rule is PrivateWarSiegeDefenderPolicy (MAIN layer), reached through three engine
# seams (GetDefenderPartiesOfSettlement, GetNextDefenderPartyOfSettlement, CanPartyJoinSide). Sally-out
# decisions are composed by DadgSallyOutCampaignBehavior through PrivateWarSallyOutPolicy.
#
# 2026-08-11 RE-VERIFICATION: every scenario's prior "Verified PASS" annotation predates the
# PrivateWars.Domain API flattening (commit 923c13b) and was treated as unverified going into this
# pass. All five scenarios were ultimately live re-verified this session: two got strong,
# dual-direction re-verification (the SallyOut/militia-exclusion pair); one got partial
# re-verification (functional membership confirmed, but the overlay panel UI itself was not
# visually re-inspected); two AI-vs-AI scenarios (populate-defenders, capture-on-prep-complete) were
# initially BLOCKED by a mid-session soft-lock, then completed after the coordinator cleared the
# soft-lock via an OS-level input-injection technique (see the corrected tooling-gap entry in the
# re-verification report — the earlier "zombie dialog" framing was wrong; the dialog was ordinary and
# dismissable, just unreachable through `ui.get_inquiry`/`ui.click_widget`/`ui.get_screen`, which are
# blind to global Gauntlet layers). See the scenario-level comments below for specifics, and the
# agent's command log for the full evidence trail (fixture commands, real ids/counts, screenshots).

Feature: Same-kingdom private-war siege defender population
  The garrison, militia, and feud-belligerent lord parties of a besieged fortification must defend it
  against a same-kingdom private-war besieger, even though both sides share a MapFaction (so the
  vanilla IsAtWarWith gate is always false). The rule is player-agnostic: it keys on the besieger clan
  vs the settlement owner, not on the player.

  Background:
    Given the `privatewartest` save is loaded
    And cheat mode is enabled with `config.cheat_mode 1`
    And the dev console is open
    # RE-VERIFICATION NOTE (2026-08-11): `privatewartest` (and every other pre-existing save from
    # before this date, incl. `pw_v13_fixture`) is STALE as of the PrivateWars.Domain API flattening
    # (commit 923c13b) and must not be trusted. Saving was ALSO completely broken in this session
    # (ButterLib `MBObjectExtensionDataStore`/`ConcurrentDictionary` SaveableTypeDefiner gap — see
    # Bug #1 in the 2026-08-11 re-verification report; every `save_game` call failed with
    # "Save Failed! Cannot create save data.", reproduced 4x), so no new fixture save could be
    # created either. All scenarios below marked "Verified PASS 2026-08-11" were instead reproduced
    # from a brand-new DADGNewGame character-creation run (clan Bassett, leader Thomas, Summer 1
    # 1471) followed by the exact console-command sequence recorded in the session's command log
    # (`campaign.change_kingdom player_faction vlandia`, `campaign.declare_private_war
    # player_faction clan_york`, `campaign.create_siege ...`, etc. — prefer `create_siege` over
    # `force_besiege`, which only issues a movement order and does not reliably create a real
    # SiegeEvent). Until saving is fixed, treat the Background's `privatewartest` load as aspirational
    # documentation of intent, not a reproducible step.

  # PARTIAL PASS 2026-08-11 — functional defender membership re-confirmed against the flattened API
  # (see command log), but the siege-overlay PANEL UI itself (GetSiegeEventSide/bucketing) was NOT
  # visually re-inspected this pass — only battle.get_state.enemyTroopCount and
  # settlement.get_settlement.partiesPresent were checked (both showed garrison AND militia present
  # and consumed as defenders). Re-run with a screenshot of the siege-strategies overlay to fully
  # close this scenario. Fresh-campaign figures (player_faction/Bassett joined vlandia=House of York,
  # same-kingdom feud vs clan_york, no WARNING from declare_private_war => confirmed same-MapFaction):
  # `dadg_Pontefract_castle` garrison started at 115 (not 137 — stale save number), militia ~164
  # pre-battle, growing to 267 after the garrison was fully depleted by the Scenario "militia
  # excluded" sally-out fight below (same fixture, reused across scenarios in this pass).
  Scenario: Player besieging a same-kingdom rival sees garrison and militia on the defender panel
    Given the player clan `player_faction` (Braxton) shares a kingdom with `clan_york`
    And `clan_york` owns `dadg_Pontefract_castle` with a 137-troop garrison and a militia
    When I run `campaign.declare_private_war player_faction clan_york dadg_Pontefract_castle`
    And the player party besieges `dadg_Pontefract_castle`
    Then the siege overlay buckets the garrison party onto the Defender panel
    And the siege overlay buckets the militia party onto the Defender panel
    And neither party appears on the player's (Attacker/besieger) panel
    # eval: Pontefract.SiegeEvent.GetSiegeEventSide(Defender).HasInvolvedPartyForEventType(garrison) == true
    # eval: Pontefract.SiegeEvent.GetSiegeEventSide(Attacker).HasInvolvedPartyForEventType(garrison) == false

  # Verified PASS 2026-08-11 — DadgSallyOutCampaignBehavior/PrivateWarSallyOutPolicy re-confirmed
  # runtime-live against the flattened API. Real fixture: `player_faction` (151-troop party, not
  # 1-troop — a 1-troop besieger could not be reproduced without a working save this session) besieged
  # `dadg_Pontefract_castle` (`clan_york`, garrison 115, militia ~164). After `create_siege` +
  # `complete_siege_prep`, the campaign-map menu changed to `encounter` with text "Garrison of
  # Pontefract Castle has sallied out to attack you!" — direct proof the AI chose to sally out
  # against the manufactured same-kingdom private-war enemy. Selecting `attack` started a FIELD
  # battle (`dadg_field_02`, not the siege scene) with `enemyTroopCount:115` — garrison-only (see the
  # next scenario for the militia-exclusion proof this number also establishes).
  Scenario: A weak besieger triggers a garrison sally-out
    Given `player_faction` (1 troop) is besieging `dadg_Pontefract_castle` owned by `clan_york`
    And the garrison plus militia far outpower the besieger
    When the sally-out strength check runs
    Then `DadgSallyOutCampaignBehavior` classifies the sides through `PrivateWarSallyOutPolicy`
    And the garrison sallies out (296 defenders vs 1 besieger), no crash
    # eval: PrivateWarSiegeDefenderPolicy.AreEnemies(player_faction, clan_york) == true
    # historical eval: sally side ≈ 324.99 ; besieger ≈ 0.797 ; garrisonWouldSally == true
    # eval: Pontefract.SiegeEvent.CanPartyJoinSide(garrison.Party, Defender) == true

  # Verified PASS (2026-06-24); RE-VERIFIED PASS 2026-08-11 against the flattened PrivateWars.Domain
  # API — this is the most decisive scenario in the file. Same `player_faction` vs `clan_york` /
  # `dadg_Pontefract_castle` fixture as above, tested both directions on the SAME settlement in one
  # continuous session:
  #   - SallyOut (garrison-initiated field battle, `dadg_field_02`): `battle.get_state.enemyTroopCount
  #     == 115`, an EXACT match for the garrison count alone. The 164-troop militia did NOT join.
  #   - Siege (player-initiated assault after the garrison was fully depleted to 0 by the sally-out
  #     fight, militia regrown to 267 over the elapsed days): `battle.get_state.enemyTroopCount == 267`,
  #     an EXACT match for the militia count alone — this time INCLUDED as expected for a non-SallyOut
  #     BattleType.
  # These two exact-match troop counts, captured back-to-back on the same settlement in the same
  # session, are dual-direction runtime proof of both branches of `IsDefender`'s SallyOut-vs-Siege
  # militia gate — read directly from `PrivateWarSiegeDefenderPolicy.cs` (current, flattened API):
  #   if (candidate.IsMilitia && mapEventType == MapEvent.BattleTypes.SallyOut) return false;
  # `IsDefender`'s signature (`MobileParty?, Clan? besiegerClan, Settlement? besieged,
  # MapEvent.BattleTypes mapEventType`) and `AreEnemies(Clan? a, Clan? b)` are UNCHANGED by the
  # flattening — all three `# eval:` lines below still compile as written.
  Scenario: Militia are excluded from the sally-out defender set, matching vanilla
    Given `player_faction` is besieging `dadg_Pontefract_castle` owned by `clan_york`
    When the defender parties for a SallyOut are enumerated
    Then the militia party is NOT included (it holds the walls but does not sally)
    But the garrison party and any feud-belligerent lord parties ARE included and sally normally
    And this matches vanilla `Town.GetDefenderParties` (`!IsMilitia || battleType != SallyOut`)
    And the wall-assault path (`CanPartyJoinSide`, a Siege battle) still admits militia as defenders
    # eval: PrivateWarSiegeDefenderPolicy.IsDefender(militia, besiegerClan, settlement, SallyOut) == false  [militia "Militia of Pontefract Castle", IsMilitia=true]
    # eval: PrivateWarSiegeDefenderPolicy.IsDefender(settlement.Town.GarrisonParty, besiegerClan, settlement, SallyOut) == true  [GarrisonParty.IsGarrison=true]
    # eval: PrivateWarSiegeDefenderPolicy.IsDefender(militia, besiegerClan, settlement, Siege) == true  [militia still defends the wall assault]
    # impl: PrivateWarSiegeDefenderPolicy.IsDefender returns false for militia when mapEventType == SallyOut

  # Verified PASS 2026-08-11 — re-verified live, AI-vs-AI, entirely from the campaign map (no player
  # mission entered; the earlier session's soft-lock was later cleared by an OS-level input-injection
  # technique — see the corrected Bug #3/#4 entry in the re-verification report — and this campaign
  # continued from that same surviving 1471 save state, not a reload). `clan_hastings` and `clan_howard`
  # both confirmed to exist with those exact ids in the fresh campaign (grep of
  # `DellarteDellaGuerraMap/ModuleData/clans/dadg_clans.xml`, both `super_faction="Kingdom.vlandia"`,
  # i.e. House of York in DADG's display naming — same MapFaction). Real fixture figures (502/407 in
  # the prior annotation were stale save numbers): `Norwich_town` owned by `clan_howard`
  # (Lord John Howard), garrisonCount 424, militia ~429.3. `campaign.declare_private_war clan_hastings
  # clan_howard Norwich_town` returned no WARNING, confirming same-MapFaction. `campaign.create_siege
  # clan_hastings Norwich_town` (not `force_besiege` — Bug #2) returned "Created siege event on
  # 'Norwich_town' (besieger: clan_hastings)"; `settlement.get_settlement Norwich_town` immediately
  # afterward showed `isUnderSiege:true` and `partiesPresent:["Garrison of · Norwich ·","Militia of
  # · Norwich ·"]` — both defender parties still stationed at the settlement, not dispersed, with no
  # player mission involved at any point. A JetBrains `evaluate_expression` for
  # `GetDefenderPartiesOfSettlement` was attempted via `pause_execution` (no player mission was ever
  # entered, so no breakpoint was needed) but failed with `CORDBG_E_ILLEGAL_AT_GC_UNSAFE_POINT` —
  # pausing an arbitrary running frame and evaluating an allocating LINQ expression is not reliable;
  # see the new tooling note in the re-verification report. `AreEnemies`/`CanPartyJoinSide` therefore
  # remain state-tool-confirmed (isUnderSiege + partiesPresent), not debugger-confirmed.
  Scenario: An AI besieger populates the defender side against a same-kingdom rival
    Given `clan_hastings` and `clan_howard` share a kingdom (House of York / vlandia)
    And `clan_howard` owns `Norwich_town` with a 424-troop garrison and a ~429-troop militia
    And `clan_hastings` (an AI clan, not the player) is the registered private-war besieger
    When I run `campaign.declare_private_war clan_hastings clan_howard Norwich_town` (no WARNING — same
    MapFaction) and `campaign.create_siege clan_hastings Norwich_town` (not `force_besiege`)
    Then the garrison and militia are admitted as defenders even with no player involved
    # eval: PrivateWarSiegeDefenderPolicy.AreEnemies(clan_hastings, clan_howard) == true  [not executed — GC-unsafe-point eval failure; state-tool-confirmed instead via isUnderSiege+partiesPresent]
    # eval: EncounterModel.GetDefenderPartiesOfSettlement(Norwich, Siege) contains garrison(424) + militia(429)  [not executed, same reason]
    # eval: Norwich.SiegeEvent.CanPartyJoinSide(garrison.Party, Defender) == true  [not executed, same reason]

  # Verified PASS 2026-08-11 — re-verified live, AI-vs-AI, same fixture as above, entirely from the
  # campaign map. `campaign.complete_siege_prep Norwich_town` returned "Set Norwich_town siege
  # preparation to complete (besieger clan_hastings). Advance ~1 hour; the capture trigger fires on the
  # next hourly tick." Time was advanced via `core.set_time_speed {speed:4}` from Summer 14 hour 13 to
  # Summer 15 hour 19 (well past the 1-hour tick). `settlement.get_settlement Norwich_town` afterward
  # showed `ownerClan:"Hastings"`, `owner:"Lord William Hastings"`, `isUnderSiege:false`,
  # `garrisonCount:2` (post-capture reset), `governor:null` — a real, engine-driven ownership transfer.
  # `campaign.list_private_wars` confirmed the war stayed Active: `clan_hastings vs clan_howard
  # goal=Norwich_town score=51.8 battle=0 status=Active` — exactly matching the "capture transfers the
  # fief; score still drives resolution" design claim. No debugger eval needed; state-tool evidence
  # (owner/ownerClan/isUnderSiege/list_private_wars) is direct and unambiguous here.
  Scenario: An AI same-kingdom siege resolves by preparation-complete capture
    Given `clan_hastings` is besieging `clan_howard`'s `Norwich_town`
    When I run `campaign.complete_siege_prep Norwich_town`
    And `TryCaptureCompletedSieges` runs (confirmed indirectly: ownership transferred on the next hourly tick)
    Then `Norwich_town` ownership transfers from `clan_howard` to `clan_hastings`
    And the SiegeEvent is cleaned up, no crash
    And the private war stays Active (capture transfers the fief; score still drives resolution at ±100)
    # eval: Norwich_town.OwnerClan == clan_hastings (was clan_howard) after TryCaptureCompletedSieges()  [confirmed via settlement.get_settlement: ownerClan "Hastings", was "Howard"]
    # eval: Norwich_town.SiegeEvent == null  [confirmed via settlement.get_settlement: isUnderSiege:false]
