# DADG Feature Domains

Inventory of every gameplay feature, where the C# code lives, and key implementation notes.

## Firearms
- **Multi-phase black-powder reload**: `DellarteDellaGuerra/Firearm/Reload/` — components for ramming (`RammingReloadComponent`), initial hand-swap (`InitialHandSwapReloadComponent`), etc. `WeaponReloadPhaseComponent` drives the phase sequence.
- **Smoke effects**: `FirearmSmokeMissionLogic`
- **Skill registration**: `Infrastructure/Firearm/FirearmSkillProvider` + `AddFirearmSkillAsRelevantSkillPatch`
- Registered as `MissionBehavior` in `SubModule.OnBeforeMissionBehaviorInitialize`

## Cannons / Siege Engines
- **Fully data-driven** — adding a cannon requires only XML changes, no C#. The falconet is the reference cannon.
- XML root: `../DellarteDellaGuerra/ModuleData/CustomXml/cannons.xml`
- Supporting XML: `Cannons/siege_engines.xml`, `cannon_items.xml`, `physics_materials.xml`, `collision_infos.xml`, `sounds.xml`
- Prefabs: `../DellarteDellaGuerra/Prefabs/cannons.xml` — three prefabs per cannon: `dadg_<id>`, `dadg_<id>_mapicon`, `dadg_<id>_spawner`
- UI sprites: `../DellarteDellaGuerraMap/GUI/DadgCannonsSpriteData.xml`, atlas `dadg_ui_cannon_1_tex.tpac` (256×256 px)
- C# side: `Infrastructure/SiegeEngines/CannonRepository` reads XML; `Integration/SiegeEngines/Campaign/DadgSiegeEventModel` + `DadgSiegeStrategyActionModel` inject into Bannerlord's siege framework
- **Critical gotcha**: cannon spawner must face **away from walls** (forward = toward camp). `clean` entity needs `rotation_euler Z = 3.14159`. Symptom if missing: cannon snaps 180° and points away from walls at battle start. See `doc/adding-a-cannon.md` Rule 4.
- Scene placement: `../DellarteDellaGuerraScenes/SceneObj/<name>/scene.xscene` — place `dadg_<id>_spawner` within range of a `siege_deployment_placeholder`

## Tournament
- `Domain/Tournament/Reward/` — `GetTournamentRewardUseCase` determines prize based on town prosperity + troop type
- `DellarteDellaGuerra/Tournament/Api/DadgTournamentModel` — overrides native tournament model via `InitializeGameStarter`
- **Jousting subsystem**: `DellarteDellaGuerra/Tournament/Jousting/` — `JoustTournament`, `JoustTournamentBehaviour`, `JoustFightMissionController`, `JoustingMissionManager`, `JoustLaneEndVolumeBox`; its own save types (`JoustingTournamentSaveableTypeDefiner`)

## Private Wars
- A **same-kingdom** war between two clans (a feudal feud), driven entirely by DADG since the engine never wages war within a kingdom.
- **Domain** (`Domain/PrivateWars/`): `DeclarePrivateWarUseCase` opens a war; `TickPrivateWarUseCase` + `PrivateWarScoreCalculator` score it daily; `ResolvePrivateWarUseCase` resolves it at score ±100 (winner keeps the goal fief, other captured fiefs revert); `ApplyBattleOutcomeUseCase` is the sole mutator of `BattleScore` (capped ±50). Models: `PrivateWar` (record), `PrivateWarStatus { Active, Concluded }`, `WarSide`.
- **Side resolution**: `WarSideResolver` walks a clan UP its suzerain chain to the nearest belligerent principal, so subtree vassals are pulled in dynamically (call-to-arms) and re-resolved on every query — never stored.
- **Registry** (`Infrastructure/PrivateWars/InMemoryPrivateWarRegistry`): implements both `IPrivateWarRepository` and `IPrivateWarHostility`. `AreEnemies` only considers `Active` wars, so concluding a war immediately lifts the hostility signal.
- **Campaign wiring** (`DellarteDellaGuerra/PrivateWars/Api/Campaign/PrivateWarCampaignBehavior`): `OnDailyTick` ticks→resolves each war; `OnAiHourlyTick` injects the goal fief as a scored besiege candidate into the engine's behaviour vote (the stock siege planner never enumerates a same-kingdom settlement); `OnMapEventEnded` bumps `BattleScore` when a battle resolves between the two sides.
- **Prisoner retention** (`Integration/PrivateWars/Patches/PrivateWarPrisonerRetentionPatch`): blocks the vanilla release sweep from freeing a same-kingdom private-war captive while the war is `Active`.

### War lifecycle / state integrity
- A war is `Active` until it either **resolves on score** (±100, via `ResolvePrivateWarUseCase` — fiefs change hands) or is **auto-concluded** for integrity reasons (below). `Concluded` is terminal — a war is never revived.
- **Kingdom-change rule** (`OnClanChangedKingdom` in `PrivateWarCampaignBehavior`): a private war is inherently a same-kingdom affair, so when a war **principal** changes kingdom and the two principals no longer share a `MapFaction` (`Clan.MapFaction` is the kingdom, or the clan itself when independent), the war is set to `Concluded`. Only wars where the moved clan is a **principal** are touched (`GetByClan`); a non-principal vassal moving does nothing, and its side is re-resolved dynamically. If both principals still share a faction after the move (e.g. they defect to the same new kingdom together), the war stays `Active`.
- This conclusion does **not** revert captured fiefs (reverts are reserved for score-threshold resolution) — it is purely a state-integrity close. Its real effect comes from `Status` flipping off `Active`: the AI hostility drive (`AreEnemies`) and the prisoner-retention hold both stop, so the registry can no longer carry a stale cross-kingdom "private" war or hold same-kingdom captives for a war that no longer makes sense.

## Music (PSAI)
- PSAI = Phase System Audio Integration, Bannerlord's music engine
- `Integration/Music/Patches/MBMusicManagerInitializePatch` patches `PsaiCore.LoadSoundtrackFromProjectFile` to load DADG's custom `soundtrack.xml`
- **Applied in `SubModule` constructor** before DI is built, because PSAI loads before `OnSubModuleLoad`
- Skill `bannerlord-music-classify` converts audio → OGG, classifies for PSAI (Battle/Losing/Campaign/Dramatic), and updates `soundtrack.xml` entries
- `soundtrack.xml` lives in the `DellarteDellaGuerra` content module

## POC Integration
- POC = Bannerlord POC mod (banner & heraldry customisation)
- Custom banners for all English clans/kingdoms; randomised armour colours
- `Infrastructure/Poc/Patches/PocConfigReaderOverriderPatch` redirects POC's config read path → `../config/poc.config.json`
- Works whether POC is bundled inside DADG or is an external module (Steam Workshop compatible)
- Full docs: `doc/poc.integration.md`

## Display Compiling Shaders
- Shows remaining shader count in the UI during game load
- `Domain/DisplayCompilingShaders/DisplayShaderNumber` is the use case
- `DellarteDellaGuerra/DisplayCompilingShaders/CompilingShaderNotifier` — a `GameHandler` that polls and updates the HUD
- Config-driven throttle: `CompilingShaderNotifierConfig` reads from `DadgConfigWatcher`

## Steam File Path Fixes
- `Infrastructure/Steam/Patches/FixSettlementFilePathPatch` — fixes settlement XML path for Steam Workshop
- `Infrastructure/Steam/Patches/FixSettlementDistanceCacheFilePathPatch` — fixes distance cache path

## Main Menu
- `DellarteDellaGuerra/MainMenu/DadgCampaignStartButtonAdder` — adds the DADG-specific campaign start button
- `DellarteDellaGuerra/MainMenu/VanillaCampaignButtonsRemover` — hides vanilla campaign options

## Character Creation
- `Infrastructure/CharacterCreation/Patches/DisableSortingBehaviourInCultureMenuPatch` — prevents native sorting from reordering culture options

## Campaign Behaviour Disabler
- `DellarteDellaGuerra/DisableNativeBehaviour/MissionBehaviours/CampaignBehaviourDisabler` — disables specific native campaign behaviours that conflict with DADG
- Called in `SubModule.OnGameInitializationFinished`

## Scene
- `DellarteDellaGuerra/Scene/ClothSimulationActivatorMissionLogic` — activates cloth physics on mission start

## Custom Battle Scenes
- `SubModule.LoadDadgBattleScenes()` calls `GameSceneDataManager.LoadSPBattleScenes` with DADG's battle scene file, overriding SandBox defaults
- Path resolved via `Infrastructure/Utils/ResourceLocator.GetBattleScenesFilePath()`

## Campaign Start Date
- Hardcoded to 1471 in `SubModule.SetCampaignStartingDate()` via reflection on `CampaignData.CampaignStartTime`
