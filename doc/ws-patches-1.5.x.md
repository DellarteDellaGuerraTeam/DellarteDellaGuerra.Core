# War Sails / Bannerlord 1.5.x beta patches: what matters for DADG

Researched 2026-10-24. The live install is BL v1.4.8 build 119303 (WS v1.2.8).

## Status

- **1.5.x is still beta-only.** The newest build is **Beta WS v1.3.5 / BL v1.5.5, dated 09/10/26**. I found no live 1.5.x release notes.
  - Steam opt-in branch: "beta - Beta v1.5.0".
  - TaleWorlds says you can only access the latest beta build, and you cannot revert to an earlier build of an active beta. This is stated in the hotfix sticky's opening post (2023, still in force).
- **Beta builds and dates:**

| Build | Date | Source |
|---|---|---|
| WS v1.3.0 / BL v1.5.0 | 2026-08-19 | Steam announcement / TaleWorlds News/610 |
| WS v1.3.1 / BL v1.5.1 | 2026-08-21 | Steam hotfix sticky #48 |
| WS v1.3.2 / BL v1.5.2 | 2026-08-27, edited 14 Sep | Steam hotfix sticky #49 |
| WS v1.3.2 / BL v1.5.2 silent hotfix | 2026-09-01 | Steam hotfix sticky #50 |
| WS v1.3.3 / BL v1.5.3 | 2026-09-15, edited 5 Oct | Steam hotfix sticky #51 |
| WS v1.3.4 / BL v1.5.4 | 2026-10-05, edited 9 Oct | Steam hotfix sticky #52 |
| WS v1.3.5 / BL v1.5.5 | 2026-10-09 | Steam hotfix sticky #53 |

**Sources:**
- 1.5.0 full notes: Steam news API item "Beta Patch Notes - WS v1.3.0 / BL v1.5.0", 2026-08-19.
  - https://steamstore-a.akamaihd.net/news/externalpost/steam_community_announcements/1841579228662390
  - Mirror: https://www.taleworlds.com/en/News/610 (403 to bots, so I read the Steam copy).
- 1.5.1 to 1.5.5: posts by developer Piconi in the official sticky "Beta and 'Silent' Hotfix Patch Notes", page 4 (posts #48-#53).
  - https://steamcommunity.com/app/261550/discussions/0/3762228679799915128/?ctp=4
- The TaleWorlds forum mirror "Beta Patch Notes - v1.5.0 - v1.5.5" exists but is behind Cloudflare, so I could not read it.
  - https://forums.taleworlds.com/index.php?threads/beta-patch-notes-v1-5-0-v1-5-5.470975/
- 1.4.x context: Steam announcements for WS v1.2.5 to v1.2.8 (2026-05-21 to 2026-08-10).

Everything in sections 1-4 is **confirmed patch-note text**, quoted or closely paraphrased, unless it is marked *[speculation]* or *[DADG note]*. Section 5 lists community and third-party claims.

---

## 1. Sea-battle freezes, hangs and audio (FMOD / MissionSail)

**Verdict: no 1.5.x note mentions FMOD, sail sounds, MissionSail, SoundEvent, or a hang or freeze in naval battles.** Nothing in 1.5.0 through 1.5.5 addresses the DADG FMOD hang. In that hang, the FMOD Studio update thread spins while the game thread is blocked in `MissionSail.UpdateSoundPos` -> `SoundEvent.SetPosition`.

These are the closest confirmed items. None is a match.

| Build | Exact item | Relevance |
|---|---|---|
| BL 1.5.0 (2026-08-19) | "Fixed a crash that occurred due to incorrect soundtrack loading." | Music loading, not sail SFX. |
| BL 1.5.0 | "Fixed a bug that caused campaign sounds to persist after certain actions, like pressing ESC and continuing." | Campaign map only. |
| WS 1.3.0 (2026-08-19) | Naval battles now end with your faction's victory music instead of Battanian; "Battle music to disappear during the finale of the Naval Storyline" fixed. | Music cue selection. |
| BL 1.5.0 | "Fixed a crash that occurred while loading a siege mission, due to an error in thread timing." | A thread-timing fix, but in siege load. |
| WS 1.3.2 silent hotfix (2026-09-01) | "Optimized memory usage for Port Scenes and Naval Missions." | Naval missions, but memory only. |
| BL 1.5.2 silent hotfix (2026-09-01) | "Fixed a crash that occurred during missions due to a broken reference on agents." | Generic. |
| BL 1.5.3 (2026-09-15) | "Fixed a crash that occurred during cloth simulation." | Sails are cloth. The same wording was already in BL 1.4.7 (2026-07-08), so this is a second cloth crash. It is a crash, not a hang. |
| BL 1.5.3 | "Improved internal systems to better diagnose GPU-related crashes." | Diagnostics only. |
| WS 1.3.0 | "Fixed a bug that caused ship collisions to have numerous camera shakes or deal damage more than once." | Collision event spam. *[speculation]* Repeated collision events could also mean repeated collision SFX, but the notes don't say so. |

Earlier naval freeze and sound fixes are already in the live 1.4.8 build, so they don't explain the DADG hang:
- WS 1.1.0 beta (2025-12-12): "Fixed a bug that caused the game to freeze while a troop was climbing onto a sinking ship."
- WS 1.1.0: "Fixed a bug that caused some sounds not to play correctly during naval battles due to distance-based optimizations."
- BL 1.4.0/1.4.5 (2026-04-09 / 2026-05-21): "comprehensive optimization pass of all FMOD project assets". This release also expanded FMOD modding support and added a sample FMOD 2.02.x project.

*[DADG note]* A 1.5.x beta A/B test would not exercise a documented fix. The FMOD hang remains a DADG investigation item, and the vanilla-ship A/B test planned in memory `ws-fmod-sail-sound-hang` is still the next step. If it is reproduced on vanilla ships, it is worth a TaleWorlds bug report, since no fix for it has been announced.

## 2. Steering, helm, pilot AI, crewing, ship weapons

All items below are WS 1.3.0 (2026-08-19) unless another build is given.

- **Helm delegation fix:** "Fixed a bug that prevented crew members from taking control of the ship when delegating command of the vessel."
  - Ship-command delegation was introduced in WS 1.2.5.
  - *[DADG note]* This touches the same pilot/helm path (`ShipControllerMachine` / pilot standing point) that DADG ships use.
- **Skeletal crew relaxed in Order of Battle (OoB):**
  - Notes: "You can now deploy ships that have fewer troops than the skeletal crew number. The UI will alert you that such ships will underperform in battle."
  - Also: "Fixed a bug that allowed the naval battle to start even with undermanned ships deployed."
  - *[DADG note]* Check how the carrack's large crew requirement and the small boat behave under the new rule.
- **Deployment spacing is now dynamic:**
  - "maximum gap of 20 meters that scales down for wider formations in river battles (e.g., 13 meters for 8 dromons)".
  - River battles use a fixed zone depth with 50 m front and back margins. Sea battles still extend the zone toward the mission boundaries.
  - *[DADG note]* The carrack is larger than vanilla hulls, so watch for overlap or spawn collisions at the tighter river spacing.
- **Collisions:** "Fixed a bug that caused ship collisions to have numerous camera shakes or deal damage more than once."
- **Ballista and ship-weapon AI:**
  - BL 1.5.0: "Improved the aiming of AI troops while using the ballista."
  - BL 1.5.0: "Fixed a bug that caused mangonels and trebuchets to consistently miss their targets under certain configurations."
  - BL 1.5.3: "Fixed a bug that caused the Ballista camera to get distorted after zooming in."
  - *[DADG note]* The DADG cannon and swivel-gun AI derive from the siege-weapon/ballista paths. Re-verify aim after any upgrade.
- **Perks:**
  - "Made Boatswain - Shipwrights Insight's primary effect a personal effect, which now only applies when the hero who has the perk uses a ballista on a ship."
  - Most non-governor perks are now "Reduced ... by 50% when used in naval battles".
  - BL 1.5.3: "Chain to Oars" no longer causes an overmanned penalty.
- **Controller orders:** WS 1.3.4 (2026-10-05) fixed "the player formation in naval battles to receive an incorrect order when pressing R2 with a controller".
- **Upgrades and slots (cosmetic only):**
  - Improved upgrade descriptions.
  - Fixed "Reinforced Battlements with Improved Rudder" not appearing visually.
  - Fixed sail and rooftop colours not showing.
  - Fixed roof colour changing between LODs.
  - Fixed the top of the Square Sails (level 3) cloth ignoring wind.
- **Not found:** nothing about more than one weapon per ship, new upgrade-slot types, `ShipOrder` API changes, or pilot AI behaviour rework anywhere in 1.5.0-1.5.5.

## 3. Boarding, ship navmeshes, climbers/planks, ship modding

- **Destructible attachment ropes (WS 1.3.0):**
  - "Ship attachment ropes can now be hit by the player and destroyed to prevent bridges from being formed. Additionally, attachment rope physics have been overhauled and made more realistic."
  - *[DADG note]* This is the biggest boarding change. DADG's custom attach points, hook positions and bridge planks on the carrack, kogge and boat should be re-tested under the new rope physics.
- **Boarding fix (WS 1.3.0):** "Fixed a bug that prevented troops from boarding the enemy ship in some scenarios."
- **OoB placement (WS 1.3.0):** Fixed formation placement in unintended areas in Seaborne and Village raid OoB.
- **Mission-ship modding fix (BL 1.5.2, 2026-08-27):**
  - "Fixed a bug that prevented the editor's ship scripts from registering ship information from modules other than NavalDLC". This responds to the forum report "warsails-modding-kit-doesnt-load-customized-mission_ships-xml".
  - *[DADG note]* This directly affects editing DADG's custom ships (`mission_ships.xml` outside NavalDLC) in the Modding Kit. On 1.4.8, the editor ship scripts may not see DADG ship entries at all.
- **Other editor and modding items:**
  - BL 1.5.2: NestedPrefabs folder no longer deleted by "Publish Module".
  - BL 1.5.2: SceneEditData shipped for the river Seaborne Village Raid `aserai_village_m_navalraid`.
  - BL 1.5.3: Nested-prefab subfolders added.
  - BL 1.5.3: Terrain brush "Ignore Water Surface".
  - BL 1.5.3: Runtime terrain editing API, plus example script `ProceduralTerrainTestScript`.
  - BL 1.5.3: Copy/paste of per-LOD material properties.
  - BL 1.5.3: Occluder fix for optimized entities.
- **Navigation faces (BL 1.5.0):**
  - "Added the ability to enable or disable navigation faces with their indices."
  - It responds to the forum thread "more-pathfinding-options-for-mobileparty", so it is probably campaign-map focused. *[speculation]* It may also apply to mission navmeshes; the notes don't say.
- **Modding Kit and docs:** these came in BL 1.4.8 (2026-08-10), which is already live, not 1.5.
  - War Sails Modding Kit (Steam DLC "War Sails Modding Kit").
  - Doc pages:
    - https://moddocs.bannerlord.com/war-sails/ship_creation/
    - https://moddocs.bannerlord.com/war-sails/wsworldmap/
    - https://moddocs.bannerlord.com/war-sails/what_makes_seaborne_village_raid_scene/
  - I found no new ship doc pages in 1.5.x notes.
- **No changes found** to the ship prefab format, ship navmesh rules, climbing nets or climbers in 1.5.x.
  - Previous relevant fixes are already live: WS 1.1.1 fixed the rope-bridge navmesh, and WS 1.2.0 let multiple troops use climbing nets at once.

## 4. API, breaking and save changes for mods

**BL 1.5.0 (2026-08-19):**
- **Deployment contract (breaking for custom mission controllers):**
  - `OnDeploymentFinished` is now a mission-object and mission-behaviour callback for all mission types.
  - A custom `DeploymentMissionController` subclass must call `Mission.OnDeploymentFinished()`.
  - A custom flow controller without a deployment controller must call `Mission.OnInitialSpawnCompleted(BattleSide)`, either in `AfterMissionLoadingFinished()` or in `OnMissionTick()`.
  - *[DADG note]* Audit Cannons, ShipSwivelGunCrewMissionLogic and any custom mission flow code.
- **MapEvent refactor:**
  - "Map event-specific checks (e.g. IsFieldBattle and IsSallyOut) have been extracted into separate classes derived from MapEventComponent".
  - *[DADG note]* Harmony patches on `MapEvent` (for example PrivateWars same-kingdom battle logic) are likely to break.
- **WeaponFlags parsing:** "Weapon flags are no longer read as true when their value is given as false."
  - *[DADG note]* Any DADG item XML with a flag set to `false` currently acts as `true` on 1.4.8. Behaviour changes on 1.5.
- **Clan party stances replaced by behaviour toggles:** raid, donate troops, join armies, own a fleet. The clan-party recovery system was also added. Expect API changes around party stance.
- **Other:**
  - Stealth equipment can be defined in XML for non-player heroes.
  - Fast Mode is no longer a separate module (it moved into ASO).
  - Fixed a crash with `mpclassdivision.xml` merging.
  - Fixed a material-editor crash on uppercase texture names.

**BL 1.5.2:** New `engine_config.txt` option `keep_shader_cache_on_module_change`.

**BL 1.5.3 (2026-09-15):**
- **Breaking `native_parameters.xml` change:** `upper/lower_arc_when_defending_with_shield` (plus the hand-shield and horseback variants) were removed and replaced by direction-specific `..._defending_up/down/left_right_with_shield`.
  - Notes: "Modules that override the old parameter names in native_parameters.xml must update to the new names."
- Clearer XML error warnings.
- Encyclopedia crash on modded playthroughs fixed.

**BL 1.5.4 (2026-10-05):**
- **Kingdom naming:**
  - `Kingdom.EncyclopediaTitle` renamed to `Kingdom.FormalName` ("save slots unaffected").
  - `GetFormalNameForKingdom` removed.
  - `KingdomManager.CreateKingdom` lost its `encyclopediaTitle` parameter.
  - **`str_kingdom_formal_name` was removed from the XML, and "The game will assert on deserialize if it's still present".**
  - *[DADG note]* Grep DADG kingdom and string XML for `str_kingdom_formal_name`.
- **Visibility:** the `isDistanceDependent` parameter was removed from `GetMobilePartyVisibilityAndInspectedState`. It is replaced by `IsVisibilityPersistent`, and if you override that, you must set visibility manually.
- Fixed modded world maps failing to load after character creation.
- "Fixed a bug that caused an instance of save corruption."

**Save compatibility:** no 1.5.x note declares a save break or a migration for naval data. Remember that you cannot roll back an active beta build.

**Also affects DADG campaigns:**
- Land and sea party speed +15%.
- Reinforcement spawn path rework.
- Blood feuds and trait effects.
- Settlement security and loyalty rebalance (BL 1.5.4).

## 5. Community and third-party reports (not patch notes)

- **"Bugs with Beta 1.5.2/ war sails"**, Steam thread, opened 12 Sep:
  - It reports hideout and army crashes and dialogue loops.
  - No naval freeze or sound reports, and no developer reply.
  - https://steamcommunity.com/app/261550/discussions/0/500617242799732303/
- **Load hang on 1.5.3**, unverified: a search-engine snippet of the TaleWorlds forum thread 470975 mentions a loading hang on 1.5.3 with War Sails tied to "Granite Error ... DestroyCache", avoided by a driver reset. I could not open the thread itself (Cloudflare).
- **Third-party summaries** of the 1.5.0 beta (calradiawar.com, gamedropdaily.com, 2026-08/10) repeat the official notes and add nothing about naval audio:
  - https://calradiawar.com/news/bannerlord-v150-beta-update
  - https://gamedropdaily.com/updates/mount-and-blade-ii-bannerlord/
- **Pilot-behaviour freeze, unverified:** a search snippet attributed "fixed rare freeze in naval battles related to pilot behavior" to a Steam Workshop changelog, `sharedfiles/filedetails/changelog/3496296180`. That is a **mod's** changelog, not TaleWorlds, and I could not open it (HTTP 429). Treat it as unverified.
- **Reddit:** not reachable by the research tools, so nothing from r/mountandblade or r/Bannerlord was checked.

## 6. Priorities if DADG moves to 1.5.x

1. Re-test boarding on all three DADG ships under destructible, physics-overhauled attachment ropes.
2. Check helm delegation and pilot seating on the DADG ships (the WS 1.3.0 delegation fix).
3. Fix the API breaks:
   - `OnDeploymentFinished` / `OnInitialSpawnCompleted` contract.
   - `MapEventComponent` refactor versus DADG/PrivateWars Harmony patches.
   - `str_kingdom_formal_name` / `Kingdom.FormalName`.
   - Shield-arc `native_parameters` names.
   - `WeaponFlags` `false` semantics.
4. Modding Kit: with the 1.5.2 fix, the editor should finally register DADG `mission_ships.xml` ships from outside NavalDLC.
5. The FMOD hang still needs its own investigation. No patch claims a fix.
