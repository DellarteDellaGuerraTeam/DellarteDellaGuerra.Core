# Church v0.5 — The Living Church

Fifth slice of the DADG religious system, building on v0.1 (church villages, abbots, donations),
v0.2 (mass, tithe, sacrilege), v0.3 (sanctuary) and v0.4 (bishops, favour). This slice makes the
Church *visible in the world*: abbots walking their cloisters as real scene NPCs, and pilgrim
bands roaming the campaign map between the abbeys and the great shrine at Walsingham — with a
gameplay hook (protecting pilgrims earns clergy relations) that also fixes v0.4's favour-dilution
grind (one act moves one relation, not a 16-way average).

**Out of scope (later/cut):** abbot-as-a-job for the player (commendatory abbot — v0.7+
candidate), church-specific scene spawn points (altar/cloister tags need scene edits or Harmony —
deliberately skipped; default spawn-point fallback is good enough), pilgrim escort *quests*
(the reward is ambient, not quest-driven), AI lords interacting with pilgrims, dioceses (v0.6).

## 0. Engine facts this design stands on (verified in decompiled 1.4.6)

- `HeroAgentSpawnCampaignBehavior.AddHeroesWithoutPartyCharactersToVillage` spawns **every**
  hero in `settlement.HeroesWithoutParty` as a walkable scene agent (no occupation filter), tag
  `sp_notable_rural_notable`, `useCivilianEquipment: true`. **Our clergy notables already stand
  in their village scenes with zero code.** Scene work in v0.5 is therefore *appearance only*.
- `MissionAgentHandler.SpawnWanderingAgent` falls back SpecialTargetTag → `npc_common_limited`
  → `npc_common` → any usable point. No silent failure if a village scene lacks notable spots.
- `SpawnNotableHelperCharacters` spawns one ambient `Culture.PreacherNotary` NPC beside each
  preacher notable (tag `sp_preacher_notary`; works in villages via `Center ?? VillageCenter`).
  **NRE risk:** `dadg_cultures.xml` has `preacher_notary` commented out for two cultures
  (lines ~941 and ~1164) — a null `PreacherNotary` NREs in FaceGen during scene entry. If any
  church village belongs to those cultures, this is a *latent v0.1 crash* that must be fixed.
- `CustomPartyComponent.CreateQuestParty(position, spawnRadius, homeSettlement, name, clan,
  partyTemplate, owner, troopLimit, mountId, harnessId, baseSpeed, avoidHostileActions)` creates
  a roaming party **without a new saved PartyComponent type** (a subclass would need a
  SaveableTypeDefiner entry and live in saves forever).
- Party `MapFaction` for a notable-owned party resolves via `Party.Owner.HomeSettlement.MapFaction`
  (flips when the village changes hands) unless `ActualClan` is set. Accepted for v0.5: pilgrims
  travel under the protection of whichever crown holds their abbey. Noted as a risk, not solved.
- `VillagerCampaignBehavior` is the travel-loop blueprint: spawn with
  `EnterSettlementAction.ApplyForParty`, hourly tick re-issues `Ai.SetMoveGoToSettlement` when
  `DefaultBehavior != GoToSettlement` or the target became invalid (war/siege reroute), and
  `DestroyPartyAction` disposes the party when empty.

## 1. Scene abbots (appearance — map-module XML only)

No C# in this repo. All changes live in the **sibling module repo `DellarteDellaGuerraMap`**
(`..\DellarteDellaGuerraMap\ModuleData\`) — see §5 for how those edits are handled.

1. **Monk look for clergy notables**: the preacher notable templates
   (`spc_notable_vlandia_2`, `spc_notable_vlandia_3` in `characters\dadg_npccharacter_commoners.xml`)
   currently wear generic wanderer civilian sets. Give them monk-like **civilian** EquipmentSets
   (dark robe, `pilgrim_hood` — confirmed to exist; reuse pieces from
   `npc_preacher_equipment_vlandia` / `cutscene_monk` where sensible). Civilian sets are what
   the scene spawn uses (`useCivilianEquipment: true`).
2. **Preacher-notary NRE guard**: check the culture of each of the 16 church villages against
   `dadg_cultures.xml`. For any culture whose `preacher_notary` is commented out, restore the
   attribute (`NPCCharacter.preacher_notary_vlandia` is the wired value elsewhere). This fixes
   the latent scene-entry crash *and* gives every abbot his ambient assistant for free.
3. Optionally give `preacher_notary_vlandia` (in `dadg_spnpccharacters.xml`, currently townsman
   gear) the same monk treatment — low-cost polish, same file family.

## 2. The shrine (config + provider)

Pilgrims need a destination. Mark it in the existing settlement config rather than hardcoding:

- `config/dadg.church_settlements.xml`: add `shrine="true"` to Walsingham —
  `<ChurchSettlement id="village_Walsingham_Abbey" kind="Abbey" shrine="true" />`.
- `ChurchSettlementData` gains `bool IsShrine` (default false);
  `ChurchSettlementsXmlProvider` parses the optional attribute.
- `ChurchSettlements` exposes `IsShrine(Settlement)` and `GetShrineSettlementIds()` (or
  equivalent). No shrine configured → pilgrimage stays dormant (log a warning once), matching
  the provider's existing degrade-gracefully contract.

## 3. Pilgrim parties (the roaming Church)

New behavior `src\DellarteDellaGuerra\Church\Api\Campaign\PilgrimageCampaignBehavior.cs`
(DI ctor: `ChurchSettlements`, `ChurchSacrilege`, `IChurchSettingsProvider`), registered in
`DadgSubModule` beside the other church behaviors.

**Spawn.** Daily tick: for each non-shrine church settlement with a living clergy notable, roll
for a pilgrim band; hard cap of `MaxPilgrimParties` (config, default 3) alive at once. The
decision is a pure policy:

- `src\DellarteDellaGuerra.Domain\Church\Pilgrimage\PilgrimagePolicy.cs` —
  `ShouldSpawn(int activeParties, int maxParties, float roll)` with
  `SpawnChancePerSettlementPerDay = 0.05f` as an identity constant. `PilgrimagePolicyTests`
  cover the cap boundary and the roll threshold.

**Creation.** `CustomPartyComponent.CreateQuestParty` at the home village (then
`EnterSettlementAction`-free — spawn just outside is fine per villager pattern):
name *"Pilgrims of {SETTLEMENT}"* (fresh `{=id}`), owner = the home clergy notable, clan `null`
(faction derives from the abbey's owner — accepted, §0), troops from the home culture's villager
party template (verify the exact template id via `MBObjectManager` at implementation; a
dedicated pilgrim template with hooded peasants is optional map-module polish, not required),
`avoidHostileActions: true`, modest `baseSpeed` (0 = default is acceptable if the overload
misbehaves), StringId prefixed **`dadg_pilgrims_`** so our parties are recognizable.

**Travel loop** (villager blueprint): outbound to the shrine, brief stay, homebound, despawn on
arrival home (`DestroyPartyAction`); also despawn when the party is empty or its home/shrine
settlement stops being valid. Hourly tick re-issues `Ai.SetMoveGoToSettlement` whenever
`DefaultBehavior` drifted or the target became unreachable (siege/war), mirroring
`VillagerCampaignBehavior.HourlyTickParty`.

**State.** Minimal `SyncData`: a `Dictionary<MobileParty, Settlement>` (party → home) plus
whatever single flag distinguishes outbound/homebound (agent may derive it from
`TargetSettlement` instead of storing it — prefer derivation). Any non-vanilla container goes
into `ChurchSaveableTypeDefiner` (base 674592360). Keys use the established
`"_dadgChurch..."` naming.

**Encounter dialog.** One flavour exchange when the player talks to a pilgrim party on the map
(villager-style party conversation): pilgrim greeting mentioning the shrine and their abbey,
player farewell line. Fresh `{=id}`s, kept in `PilgrimageCampaignBehavior` (it owns the party
lifecycle; the abbot dialog class stays clergy-only).

## 4. Favour hooks

Both hang off `CampaignEvents.MapEventEnded`:

1. **Protection** — the player wins a map event against **bandits**, and one of our pilgrim
   parties (alive, not involved) is within a small radius of the battle (~5 map-distance units;
   verify a sensible constant against `MobileParty.SeeingRange` at implementation): the player
   gains `PilgrimProtectionRelation` (config, default 2) with each such party's home clergy
   notable, once per battle, with an on-screen message (*"The pilgrims of {SETTLEMENT} bless
   you for your protection."*, fresh `{=id}`). This is the favour-dilution fix: one deed moves
   one clergy relation directly.
2. **Attacking pilgrims is sacrilege** — a map event where the player attacked one of our
   pilgrim parties ends: `ChurchSacrilege.Apply(Hero.MainHero, homeSettlement)` (the existing
   −15 local / −5 cascade + message). No new tuning.

## 5. Map-module edits (sibling repo — special handling)

`DellarteDellaGuerraMap` is a separate module/repo outside this worktree. Rules for the
implementation run: make the XML edits in place (the game reads them live), **never commit
there**, and report the full diff for review. If the folder turns out not to be a git repo,
additionally save a copy of each edit as a patch note in the implementation report so nothing
is unrecoverable.

## 6. Configuration

Two new knobs in `<ChurchConfig>` (config/dadg.config.xml), same plumbing
(`ChurchConfig` → `ChurchSettings` → `ChurchSettingsConfig`), hot-reloaded:

| Element | Default | Meaning |
|---|---|---|
| `MaxPilgrimParties` | 3 | Max pilgrim bands alive at once (v0.5) |
| `PilgrimProtectionRelation` | 2 | Relation gained with a band's home clergy for defeating bandits near them (v0.5) |

Update the v0.2 doc's §4b table with these rows. Deliberately not configurable: spawn chance
(0.05/settlement/day), protection radius, shrine choice (that's data — the settlements XML).

## 7. Code layout

- `src\DellarteDellaGuerra.Domain\Church\Pilgrimage\PilgrimagePolicy.cs` — pure, plain values,
  no ports (like `DonationPolicy`/`MassPolicy`); + `PilgrimagePolicyTests`.
- `src\DellarteDellaGuerra.Domain\Church\Port\...` — `ChurchSettlementData.IsShrine`.
- `src\DellarteDellaGuerra.Infrastructure\Church\ChurchSettlementsXmlProvider.cs` — parse `shrine`.
- `src\DellarteDellaGuerra\Church\ChurchSettlements.cs` — `IsShrine`, shrine lookup.
- `src\DellarteDellaGuerra\Church\Api\Campaign\PilgrimageCampaignBehavior.cs` — spawn/travel/
  despawn, encounter dialog, both MapEventEnded hooks, SyncData.
- `ChurchSaveableTypeDefiner` — container registrations as needed.
- `ChurchConfig`/`ChurchSettings`/`ChurchSettingsConfig` + `config/dadg.config.xml` — two knobs.
- `DadgServiceContainer`/`DadgSubModule` — register the new behavior.
- Map module (sibling): monk civilian sets, preacher-notary NRE fix (§1, §5).

## 8. Verification

1. Unit: `PilgrimagePolicyTests` green; full Domain suite passes (30 existing + new).
2. Build Debug via `subst` short path; `dotnet test --no-build` (memory/worktree-build-quirks).
3. Static: XML well-formed (settlements, cultures, characters); all 16 church-village cultures
   have a non-commented `preacher_notary`.
4. Live playtest (deferred with v0.1–v0.4's): abbot in monk robes walking the village scene;
   pilgrim bands appear over a few in-game days, walk to Walsingham and home, despawn; talk
   encounter shows the pilgrim line; beating bandits next to a band prints the blessing message
   and +2 with that abbot; attacking a band triggers the sacrilege cascade; save/load mid-journey
   resumes travel.

## 9. Risks

- **Faction flip**: pilgrim `MapFaction` follows the abbey's current owner; at war with that
  kingdom, bandits-vs-pilgrims reads oddly and the player can engage them. Mitigation is the
  sacrilege hook; `ActualClan` neutrality is a v0.6+ option if playtest demands it.
- **Save-container registration**: `Dictionary<MobileParty, Settlement>` may need a container
  definition in `ChurchSaveableTypeDefiner`; forgetting it corrupts saves silently until load.
  Test save/load with a live pilgrim party before committing.
- **Map-module drift**: the sibling-repo XML edits are uncommitted by us (§5); until the user
  reviews/commits them, a Steam verify or repo reset could silently undo the monk look and,
  worse, re-expose the preacher-notary NRE.
- **Dialog collision**: pilgrim troops use villager templates, so the encounter-dialog condition
  must key on the party (StringId prefix `dadg_pilgrims_`), not the character, or normal
  villagers would greet as pilgrims.

## 10. As-built notes (1.4.7 reality)

Implementation revealed the live game is **Bannerlord 1.4.7 (War Sails)**, not 1.4.6 (reference
assemblies `1.4.7.117484`; APIs verified against the shipped, decompiled DLLs). Deviations from
this spec, all forced by 1.4.7:

- `CustomPartyComponent.CreateQuestParty` no longer exists → parties are created with
  `CreateCustomPartyWithPartyTemplate(...)`. It takes no troop limit; initial roster size comes
  from `PartySizeLimitModel`.
- The custom-party StringId is hardcoded (`quest_party_template_1`), so pilgrim bands are
  recognized via the saved `Dictionary<MobileParty, Settlement>` (party → home), not a StringId
  prefix — which also resolves the §9 dialog-collision risk (the condition keys on dictionary
  membership).
- Movement uses the 1.4.7 villager pattern:
  `AiHelper.GetBestNavigationTypeAndAdjustedDistanceOfSettlementForMobileParty` +
  `SetPartyAiAction.GetActionForVisitingSettlement` (handles land/naval routing).
- Event-ordering check (decompiled `MapEvent.FinalizeEventAux`): `MapEventEnded` is dispatched
  *before* wiped parties are destroyed, so the sacrilege hook sees a band the player annihilated.
- Culture audit: all 16 church villages are `Culture.empire`, whose `preacher_notary` is active —
  the commented-out entries sit in dead duplicate culture blocks, so the feared scene-entry NRE
  cannot occur and no cultures fix was needed. Monk garb was applied to the preacher notable
  templates and `preacher_notary_vlandia` in the map module (uncommitted there).
