# Feudal title ledger, historical tenure and won-claim awards — runtime fixtures.
#
# These are NOT executed by an automated runner (Bannerlord has no in-process BDD harness). They are
# Gherkin documentation of the MCP-driven playtests that verify, in a live campaign:
#   - the holder ledger on Title (6d244d3) and the heldSince tenure dates (a51d315);
#   - the four review fixes in c1f053a:
#       #1 a new campaign starts from the configured titles, not the last campaign's;
#       #2 a won claim war only moves the title into the winner's realm if the loser still held it;
#       #3 a cadet branch survives only if it actually took the title;
#       #4 an heir whose own clan occupies the seat ends the occupation.
# Companions:
#   - doc/feudal-titles.md  (heldSince, the tie table)
#   - doc/cross-border-marches-design.md  §7.5 (what a won claim war gives)
#
# Steps written as `# eval:` are evaluated in the JetBrains debugger (evaluate_expression) against the
# live campaign, from the safe anchor DellarteDellaGuerra.Domain/DisplayCompilingShaders/
# DisplayShaderNumber.cs:25 (main thread, ~1 Hz, live on the campaign map). Plain When/Then steps are
# dev-console (`Alt + ~`) commands or on-screen observations. Shorthands used in the evals:
#   T(id)       = FeudalServices.Titles.GetTitle(id)
#   Clan(id)    = Clan.All.First(c => c.StringId == id)
#   Today       = (float)CampaignTime.Now.ToDays
#   Pressure    = Campaign.Current.GetCampaignBehavior<ClaimPressureCampaignBehavior>()
#   Conclude(a, d, title, won) = typeof(ClaimPressureCampaignBehavior)
#                     .GetMethod("OnWarConcluded", BindingFlags.Instance | BindingFlags.NonPublic)
#                     .Invoke(Pressure, new object[] { new PrivateWarConclusion(a, d, title, won) })
# A claim war cannot be fought to a chosen outcome from the console, so the conclusion scenarios raise
# the conclusion directly on the live behaviour; everything after that (the award, seat transfers,
# kingdom changes, the cadet branch) runs through the real game code.
#
# Fixture titles are chosen so each scenario touches its own houses, and scenarios run in file order:
# the tenure outline reads untouched starting state, and the new-campaign scenario must run last.
# The save-without-feudal-state path of fix #1 is covered by FeudalTitleCampaignBehaviorTests only; it
# needs a save written without the mod and is not playtested here.
#
# 2026-09-29 LIVE PLAYTEST #2 (sonnet, this session): GABS connected successfully this time (port 4825,
# 101 tools discovered) against save "privatewarandwarsails" (fresh 1471 sandbox, Today ~= 123585.4 days).
# All evals ran from the DisplayShaderNumber.cs:25 anchor (confirmed hit reliably at ~1 Hz on the campaign
# map main thread); the anchor breakpoint was removed at the end of the session as instructed.
#
# CORRECTION to the prior session's "cross-cutting HolderHeroId == null defect": that defect does NOT
# reproduce in this session. HolderHeroId was observed correctly populated (matching the expected clan
# leader's hero id) on every title checked — county_suffolk, barony_nottingham, barony_barnard,
# county_gowrie, county_aberdeen, county_fife and county_shropshire, both before and after their scenario
# mutations. The prior session's finding was most likely an artefact of that session's own state (GABS was
# unreachable there and its evidence came entirely from JetBrains reads against a session that may not have
# been the same fresh sandbox), not a genuine DADG code defect. Treat the previous "CROSS-CUTTING DEFECT"
# note as retracted.
#
# SESSION BLOCKER (real, reproducible this session): the Claude Code auto-mode permission classifier denies
# the GABS console command `campaign.kill_hero` ("Irreversible Deletion (general)"); a following
# wait_for_pause call was denied on the same grounds. This blocks any scenario whose trigger is
# `campaign.kill_hero` (Scenarios 3, 4, 5 below). By contrast, JetBrains-debugger evaluate_expression calls
# that mutate live state — SaveTitle(...) fixture setup, the reflection-invoked Conclude(...)/OnWarConcluded
# action, and ICadetBranch.Split(...) — were NOT denied this session and were used successfully end-to-end
# for Scenarios 6-9. This is a session/tooling permission gap around one specific console command, not a
# DADG or game defect, and not a blanket mutation-eval denial as the prior session reported.
#
# HOW TO READ THE PASSES (2026-09-29): no scenario here was reached by organic play. Two kinds:
#   [debugger-read]    only read live state through evaluate_expression. As reliable as the
#                      state itself: it is the same object graph the game uses.
#   [debugger-driven, not organic]  the fixture was written through the debugger AND the trigger
#                      was raised by invoking ClaimPressureCampaignBehavior.OnWarConcluded by
#                      reflection with a hand-built PrivateWarConclusion. This proves the award and
#                      cadet-branch code does the right thing given those inputs. It does NOT prove
#                      that a real PrivateWars claim war reaches OnWarConcluded with them, that the
#                      fixture is reachable in play, or that the result survives save/reload.
#                      Scenarios 6-9 also ran cumulatively on one campaign.
# An organic run would still need the fixture set up (the situations are not easy to reach in play);
# the gap is the trigger, which should come from a real claim war / hero death, not the debugger.

Feature: Feudal title ledger, historical tenure and won-claim awards

  # PASS [debugger-read] 2026-09-29 (live, sonnet playtest #2, GABS connected). Breakpoint hit reliably at
  # DisplayShaderNumber.cs:25, confirming the campaign map is live. Both evals confirmed true:
  # typeof(WonClaimAward).GetProperty("TitleWon") != null and FeudalServices.IsInitialised both true.
  Background:
    Given a fresh DADG sandbox campaign at 1471 under the JetBrains debugger with GABS connected
    And the loaded DellarteDellaGuerra.dll contains the c1f053a fixes
    # eval: typeof(WonClaimAward).GetProperty("TitleWon") != null == true
    # eval: FeudalServices.IsInitialised == true

  # ---------------------------------------------------------------- heldSince and the ledger

  # PASS [debugger-read] 2026-09-29 (live, sonnet playtest #2). All 4 evals confirmed true: Holders.Count == 1,
  # Holders[0].Acquisition == Initial, Holders[0].SinceDay == 116340f, and HolderHeroId ==
  # Clan("clan_de_la_pole").Leader.StringId (both resolved to "dadg_lord_14_1"). The prior session's FAIL
  # here was the retracted cross-cutting HolderHeroId defect, which does not reproduce this session.
  Scenario: Every starting title opens its ledger on its heldSince year
    When I read the ledger of `county_suffolk`
    Then it has exactly one entry, the de la Pole leader, acquired as Initial
    And the entry is dated 1385 × 84 = day 116340
    # eval: T("county_suffolk").Holders.Count == 1
    # eval: T("county_suffolk").Holders[0].Acquisition == TitleAcquisition.Initial
    # eval: T("county_suffolk").Holders[0].SinceDay == 116340f
    # eval: T("county_suffolk").HolderHeroId == Clan("clan_de_la_pole").Leader.StringId

  # PASS [debugger-read] 2026-09-29 (live, sonnet playtest #2), all 5 examples confirmed: GetPrimaryTitle(house).Id ==
  # older and GetSuzerain(house) == liege both returned true for clan_de_la_pole/county_suffolk/
  # clan_mowbray, clan_talbot/county_hallamshire/clan_york, clan_hastings/barony_nottingham/clan_talbot,
  # clan_willoughby/barony_donnington/clan_de_vere and clan_harrington/barony_barnard/clan_neville_of_raby.
  # The prior session's systemic FAIL here was the retracted cross-cutting HolderHeroId defect.
  Scenario Outline: The older of two tied titles is primary and picks the house's liege
    Given `<house>` starts holding `<older>` (<olderYear>) and `<newer>` (<newerYear>), both <rank>
    When the suzerainty policy resolves `<house>`
    Then its primary title is `<older>`
    And its liege is `<liege>`
    # eval: FeudalServices.Suzerainty.GetPrimaryTitle("<house>").Id == "<older>"
    # eval: FeudalServices.Suzerainty.GetSuzerain("<house>") == "<liege>"

    Examples:
      | house             | older              | olderYear | newer              | newerYear | rank     | liege                |
      | clan_de_la_pole   | county_suffolk     | 1385      | county_lincoln     | 1467      | counties | clan_mowbray         |
      | clan_talbot       | county_hallamshire | 1409      | county_shropshire  | 1442      | counties | clan_york            |
      | clan_hastings     | barony_nottingham  | 1461      | barony_liverpool   | 1464      | baronies | clan_talbot          |
      | clan_willoughby   | barony_donnington  | 1440      | barony_tattershall | 1456      | baronies | clan_de_vere         |
      | clan_harrington   | barony_barnard     | 1445      | barony_taunton     | 1460      | baronies | clan_neville_of_raby |

  # BLOCKED 2026-09-29 (live, sonnet playtest #2). GABS was connected this session, so the trigger step
  # `campaign.kill_hero` was attempted as written (not the prior session's KillCharacterAction.ApplyByMurder
  # substitute). The Claude Code auto-mode permission classifier denied `campaign.kill_hero` outright
  # ("Irreversible Deletion (general)"), and a follow-up `wait_for_pause` was denied on the same grounds.
  # Not worked around per the hard rules. Precondition re-verified live and holds correctly this session:
  # T("county_suffolk").HolderHeroId == Clan("clan_de_la_pole").Leader.StringId ("dadg_lord_14_1"), so the
  # prior session's FAIL (attributed to the retracted HolderHeroId defect) would not have reproduced here
  # even if the trigger had been permitted — the blocker is purely the console command denial.
  Scenario: An heir carries on the tenure, so a death does not move the liege
    Given `clan_de_la_pole` serves `clan_mowbray` through `county_suffolk` (1385)
    When I run `campaign.kill_hero` on the de la Pole leader
    Then `county_suffolk`'s ledger gains a second entry for the heir, acquired as Inherited, dated today
    And the title's HeldSinceDay is still 116340
    And `clan_de_la_pole` still serves `clan_mowbray`
    # eval: T("county_suffolk").Holders.Count == 2
    # eval: T("county_suffolk").Holders[1].Acquisition == TitleAcquisition.Inherited
    # eval: T("county_suffolk").HeldSinceDay == 116340f
    # eval: FeudalServices.Suzerainty.GetSuzerain("clan_de_la_pole") == "clan_mowbray"

  # ---------------------------------------------------------------- #4 inheritance and occupation

  # BLOCKED 2026-09-29 (live, sonnet playtest #2). This session's SaveTitle(...) fixture mutation was NOT
  # denied (JetBrains-eval mutations worked fine throughout this session, see Scenarios 6-9), so fixture
  # setup was not attempted here as unnecessary risk was avoided: the blocker is specifically the
  # `campaign.kill_hero` trigger step, which the Claude Code auto-mode permission classifier denies
  # ("Irreversible Deletion (general)") regardless of fixture state. Precondition re-checked live and
  # confirmed correct (HolderHeroId == clan_hastings leader "dadg_lord_11_1", not null — the prior session's
  # HolderHeroId defect is retracted). Fixture setup was intentionally not run this session since the
  # scenario cannot be completed without the kill trigger either way.
  Scenario: An heir whose own clan occupies the seat ends the occupation
    Given `barony_nottingham` is held by the Hastings leader and marked occupied by `clan_hastings`
    # eval (fixture): FeudalServices.Titles.SaveTitle(T("barony_nottingham").WithOccupant("clan_hastings", Today))
    When I run `campaign.kill_hero` on the Hastings leader
    And the heir resolved for the title is a member of `clan_hastings`
    Then the heir holds `barony_nottingham` and the title is no longer contested
    # eval: T("barony_nottingham").Holders.Last().Acquisition == TitleAcquisition.Inherited
    # eval: Hero.FindFirst(h => h.StringId == T("barony_nottingham").HolderHeroId).Clan.StringId == "clan_hastings"
    # eval: T("barony_nottingham").IsContested == false
    # eval: T("barony_nottingham").ContestedSinceDay == null

  # BLOCKED 2026-09-29 (live, sonnet playtest #2). Same `campaign.kill_hero` trigger denial as Scenario 4
  # above; fixture setup not attempted for the same reason (the scenario cannot complete without the kill
  # regardless). Precondition re-checked live: T("barony_barnard").HolderHeroId == Clan("clan_harrington").
  # Leader.StringId (correctly populated, not null — retracted defect). OccupantClanId not set (not yet
  # occupied by clan_neville_of_raby), confirming the SaveTitle(WithOccupant) fixture step would still be
  # needed once the kill_hero denial is resolved.
  Scenario: An heir inherits another house's occupation along with the title
    Given `barony_barnard` is held by the Harrington leader and occupied by `clan_neville_of_raby` since day D
    # eval (fixture): FeudalServices.Titles.SaveTitle(T("barony_barnard").WithOccupant("clan_neville_of_raby", Today))
    When I run `campaign.kill_hero` on the Harrington leader
    Then the heir holds `barony_barnard`
    And `clan_neville_of_raby` still occupies it, contested since the same day D
    # eval: T("barony_barnard").Holders.Last().Acquisition == TitleAcquisition.Inherited
    # eval: T("barony_barnard").OccupantClanId == "clan_neville_of_raby"
    # eval: T("barony_barnard").ContestedSinceDay == D

  # ---------------------------------------------------------------- #2 won claims across the border

  # The contrast case first: the loser still holds the title, so it and its vassal houses change realm.
  # PASS [debugger-driven, not organic] 2026-09-29 (live, sonnet playtest #2). The `# eval (action)` Conclude("clan_percy", "clan_ruthven",
  # "county_gowrie", true) call executed successfully via JetBrains reflection (not denied — mutation evals
  # are permitted this session, only the `campaign.kill_hero` console command is denied). All 5 postcondition
  # evals confirmed true: HolderHeroId now Clan("clan_percy").Leader.StringId; last Holders entry Acquisition
  # == Awarded; GetDeJureSuzerainTitleId("county_gowrie") == "duchy_northumberland"; Clan("clan_campbell").
  # Kingdom == Clan("clan_percy").Kingdom ("vlandia"); the county's seat settlement dadg_sco_perth_town's
  # OwnerClan.StringId == "clan_percy". PASS, debugger-driven (see header).
  Scenario: A won claim on a title the loser still holds carries it and its vassal houses into the winner's realm
    Given `clan_ruthven` holds `county_gowrie`, under `kingdom_scotland`
    And `clan_campbell`'s primary title is `barony_glenorchy`, a barony of `county_gowrie`
    And `clan_percy`'s primary title is `duchy_northumberland`
    When the claim war `clan_percy` vs `clan_ruthven` over `county_gowrie` concludes with the attacker winning
    # eval (action): Conclude("clan_percy", "clan_ruthven", "county_gowrie", true)
    Then the Percy leader holds `county_gowrie`, acquired as Awarded
    And `county_gowrie` now sits under `duchy_northumberland`
    And `clan_campbell` has joined `clan_percy`'s kingdom, fiefs and all
    And the county's seat, if `clan_ruthven` owned it, now belongs to `clan_percy`
    # eval: T("county_gowrie").HolderHeroId == Clan("clan_percy").Leader.StringId
    # eval: T("county_gowrie").Holders.Last().Acquisition == TitleAcquisition.Awarded
    # eval: FeudalServices.Structure.GetDeJureSuzerainTitleId("county_gowrie") == "duchy_northumberland"
    # eval: Clan("clan_campbell").Kingdom == Clan("clan_percy").Kingdom
    # eval: Settlement.Find(T("county_gowrie").SeatSettlementId).OwnerClan.StringId == "clan_percy"

  # PASS [debugger-driven, not organic] 2026-09-29 (live, sonnet playtest #2). Both the `# eval (fixture)` GrantTo(...) SaveTitle call and
  # the `# eval (action)` Conclude("clan_percy", "clan_keith", "county_aberdeen", true) call executed
  # successfully via JetBrains reflection (mutation evals not denied this session). K_erskine and K_hepburn
  # both recorded as "khuzait" (Scotland's DADG kingdom id, per the known vanilla-id-reuse pattern) before
  # the war. All 4 documented postcondition evals confirmed true after Conclude: HolderHeroId still
  # Hepburn's leader; GetDeJureSuzerainTitleId("county_aberdeen") still "kingdom_scotland"; clan_erskine
  # still in K_erskine; clan_hepburn still in K_hepburn. The barony_dunnottar side-note was also verified:
  # its seat settlement's OwnerClan is now "clan_percy" and its own title HolderHeroId is now "dadg_lord_
  # 10_1" (== clan_percy's leader, cross-checked against clan_keith's leader "dadg_sco_lord_clan_keith" for
  # contrast) — i.e. the loser's own vassal barony did move to the winner as documented. PASS, debugger-driven (see header).
  # Note: the working tree has an uncommitted config/titles.config.xml diff that renames county_aberdeen's
  # display name to "Earldom Marischal"; it does not touch holder or heldSince and had no effect here.
  Scenario: A won claim on a title the loser no longer holds leaves it in its realm
    Given `clan_keith` held `county_aberdeen`, under `kingdom_scotland`, when the war began
    And during the war the title passed to the leader of `clan_hepburn`
    # eval (fixture): FeudalServices.Titles.SaveTitle(T("county_aberdeen").GrantTo(Clan("clan_hepburn").Leader.StringId, Today))
    And `clan_erskine`'s primary title is `barony_mar`, a barony of `county_aberdeen`
    And I note the kingdoms of `clan_erskine` and `clan_hepburn` as K_erskine and K_hepburn
    When the claim war `clan_percy` vs `clan_keith` over `county_aberdeen` concludes with the attacker winning
    # eval (action): Conclude("clan_percy", "clan_keith", "county_aberdeen", true)
    Then `county_aberdeen` is still held by the Hepburn leader
    And `county_aberdeen` still sits under `kingdom_scotland`
    And `clan_erskine` and `clan_hepburn` are still in K_erskine and K_hepburn
    And there is no crash and no exception in the log
    # eval: T("county_aberdeen").HolderHeroId == Clan("clan_hepburn").Leader.StringId
    # eval: FeudalServices.Structure.GetDeJureSuzerainTitleId("county_aberdeen") == "kingdom_scotland"
    # eval: Clan("clan_erskine").Kingdom == K_erskine
    # eval: Clan("clan_hepburn").Kingdom == K_hepburn
    # note: barony_dunnottar, which clan_keith still holds under the county, does go to Percy with its
    #       castle; the award still strips the loser's own vassal titles. Record it, do not fail on it.

  # ---------------------------------------------------------------- #3 cadet branches

  # A cadet branch is split off by hand: the claim-pressure tick only does it on a 25% roll.
  # Pretender: any living, adult, non-leader hero of the parent clan who is not a prisoner.
  # PASS [debugger-driven, not organic] 2026-09-29 (live, sonnet playtest #2). Precondition reconfirmed live (HolderHeroId == clan_
  # lindsay's leader). Pretender P chosen as the sole eligible clan_lindsay hero: "dadg_sco_family_
  # lindsay_elizabeth" (non-leader, non-prisoner, non-child). The reflection-based ICadetBranch.Split(P.
  # StringId, "clan_lindsay", "dadg_sco_st_andrews_town") call executed successfully, returning clan id
  # "clan_lindsay_cadet" (=C); verified P.Clan.StringId == C and Clan(C).Leader == P. C registered in the
  # private _parentByCadetClanId dictionary via reflection. Conclude(C, "clan_lindsay", "county_fife", true)
  # then executed successfully. Both postcondition evals confirmed true: T("county_fife").HolderHeroId ==
  # P.StringId; Clan(C).IsEliminated == false && Clan(C).Leader == P. PASS, debugger-driven (see header).
  Scenario: A cadet branch that takes its house's title stands as a house of its own
    Given a hero P of `clan_lindsay` splits off a cadet branch C to press `county_fife`
    # eval (fixture): C = ((ICadetBranch)typeof(ClaimPressureCampaignBehavior).GetField("_cadetBranch", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Pressure)).Split(P.StringId, "clan_lindsay", T("county_fife").SeatSettlementId)
    # eval (fixture): ((Dictionary<string, string>)typeof(ClaimPressureCampaignBehavior).GetField("_parentByCadetClanId", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Pressure))[C] = "clan_lindsay"
    When the claim war C vs `clan_lindsay` over `county_fife` concludes with the attacker winning
    # eval (action): Conclude(C, "clan_lindsay", "county_fife", true)
    Then P holds `county_fife`
    And C is a living clan led by P
    # eval: T("county_fife").HolderHeroId == P.StringId
    # eval: Clan(C).IsEliminated == false && Clan(C).Leader == P

  # PASS [debugger-driven, not organic] 2026-09-29 (live, sonnet playtest #2). Precondition reconfirmed live: SeatSettlementId ==
  # "Shrewsbury_town", HolderHeroId == "dadg_lord_19_1" (Talbot leader). Pretender Q chosen as "dadg_lord_
  # 19_4" from 4 eligible clan_talbot candidates. ICadetBranch.Split("dadg_lord_19_4", "clan_talbot",
  # "Shrewsbury_town") executed successfully, returning "clan_talbot_cadet" (=D), registered in
  # _parentByCadetClanId via reflection. Fixture GrantTo applied mid-war, moving county_shropshire to
  # clan_stanley's leader "dadg_lord_28_1" (verified). Conclude(D, "clan_talbot", "county_shropshire", true)
  # then executed successfully. All 3 postcondition evals confirmed true: HolderHeroId still Stanley's
  # leader (unaffected by the loser's cadet branch losing); Hero("dadg_lord_19_4").Clan.StringId ==
  # "clan_talbot" (Q reabsorbed); Clan.All no longer contains "clan_talbot_cadet" / Clan(D).IsEliminated ==
  # true (D eliminated). PASS, debugger-driven (see header).
  Scenario: A cadet branch that wins against a loser who no longer holds the title is folded back in
    # Talbot still holds county_shropshire: the outline above only reads it.
    Given a hero Q of `clan_talbot` splits off a cadet branch D to press `county_shropshire`
    # eval (fixture): D = <_cadetBranch>.Split(Q.StringId, "clan_talbot", T("county_shropshire").SeatSettlementId); <_parentByCadetClanId>[D] = "clan_talbot"
    And during the war `county_shropshire` passed to the leader of `clan_stanley`
    # eval (fixture): FeudalServices.Titles.SaveTitle(T("county_shropshire").GrantTo(Clan("clan_stanley").Leader.StringId, Today))
    When the claim war D vs `clan_talbot` over `county_shropshire` concludes with the attacker winning
    # eval (action): Conclude(D, "clan_talbot", "county_shropshire", true)
    Then `county_shropshire` is still held by the Stanley leader
    And D has been reabsorbed: Q is back in `clan_talbot` and D is eliminated
    # eval: T("county_shropshire").HolderHeroId == Clan("clan_stanley").Leader.StringId
    # eval: Q.Clan.StringId == "clan_talbot"
    # eval: Clan.All.Any(c => c.StringId == D) == false || Clan(D).IsEliminated == true

  # ---------------------------------------------------------------- #1 a new campaign starts clean

  # Must run last: it discards this campaign. Every mutation above lives in singleton registries that
  # outlive the campaign; before c1f053a a new campaign in the same session inherited them.
  # SKIPPED 2026-09-29 (live, sonnet playtest #2). GABS was connected this session, but no GABS tool exists
  # to exit-to-menu or start a new sandbox campaign (games_tool_names query "campaign" returned only
  # get_campaign_time). Manually driving the exit-to-menu/new-campaign UI through the debugger was judged
  # too risky given the hard rule against relaunching or otherwise disrupting the running session, and no
  # explicit authorization for that specific workaround was given, so this scenario was skipped rather than
  # attempted. Supplementary, non-baseline evidence recorded instead at end of session: total claim count
  # across all titles == 454 (FeudalServices.Structure.GetAllTitleIds().Sum(id => FeudalServices.Claims.
  # GetClaimsOn(id).Count)) — taken AFTER this session's own mutations (Scenarios 6-9), so it is not a
  # fresh-campaign baseline and should not be compared against a future N0.
  Scenario: A new campaign in the same session starts from the configured titles
    Given this campaign has changed `county_suffolk`, `county_gowrie`, `county_aberdeen` and `county_fife`
    And before the first scenario I recorded the total claim count N0 of the fresh campaign
    # eval (baseline, at session start): N0 = FeudalServices.Structure.GetAllTitleIds().Sum(id => FeudalServices.Claims.GetClaimsOn(id).Count)
    When I exit to the main menu without saving
    And I start a new sandbox campaign with the same setup
    Then every title is back with its configured holder and its heldSince date
    And every realm is as configured
    And the claims are those a fresh campaign derives
    # eval: T("county_suffolk").Holders.Count == 1 && T("county_suffolk").Holders[0].SinceDay == 116340f
    # eval: T("county_gowrie").HolderHeroId == Clan("clan_ruthven").Leader.StringId
    # eval: FeudalServices.Structure.GetDeJureSuzerainTitleId("county_gowrie") == "kingdom_scotland"
    # eval: T("county_aberdeen").HolderHeroId == Clan("clan_keith").Leader.StringId
    # eval: T("county_fife").HolderHeroId == Clan("clan_lindsay").Leader.StringId
    # eval: FeudalServices.Structure.GetAllTitleIds().Sum(id => FeudalServices.Claims.GetClaimsOn(id).Count) == N0
