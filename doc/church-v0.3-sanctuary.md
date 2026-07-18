# Church v0.3 — Sanctuary

Third slice of the DADG religious system, building on v0.1 (church villages, abbots, donations)
and v0.2 (mass, tithe, sacrilege). Gives monasteries strategic meaning: they shelter the hunted —
including you — and violating that shelter is sacrilege.

**Out of scope (later/cut):** hierarchy characters (v0.4), AI lords dragging fugitives out
(player-only violation for now), any Harmony patching.

**Design note (2026-07-19):** player-claimed sanctuary (§1) was debated and briefly cut — the
1.4.6 AI targeting loop already skips any settled party, so "hide from enemies" exists in every
neutral settlement and vanilla's threat model rarely hunts the player. It was restored as
flavor-plus: its real deltas are shelter inside *enemy-faction* villages, raid-proof waiting
(a normal village wait ejects you into the raid encounter), and roleplay. A future reframe worth
considering: tie it to the post-captivity escape, the one moment the player is a lone hero anyone
can run down.

All engine facts below verified against decompiled 1.4.6 sources: AI targeting skips parties with
`CurrentSettlement != null` that aren't garrison/lord parties; the only force-out is the menu
interrupt `encounter_interrupted_raid_started` returned by
`DefaultEncounterGameMenuModel.GetGenericStateMenu` when the current village `IsUnderRaid` — and
that switch is *requested by the wait menu's own tick delegate* (vanilla's calls
`SwitchToMenuIfThereIsAnInterrupt`; ours doesn't). Fugitive lords are re-teleported to their own
faction's fiefs by `HeroSpawnCampaignBehavior.OnHeroDailyTick` (30%/day, ~100% when sheltering in
an at-war faction's settlement), so placement must be re-asserted, not one-shot.

## 1. Player sanctuary

A church-village menu option → a wait menu where time passes and nothing can touch you.

- **Entry**: "Claim sanctuary" on the `"village"` menu — visible only at church settlements,
  enabled when village state is `Normal`, `LeaveType.Wait`. Consequence: record
  `_playerSanctuaryStart = CampaignTime.Now`, switch to the `dadg_church_sanctuary` wait menu.
- **Wait menu**: `AddWaitGameMenu` with `WaitMenuShowProgressAndHoursOption`; **40-day cap**
  (historical sanctuary term) with the progress bar driven manually from the tick
  (`SetProgressOfWaitingInMenu(elapsedDays / 40f)`) so it survives save/load. On expiry:
  *"Your forty days of sanctuary are spent."* and ejection back to the village menu.
  "Leave the sanctuary" option exits early. The tick never calls vanilla's interrupt helper —
  that is what keeps raids from ejecting the player.
- **Raid while inside**: one log line (*"Raiders torch {VILLAGE_NAME} around you, but the abbey
  walls hold."*) and the player stays seated.
- **No gate, no cost**: sanctuary was owed to anyone; the price is time.

## 2. Fugitive lords take sanctuary

Defeated lords appear at the nearest monastery instead of vanishing into the fugitive ether.

- **Hook**: `CampaignEvents.MobilePartyDestroyed` (`Action<MobileParty, PartyBase>`). Eligible:
  `mobileParty.IsLordParty`, `LeaderHero` alive, not a prisoner, not player clan. On eligibility:
  pick the nearest church settlement by map distance, tag the hero
  (`_fugitiveSanctuaries[hero] = settlement`, `_fugitiveSanctuaryStarts[hero] = CampaignTime.Now`),
  `EnterSettlementAction.ApplyForCharacterOnly(hero, settlement)`, and if the player has met the
  hero, log *"{LORD_NAME} has taken sanctuary at {MONASTERY_NAME}."*
- **Holding them there** (the part vanilla fights): `CampaignEvents.DailyTickHeroEvent` — if the
  hero is tagged and `hero.CurrentSettlement != tagged settlement`, re-enter them. This runs after
  vanilla's relocation in the same daily cadence and simply wins the tug-of-war; no Harmony.
- **Sanctuary ends** (untag, no teleport) when any of: hero dead/prisoner; hero has a party again
  (`hero.PartyBelongedTo != null` — the clan re-spawned them, and the new party spawns *at the
  monastery*, which is exactly the fantasy); or **20 days** elapsed (fallback so nobody rots
  forever — vanilla relocation then does its thing).
- The tagged lord is a normal settlement-stay hero: visible in the village hero lists, talkable,
  appears in encyclopedia as staying there. Zero extra UI.

## 3. Violating sanctuary (the sacrilege payoff)

- **Option** on the `"village"` menu at church settlements:
  *"Drag {FUGITIVE_NAME} from the cloister"* — visible only when a tagged fugitive is currently
  in this settlement; enabled only if the player's faction is **at war** with that fugitive's
  faction (tooltip otherwise: *"You are not at war with {FUGITIVE_NAME}."*).
  `LeaveType.HostileAction` (1.4.6 has no `Escort`). Targets the first war-eligible fugitive
  present; with several fugitives, each seizure re-shows the option for the next.
- **Consequence**: `TakePrisonerAction.Apply(PartyBase.MainParty, fugitive)` (verified 1.4.6
  signature; `ApplyInternal` nulls `StayingInSettlement`, so no manual settlement-leave needed),
  untag the hero, then **sacrilege** — the v0.2 raid-relation cascade refactored into shared
  `ChurchCampaignBehavior.ApplySacrilege(Hero offender, Settlement site)` (−15 with the local
  abbot, −5 with every other church abbot, player message) called from both the raid listener
  and here.
- The strategic loop: monasteries protect your beaten enemies; you *can* break the rules and take
  a valuable prisoner, and all of England's clergy remembers.

## 4. Code layout (as implemented)

- `src\DellarteDellaGuerra.Domain\Church\Sanctuary\SanctuaryPolicy.cs` — pure:
  `PlayerSanctuaryDays = 40`, `FugitiveSanctuaryDays = 20`,
  `Evaluate(float daysElapsed, float capInDays)` → `SanctuaryOutcome.Active/Expired`;
  `SanctuaryPolicyTests` (3 boundary tests).
- `src\DellarteDellaGuerra\Church\Api\Campaign\SanctuaryCampaignBehavior.cs` — all three features,
  `SyncData` for `_playerSanctuaryStart` (`CampaignTime`), `_fugitiveSanctuaries`
  (`Dictionary<Hero, Settlement>`), `_fugitiveSanctuaryStarts` (`Dictionary<Hero, CampaignTime>`).
  Registered in `SubModule.InitializeGameStarter` next to `ChurchCampaignBehavior`.
- `src\DellarteDellaGuerra\Church\Api\Campaign\ChurchSaveableTypeDefiner.cs` — registers the
  `Dictionary<Hero, Settlement>` save container (vanilla 1.4.6 doesn't define it); base id
  674592360, distinct from the tournament definers.
- `src\DellarteDellaGuerra\Church\Api\Campaign\ChurchCampaignBehavior.cs` — `OnVillageLooted`
  cascade refactored into `internal static ApplySacrilege`.

## 5. Risks

- **Raid interrupt**: our wait menu's tick not calling the interrupt helper *should* keep the
  player seated during a raid, since the vanilla switch originates in the wait-tick. If some
  other path still forces `encounter_interrupted_raid_started` in playtest, fallback: listen to
  `VillageBeingRaided` for the sanctuary village and switch back to `dadg_church_sanctuary`.
  Harmony on `GetGenericStateMenu` is the last resort.
- **Daily-tick ordering**: vanilla's fugitive relocation and our re-placement both run on daily
  hero cadence; if ordering ever lets a lord blink away for part of a day, it's off-screen and
  self-corrects. Accepted.
- **Party-spawn location**: the clan may re-spawn the lord's party at the monastery, briefly
  putting a hostile lord party next to a church village. Thematic, accepted.
- **Save container**: `Dictionary<Hero, Settlement>` registration must survive a live save/load
  round-trip; if the saver rejects it, fall back to a `Settlement.StringId`-keyed dict.
- **`MobilePartyDestroyed` timing**: if the engine nulls `LeaderHero` before the event in some
  defeat paths, those lords silently skip sanctuary. Eligible-only by design.

## 6. Verification

1. Unit: `SanctuaryPolicyTests` (3 boundary cases) — all Domain tests pass (13 total).
2. Build Debug via `subst` short path + `dotnet test --no-build` (memory/worktree-build-quirks).
3. Live playtest (deferred with v0.1/v0.2's):
   - Claim sanctuary at Tintern; time passes with progress bar; bar state survives save/load;
     leave early works; 40-day expiry ejects to village menu.
   - Have the village raided while waiting: player stays seated, log line shown.
   - Defeat an AI lord's party: he appears staying at the nearest monastery; still there next day;
     gone once his clan re-spawns his party (party appears at the monastery) or after 20 days.
   - Drag a war-enemy fugitive out: he becomes your prisoner; −15/−5 clergy relations + sacrilege
     message. Option greyed when not at war with him; hidden when no fugitive present.
   - Save/load with a tagged fugitive: tag survives (exercises the new save container).
