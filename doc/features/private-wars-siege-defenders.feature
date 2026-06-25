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
# The single shared rule under test is PrivateWarSiegeDefenderPolicy (MAIN layer), reached through three
# engine seams (GetDefenderPartiesOfSettlement, GetNextDefenderPartyOfSettlement, CanPartyJoinSide) plus
# the SallyOutStrengthPatch strength scan.

Feature: Same-kingdom private-war siege defender population
  The garrison, militia, and feud-belligerent lord parties of a besieged fortification must defend it
  against a same-kingdom private-war besieger, even though both sides share a MapFaction (so the
  vanilla IsAtWarWith gate is always false). The rule is player-agnostic: it keys on the besieger clan
  vs the settlement owner, not on the player.

  Background:
    Given the `privatewartest` save is loaded
    And cheat mode is enabled with `config.cheat_mode 1`
    And the dev console is open

  # Verified PASS — the original overlay-bucketing bug fix (GetNextDefenderPartyOfSettlement override).
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

  # Verified PASS — sally-out reaches the same augmented defender set; SallyOutStrengthPatch fires.
  Scenario: A weak besieger triggers a garrison sally-out
    Given `player_faction` (1 troop) is besieging `dadg_Pontefract_castle` owned by `clan_york`
    And the garrison plus militia far outpower the besieger
    When the sally-out strength check runs
    Then `SallyOutStrengthPatch` enters its private-war branch because the besieger is the owner's enemy
    And the garrison sallies out (296 defenders vs 1 besieger), no crash
    # eval: PrivateWarSiegeDefenderPolicy.AreEnemies(player_faction, clan_york) == true
    # eval: SallyOutStrengthPatch — num3 (sally side) ≈ 324.99 ; num (besieger) ≈ 0.797 ; garrisonWouldSally == true
    # eval: Pontefract.SiegeEvent.CanPartyJoinSide(garrison.Party, Defender) == true

  # Verified PASS (2026-06-24) — vanilla sally-out parity; militia are static wall defenders, excluded from SallyOut.
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

  # Verified PASS — the policy is player-agnostic; no overlay involved.
  Scenario: An AI besieger populates the defender side against a same-kingdom rival
    Given `clan_hastings` and `clan_howard` share a kingdom (House of York / vlandia)
    And `clan_howard` owns `Norwich_town` with a 502-troop garrison and a 407-troop militia
    And `clan_hastings` (an AI clan, not the player) is the registered private-war besieger
    When I run `campaign.force_besiege clan_hastings Norwich_town` and `campaign.create_siege clan_hastings Norwich_town`
    Then the garrison and militia are admitted as defenders even with no player involved
    # eval: PrivateWarSiegeDefenderPolicy.AreEnemies(clan_hastings, clan_howard) == true
    # eval: EncounterModel.GetDefenderPartiesOfSettlement(Norwich, Siege) contains garrison(502) + militia(407)
    # eval: Norwich.SiegeEvent.CanPartyJoinSide(garrison.Party, Defender) == true

  # Verified PASS — same-kingdom AI sieges resolve by prep-complete capture, not an assault MapEvent.
  Scenario: An AI same-kingdom siege resolves by preparation-complete capture
    Given `clan_hastings` is besieging `clan_howard`'s `Norwich_town`
    When I run `campaign.complete_siege_prep Norwich_town`
    And `TryCaptureCompletedSieges` runs
    Then `Norwich_town` ownership transfers from `clan_howard` to `clan_hastings`
    And the SiegeEvent is cleaned up, no crash
    And the private war stays Active (capture transfers the fief; score still drives resolution at ±100)
    # eval: Norwich_town.OwnerClan == clan_hastings (was clan_howard) after TryCaptureCompletedSieges()
    # eval: Norwich_town.SiegeEvent == null
