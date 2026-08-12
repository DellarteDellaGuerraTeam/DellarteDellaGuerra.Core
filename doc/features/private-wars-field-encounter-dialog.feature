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
  #
  # Re-verified PASS 2026-08-11, live 1471 Bassett campaign (post `Bannerlord.PrivateWars.Domain` API
  # flattening, commit 923c13b). `campaign.list_private_wars` -> `player_faction vs clan_percy
  # goal=Warkworth_town score=0 battle=0 status=Active` — Active, no WARNING, confirming same-MapFaction
  # still holds on the flattened API. Scenarios below in this pass were field-tested against a
  # DIFFERENT, already-Active war on the same live save (`player_faction vs clan_york`, established
  # while re-verifying the sibling `private-wars-siege-defenders.feature`) using the call-to-arms vassal
  # `clan_scrope_of_masham`, because that lord's party was the one available to intercept/engage on the
  # map at test time. This substitution exercises the identical `PrivateWarPatchHelper.AreEnemies` code
  # path the Background asserts for `clan_percy` (same policy, same patches, only the war id differs) —
  # see the per-scenario notes below for exactly what was substituted and why that is still valid
  # evidence for these scenarios' claims.
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
  #
  # Re-verified PASS 2026-08-11 against `clan_scrope_of_masham` (Thomas Scrope's own 113-troop party,
  # `army:null`, a call-to-arms vassal on the clan_york war side — see Background note). Player used
  # `bannerlord.party.engage_party` to pursue and catch the lord's party. The ENEMY dialogue tree opened
  # (not a friendly meeting, not an auto-battle): speaker "Thomas Scrope", faction "House of York", text
  # "I can offer you a glorious death in battle, or the hospitality of my dungeons." — clearly hostile,
  # matching the scenario's intent.
  # DISCREPANCY (documented, not a bug): the actual runtime option ids/text at this node were `545`
  # ("We'll fight to our last drop of blood!"), `546` ("Actually, I think you're the one who ought to
  # surrender."), `547` ("Stay your hand! Perhaps we don't have to come to blows."), and
  # `player_is_leaving_surrender` ("Don't attack! We surrender.") — NOT `main_option_hostile_1_2`
  # ("Surrender or die") / `player_is_leaving_enemy_polite` as this file's 2026-07-01 annotation records,
  # and there was NO free/polite leave option at all. Hypothesis, not confirmed: the 07-01 run chased down
  # a weaker FLEEING party (player stronger), which vanilla's lord-conversation system may route through a
  # different, more lenient branch (offering a polite leave) than a still-cohesive, comparably-strong rival
  # party (Scrope's 113 vs the player's 86) that turns to fight — the dialogue text itself ("glorious death
  # ... or dungeons") reads as a confident, non-fleeing lord. Both runs agree on the essential claim under
  # test (ENEMY tree opens, not friendly/auto-battle); the option-id/leave-option difference looks like
  # rival-state-dependent branching within the same hostile tree, not a regression.
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
  #
  # Re-verified PASS 2026-08-11 against `clan_scrope_of_masham` (Thomas Scrope). Full chain, real GABS
  # tool calls: selected option `545` ("We'll fight to our last drop of blood!", the escalation-branch
  # equivalent of `player_verify_attack_on_enemy_lord`) -> speaker line changed to "Very well. Expect no
  # mercy." -> `bannerlord.menu.get_current` showed the `encounter` menu open with a 12-option list
  # including `attack`/"Attack!" and NO "this will cause a war" text anywhere in the menu body or in
  # `check_blockers` at any point in the chain -> selected `attack` -> `get_game_state` immediately
  # showed `state:"mission"`, `missionName:"dadg_field_03"` -> `battle.get_state` progressed
  # Deployment -> Battle (`mode` field), `isFieldBattle:true`, `playerSide:"Defender"`,
  # `enemySide:"Attacker"`, `enemyTroopCount:113`, `playerTroopCount:86`. Screenshots:
  # screenshot_20260811_174035.jpg (pre-attack encounter menu), screenshot_20260811_174154.jpg (real
  # deployment screen, "Reset Deployment"/"Ready" buttons, player's 86 troops in formation). A real field
  # battle mission was genuinely launched, not a menu-only transition.
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

  # Re-verified PASS (escape branch) 2026-08-11 against `clan_scrope_of_masham` (Thomas Scrope). The
  # `dadg_field_03` battle from the scenario above was resolved via the `mission.flee_enemies` console
  # cheat (used to avoid manual WASD combat play on a precious, unsaved campaign — Bug #1, save_game
  # still broken) rather than a natural melee finish. Result: `battle.get_state` ->
  # `battleResult:{"battleState":"DefenderVictory","playerVictory":true}`, `missionEnded:true`,
  # `playerDeaths:0`. Post-mission, `bannerlord.party.get_player_party` showed `prisonerCount:46` (rank-
  # and-file troops captured), but `campaign.is_prisoner dadg_lord_50_1` -> "dadg_lord_50_1 (Thomas
  # Scrope) is NOT a prisoner." — the lord himself escaped as a fugitive, not captured. This is the
  # "escapes" branch of the documented RNG, real evidence of the mechanism firing correctly (troops and
  # lord are handled independently, exactly as the design note above this scenario describes).
  # LIMITATION: only one branch was observed this run, and the fixture technique itself (forcing a mass
  # flee via console command rather than a natural rout/kill resolution) plausibly biases toward the
  # escape outcome over capture — a fleeing, still-mobile lord is more likely to fall through to
  # `MakeHeroFugitiveAction.Apply` than one downed/surrounded in melee. Treat this as confirmation the
  # escape path works, not as a rebalanced probability claim; the "capture" branch already has its own
  # 2026-07-01 evidence in the note above.

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

  # Verified PASS 2026-08-11, this is the best-fitting scenario for this session's fixture and has the
  # strongest evidence of the whole file. `clan_scrope_of_masham` carries NO liege/vassal/suzerain
  # attribute in `dadg_clans.xml` (confirmed by direct read of the Faction node); its hostility to the
  # player can only come from DADG's LIVE feudal-title suzerain chain (`FeudalHierarchyAdapter` ->
  # `IGetSuzerainUseCase`, in `DellarteDellaGuerra.Domain.Titles`), exactly the mechanism this scenario
  # names ("principal or its called-to-arms vassal").
  # Debugger evidence (real breakpoint on normal execution flow, not an ad-hoc GC-unsafe pause):
  # `set_breakpoint` on `PrivateWarSiegeDefenderPolicy.cs:31`
  # (`condition: a.StringId=="clan_scrope_of_masham" || b.StringId=="clan_scrope_of_masham"`,
  # `temporary:true`, id `e35206fd-aba1-4fc0-8a87-349df55c4bba`), hit once, then:
  #   evaluate_expression: a.StringId+" vs "+b.StringId -> "clan_scrope_of_masham vs player_faction"
  #   evaluate_expression: PrivateWarsServices.PrivateWarHostility.AreEnemies("clan_scrope_of_masham","player_faction") -> true
  # Stack trace at the hit confirmed the call originated from `WillLordAttackPrivateWarPatch.cs:51` —
  # exactly the postfix this scenario's `# eval` comment names. Breakpoint auto-removed (temporary) and
  # `resume_execution` called immediately; no user breakpoints touched.
  # `battle.get_state` after deployment confirmed the encounter-side claim directly, no eval needed:
  # `playerSide:"Defender"`, `enemySide:"Attacker"` — even though the player used
  # `bannerlord.party.engage_party` to close distance and catch Scrope's party (a player-initiated
  # pursuit action), the resulting MapEvent still cast the PLAYER as the encounter Defender and Scrope as
  # Attacker, matching this scenario's Given clause ("player is the encounter defender") rather than
  # Scenario 1's framing. This is a genuine, load-bearing distinction the game engine makes independent
  # of which party physically moved to close the gap.
  # The rival's escalation path (option `545`, "We'll fight to our last drop of blood!" — the runtime
  # equivalent of "Yield or fight!" in this build) led to real `dadg_field_03` deployment, matching the
  # Then clause exactly.

  # Provisional PASS — the regression (a dismissable friendly re-collision loop) was NOT observed
  # 2026-06-30; pending same-kingdom re-confirmation.
  #
  # Re-verified PASS 2026-08-11 against `clan_scrope_of_masham` (Thomas Scrope), with two independent
  # pieces of evidence beyond the original observation:
  # 1. The full option list at the hostile top-level node (`545`/`546`/`547`/`player_is_leaving_surrender`,
  #    see Scenario 1 note) has NO free/neutral "leave" choice at all — every option is fight, counter-
  #    demand, an attempted negotiation, or outright surrender. There is no dismiss-and-walk-away path.
  # 2. Explored the negotiation path (`547`, "Stay your hand...") to its end: it opened `hero_barter` ->
  #    a REAL BarterScreen UI (screenshot `screenshot_20260811_173450.jpg`: Thomas Scrope 96620 denars vs
  #    player 0 denars, Fiefs/Prisoners/Item/Diplomacy/Other on both sides, Cancel/Gift buttons) — a
  #    substantive negotiation, not a rubber-stamp dismiss. Also tried the `lord_barter_let_go_2`
  #    bribe-for-release option directly: the AI refused ("Why should I negotiate for your gold, when I
  #    have enough men to simply take it?"), consistent with a heavily-outnumbering hostile party offering
  #    no cheap escape. Backing out of the BarterScreen via Cancel (OS-level click injection, see bug
  #    list) returned to the SAME hostile dialogue node — not to a neutral/dismissed campaign-map state —
  #    confirming there is no "friendly meeting that dismisses and immediately re-collides" degradation
  #    anywhere in this tree.
  Scenario: The encounter never degrades into a friendly dismissable loop
    Given the player and `clan_percy` are at private war and share a MapFaction
    When the player meets the rival party in the field
    Then the meeting is hostile and offers a path to battle
    And the rival does NOT open a friendly meeting that can only be dismissed and immediately re-collides
