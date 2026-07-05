# Same-kingdom private-war field-encounter dialog — runtime fixtures.
#
# These are NOT executed by an automated runner (Bannerlord has no in-process BDD harness). They are
# Gherkin documentation of the manual / MCP-driven playtests that verify the player-facing FIELD
# ENCOUNTER between two same-kingdom clans at private war: meeting a rival party on the map must open
# the ENEMY conversation (not a forced battle, not a friendly dismissable meeting) and that
# conversation must be able to escalate to a real battle in both directions. Companions:
#   - doc/feudal-private-wars-design.md  §4.3 (player-immersion encounter/menu layer)
#   - doc/feudal-private-wars-manual-test-guide.md  (console-command playbook)
#   - doc/features/private-wars-siege-defenders.feature  (the sibling siege fixtures)
#
# Steps written as `# eval:` are evaluated in the JetBrains debugger (evaluate_expression) against the
# live campaign; plain When/Then steps are dev-console (`Alt + ~`) commands or on-screen observations.
# Clan ids look like `clan_percy`; the player clan is `player_faction` (Braxton).
#
# CRITICAL FIXTURE RULE: these scenarios are only meaningful when the player clan and the rival clan
# share a MapFaction (one kingdom). That is the whole point of a *private* war — a cross-kingdom or
# independent pair is an ordinary war the stock engine already handles. `declare_private_war` prints a
# WARNING when the two clans are NOT in the same MapFaction; that warning MUST be absent for these
# runs. The `privatewartest` save is expected to seat Braxton and clan_percy in one kingdom, but the
# save state can drift (e.g. a won battle declares a real kingdom war and pulls the player out of the
# shared faction). Always assert same-MapFaction in Background before trusting a result; use
# `campaign.change_kingdom` to repair the fixture if it has drifted.
#
# The three units under test (all gate on PrivateWarPatchHelper.AreEnemies via the `__result` guard, so
# they only fire when the vanilla IsAtWarAgainstFaction check is already false — i.e. exactly the
# same-kingdom case — and never disturb a genuine MapFaction war):
#   - PlayerIsEnemyTagPatch                 (postfix on PlayerIsEnemyTag.IsApplicableTo)            -> hostile dialogue tree
#   - PlayerCanAttackPrivateWarRivalPatch   (postfix on conversation_player_can_attack_hero_on_condition) -> player's "Yield or fight!" option
#   - WillLordAttackPrivateWarPatch         (postfix on HeroHelper.WillLordAttack)                  -> rival opens a hostile encounter on the player
# The old DadgEncounterGameMenuModel forced-battle model is DELETED; there is no forced-battle fallback.

Feature: Same-kingdom private-war field encounter opens an enemy dialog that can escalate to battle
  When two clans of one kingdom are at private war, running into the rival's party on the campaign map
  must behave like meeting an enemy: the enemy conversation tree (hostile greeting + an attack option)
  rather than a friendly dismissable meeting, and the conversation must be able to start a real field
  battle whether the player or the rival initiates. Both sides still share a MapFaction, so every
  vanilla escalation gate (IsAtWarAgainstFaction) returns false and the DADG postfixes are the only
  thing opening the fight path.

  # Same-MapFaction precondition — Verified PASS 2026-06-30 on the `pw_v13_fixture` save (Braxton and
  # clan_percy both seated in House of York, kingdom/MapFaction id `vlandia`). Prefer this save over
  # `privatewartest` (whose player clan is independent) for a genuine private-war fixture.
  Background:
    Given the `pw_v13_fixture` save is loaded (Braxton and clan_percy both in House of York / `vlandia`)
    And cheat mode is enabled with `config.cheat_mode 1`
    And the dev console is open
    And the player clan `player_faction` (Braxton) and `clan_percy` share one MapFaction (kingdom)
    # If they do not (declare prints a same-MapFaction WARNING), repair before continuing:
    #   campaign.change_kingdom player_faction <clan_percy's kingdom id>
    # eval: Hero.MainHero.Clan.MapFaction == Clan.FindFirst(c => c.StringId == "clan_percy").MapFaction
    When I run `campaign.declare_private_war player_faction clan_percy`
    Then the command reports the war Active and prints NO "not in the same MapFaction" warning
    # eval: PrivateWarPatchHelper.AreEnemies(player_faction, clan_percy) == true

  # Verified PASS 2026-07-01 on a genuine same-kingdom clan_percy party (Baroness Eleanor leading a
  # Percy party; player fielded the stronger force and ran her fleeing party down). Riding into her
  # opened the ENEMY dialog (hostile greeting), not a friendly meeting or an auto-battle.
  Scenario: The player riding into a same-kingdom rival opens the enemy dialog, not a battle or friendly meeting
    Given the player and `clan_percy` are at private war and share a MapFaction
    And a free (non-prisoner) `clan_percy` lord leads a party on the campaign map
    When the player party engages that lord's party in the field
    Then a map CONVERSATION opens (not an auto-battle, not a forced encounter menu)
    And the greeting uses the ENEMY dialogue tree, not a friendly meeting
    And the hostile demand option `main_option_hostile_1_2` ("Surrender or die") is offered
    And the enemy-tagged leave option `player_is_leaving_enemy_polite` is present
    # eval: PlayerIsEnemyTag.IsApplicableTo(rivalLord.CharacterObject) == true   [via PlayerIsEnemyTagPatch postfix]
    # eval: LordConversationsCampaignBehavior.conversation_player_can_attack_hero_on_condition() == true [via PlayerCanAttackPrivateWarRivalPatch postfix]

  # Verified PASS 2026-07-01 on the same-kingdom clan_percy party (Baroness Eleanor). The full chain
  # ran: hostile menu -> "You know we're at war" -> "Yield or fight!" -> Attack menu -> field battle
  # engaged and WON, with the loot + prisoner screens afterward and no "this will cause a war" warning.
  Scenario: The player can escalate the dialog to a real field battle
    Given the enemy conversation with the `clan_percy` lord is open
    When I choose `main_option_hostile_1_2` then `player_verify_attack_on_enemy_lord` ("You heard me. Yield or fight!")
    Then the `encounter` menu opens with an "Attack!" option
    And the encounter text shows NO "this will cause a war" warning (no MapFaction war is created)
    And selecting "Attack!" launches a real field battle mission
    # eval: PlayerEncounter.Current != null && PlayerEncounter.Current.IsJoinDecisionMade after Attack!

  # Post-battle lord capture — NOT a private-war defect; CONFIRMED vanilla RNG. On the first 2026-07-01
  # win the loot + troop-prisoner screens appeared but Baroness Eleanor was not captured and no
  # captured-lord dialog fired; a fresh playthrough with the identical workflow DID capture her. So
  # capture is possible — the same-kingdom layer neither forces nor blocks it. Root cause is vanilla:
  # Hero.CanBecomePrisoner() returns true for any lord with NO war/faction gate (Hero.cs), so the
  # same-kingdom MapFaction wall does not block capture. In MapEvent.LootDefeatedPartyMembers a defeated
  # lord who is not rolled into the winner's loot-share prisoner roster instead falls through to
  # MakeHeroFugitiveAction.Apply (escapes); and if the defeated party RETREATS rather than being wiped
  # out the method early-returns (RetreatingSide != None) and no hero disposition happens at all. Both
  # non-capture outcomes are SILENT on the map screen: CharacterBecameFugitiveLogEntry is IEncyclopediaLog
  # only (no chat/notification banner), so the absence of an on-screen "she escaped" message does NOT
  # imply a bug — check the hero's encyclopedia history to see a fugitive entry. The CapturedLord
  # conversation (PlayerEncounter.DoCaptureHeroes) only fires for a hero actually placed in the prisoner
  # roster.
  Scenario: Winning the field battle may or may not capture the rival lord (vanilla capture/escape roll)
    Given the player wins the escalated field battle against the `clan_percy` lord
    Then the loot and troop-prisoner screens open as normal
    And the rival lord is EITHER taken prisoner (captured-lord dialog fires) OR escapes as a fugitive
    # This is vanilla RNG; the private-war layer neither forces nor blocks the hero capture.

  # Verified PASS (call-to-arms) 2026-06-30 on pw_v13_fixture. A rival-side party intercepted the
  # player and opened a hostile encounter that escalated to the `dadg_field_03` battle deployment, with
  # NO MapFaction-war warning. NOTE: the intercepting clan was `clan_harrington` (a de jure VASSAL of
  # the defender principal clan_percy), not clan_percy itself. That is CORRECT, not a bug: AreEnemies
  # resolves each clan's war side by walking UP its suzerain chain to the nearest belligerent principal
  # (design §18.A recursive call-to-arms; proven by Domain.Tests WarSideResolverTests), so every clan in
  # the rival principal's de jure subtree is a legitimate belligerent. `list_private_wars` prints only
  # the two PRINCIPALS, so a called-to-arms vassal will not appear there even though it is correctly
  # hostile. Egress at the deployment phase was force-aborted (a separate battle-leave snag, not a
  # feature defect). So "the rival" below means the rival principal OR any clan on its side.
  Scenario: A same-kingdom rival (principal or its called-to-arms vassal) catches the player and opens a hostile encounter that escalates to battle
    Given the player and `clan_percy` are at private war and share a MapFaction
    And a party on clan_percy's war side (clan_percy or a de jure vassal of it) intercepts the player party (player is the encounter defender)
    When the encounter opens
    Then the rival uses the attacking conversation branch, not a friendly meeting
    And the rival's "Yield or fight!" path leads to a real field battle (dadg_field_03 deployment reached)
    # eval: HeroHelper.WillLordAttack(rivalLord) == true   [via WillLordAttackPrivateWarPatch postfix]
    # eval: PrivateWarPatchHelper.AreEnemies(player_faction, interceptingClan) == true  [vassal resolves to Percy's side via WarSideResolver]
    # eval: PlayerEncounter.Current.PlayerSide == BattleSideEnum.Defender

  # Provisional PASS — the regression (a dismissable friendly re-collision loop) was NOT observed
  # 2026-06-30; pending same-kingdom re-confirmation.
  Scenario: The encounter never degrades into a friendly dismissable loop
    Given the player and `clan_percy` are at private war and share a MapFaction
    When the player meets the rival party in the field
    Then the meeting is hostile and offers a path to battle
    And the rival does NOT open a friendly meeting that can only be dismissed and immediately re-collides
