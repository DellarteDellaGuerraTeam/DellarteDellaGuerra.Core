# DADG ships compared with installed War Sails

Audit date: 14 November 2026. Baseline: installed Bannerlord **1.5.7**, NavalDLC / War Sails **1.3.7**, not the 1.4.8 / 1.2.8 baseline used by the September–October playtests.

Follow-up repair status: fire visual/audio prefabs have since been added to all three live assemblies. Rudder and SDF source/cutover packages are prepared; native mesh imports and runtime checks remain pending. See the [repair report](<D:/SteamLibrary/steamapps/common/Mount & Blade II Bannerlord/Modules/DellarteDellaGuerra.Core/src/artifacts/ship-visual-fixes-20261114/REPORT.md>). Findings below describe the original audit snapshot. Stat-only upgrades are valid; visual variants are optional.

Scope: the three deployed DADG hull and mission definitions, their loose nested prefab XMLs, culture registration, ship upgrades, custom gun-crew logic, installed NavalDLC XML/prefabs, and selected classes decompiled directly from the installed NavalDLC DLL. No game assets or gameplay code were changed. This is a static audit, not a new runtime verification.

## Most important discrepancies

### 1. Buoyancy is configured on the wrong object for the current battle implementation

All three DADG `MissionShip` definitions specify `floating_force_multiplier="0.50"`, but none of their `ShipHull` definitions specifies it.

The installed `MissionShip.InitializeNavalPhysics` obtains buoyancy from `ShipOrigin.Hull.FloatingForceMultiplier`, not the mission object's value. The installed-version `ShipHull.Deserialize` defaults an omitted value to **0.451**. Civilian port ship spawning also passes the hull value to the visual ship factory. Consequently a campaign battle uses **0.451**, 9.8% less than the apparent intended 0.50. Actual waterline and stability still require measurement; this does not prove the ships sink.

Native hulls explicitly set this parameter: normal trade cog 1.0, war cog 0.5, roundship 0.62. These are hull-specific calibrations, not values to copy blindly.

First correction to consider: put the calibrated multiplier on each DADG hull, then verify both battle and port waterlines. Merely changing the mission XML will not affect this battle path.

Evidence: [DADG missions](<D:/SteamLibrary/steamapps/common/Mount & Blade II Bannerlord/Modules/DellarteDellaGuerraMap/ModuleData/dadg_custom_mission_ships.xml:4>), [DADG hulls](<D:/SteamLibrary/steamapps/common/Mount & Blade II Bannerlord/Modules/DellarteDellaGuerraMap/ModuleData/dadg_custom_ship_hulls.xml:7>), [current MissionShip decompile](<D:/SteamLibrary/steamapps/common/Mount & Blade II Bannerlord/Modules/DellarteDellaGuerra.Core/src/artifacts/ship-comparison-20261114/MissionShip.cs:3666>), [ShipHull deserializer](<D:/SteamLibrary/steamapps/common/Mount & Blade II Bannerlord/Modules/DellarteDellaGuerra.Core/src/artifacts/bannerlord-source-1.5.7-20261111/source/TaleWorlds.Core/TaleWorlds.Core/ShipHull.cs:146>).

### 2. All three sails have quest-scale health

Every DADG hull has **100,000 sail HP**. Comparable ordinary ships use:

- Fishing ship: 2,000 — English boat has **50×** this value.
- Trade cog: 8,000 — DADG kogge has **12.5×** this value.
- War cog: 9,300 — DADG kogge has **10.75×** this value.
- Roundship: 21,000 — DADG carrack has **4.76×** this value.

The same 100,000 value appears on native quest/story variants, including `ship_trade_cog_q1` and `ship_trade_cog_q3`. The DADG kogge also shares their 18,000 value, 3,600 cargo, 2.9 campaign speed and shallow-water permission, whereas the ordinary trade cog uses 13,000 / 11,000 / 3.3 / false. This suggests quest-template values were carried into a regular hull; that provenance is an inference, not a proven authoring history.

Effect: DADG rigging is disproportionately difficult to disable relative to hull durability. The tiny boat has the same sail-health pool as the five-sailed carrack.

### 3. One upgrade installs several guns, while the native ship API retains one weapon

The kogge has **3** `ShipBallista` falconets; the carrack has **5**. They are simultaneous children under the same `dadg_gun` upgrade slot and all carry the matching piece tag. These are not mutually exclusive upgrade alternatives.

However, the upgrade XML describes **one bow falconet**, links one siege-engine type, and charges for one piece. The installed `MissionShip.InitializeLists` stores only the last ranged siege weapon in `ShipSiegeWeapon`. Its deployment callback, model-calculated initial ammunition and helm direct-control paths use that single reference.

DADG already compensates for part of this with `ShipSwivelGunCrewMissionLogic`, registered in DI and added to missions. It assigns AI crew to additional guns and initializes their threat seekers. Therefore “the other guns never work” would be an incorrect conclusion. The remaining discrepancy is incomplete parity: the helper does not apply the native ship ammunition calculation to every extra gun, nor add a way to select each gun through the native helm weapon reference. Extra guns have prefab `StartingAmmoCount=30`. Campaign representation also remains one upgrade/siege-engine entry rather than an explicit 3/5-gun battery.

Evidence: [kogge gun assembly](<D:/SteamLibrary/steamapps/common/Mount & Blade II Bannerlord/Modules/DellarteDellaGuerraMap/NestedPrefabs/dadg_ws_kogge_ship_nested/control_point_holder.xml:280>), [carrack gun assembly](<D:/SteamLibrary/steamapps/common/Mount & Blade II Bannerlord/Modules/DellarteDellaGuerraMap/NestedPrefabs/dadg_ws_carrack_ship_nested/control_point_holder.xml:306>), [upgrade definition](<D:/SteamLibrary/steamapps/common/Mount & Blade II Bannerlord/Modules/DellarteDellaGuerraMap/ModuleData/dadg_ship_upgrade_pieces.xml:5>), [crew helper](<D:/SteamLibrary/steamapps/common/Mount & Blade II Bannerlord/Modules/DellarteDellaGuerra.Core/src/DellarteDellaGuerra.Integration/Naval/ShipSwivelGunCrewMissionLogic.cs:44>), `MissionShip.cs` lines 764–770, 4875–4878 and 5032–5059 in this evidence folder.

### 4. Burning and water-clipping systems are absent

None of the three custom assemblies contains `ShipBurningSystem`, `BurningNode`, `BurningSoundNode`, or `signed_distance_field`. The native fishing, trade cog, war cog and roundship assemblies contain these systems.

This has two distinct implications:

- Hull fire damage still works: current `MissionShip` reduces fire HP and triggers abandonment. Calls into the optional burning system are null-conditional. What is missing is the normal hull fire spread, lighting and sound presentation; it is not fire immunity. Sail burning meshes are present and are a separate mechanism.
- There is no authored SDF water clip in the custom assemblies. This leaves a risk of ocean water appearing through the hull interior. Existing `ship_water_effects`, `hull_water_mesh` and `render_to_depth` setup does not establish SDF coverage.

The [official ship creation guide](https://moddocs.bannerlord.com/war-sails/ship_creation/) describes the burning presentation and SDF water clipping separately. Verify the visual result in waves before assigning a runtime severity.

### 5. Sail upgrades change statistics without corresponding visual variants

All three hulls expose the native `sail` slot, but their custom prefabs have no `upgrade_slot` anchor tagged `sail`. The boat has no visual upgrade anchor at all; the kogge and carrack have only the gun anchor.

The hull XML comment acknowledges this design. The native slot/piece system can therefore award sail statistics while the DADG sail appearance stays the same. Native comparison hulls also expose more systems: trade cog deck/oars/sail; war cog aft/deck/oars/sail/side; roundship fore/aft/hull/deck/oars/sail. Figureheads are explicitly disabled on all three DADG hulls.

This is reduced visual variety and fewer upgrade choices, potentially intentional. **Stat-only upgrades are valid: there is no requirement that an upgrade change appearance.** The missing sail visual variants are optional presentation work, not a functional defect. Broader slot choices can also be added without new visuals when their effects are internal or subtle.

### 6. Rudder holders contain only empty stock markers

Each DADG `rudders_holder.xml` provides an empty `rudder_stock` entity. There is no rudder mesh in those holders and no `rudder_rotation_entity` tag anywhere in the three assemblies. Native trade cog, war cog and roundship assemblies have that tag.

Steering forces are still configured in mission XML, so missing visual wiring does not imply steering physics is absent. The discrepancy is the lack of a separately wired animated rudder; any rudder baked into the hull cannot rotate through this missing setup.

### 7. Boarding capacity is much more concentrated

Each custom ship has only **2 attachment machines and 2 receiving attachment-point machines**, one pair per side. The native trade cog has 6 launchers / 16 receivers, war cog 6 / 21, and roundship 8 / 20 in its serialized assembly.

That concentrates the carrack's much larger crew around fewer boarding access points and is a plausible contributor to congestion. It is not proof of a navigation bug. Both custom and comparison ships have two climbers, so climber count itself is not the discrepancy.

## Balance and role comparison

Values below are base XML values, before upgrades, skills or perks. Ordinary campaign hulls are used; quest variants are excluded.

| Hull | Total crew | Hull HP | Sail HP | Cargo | Base value | Campaign speed | Sea worthiness | Shallow water |
|---|---:|---:|---:|---:|---:|---:|---:|---|
| DADG English boat | 8 | 4,000 | 100,000 | 600 | 4,000 | 3.0 | 20 | Yes |
| Native fishing ship | 35 | 4,000 | 2,000 | 2,500 | 12,000 | 3.0 | 40 | Yes |
| DADG kogge | 30 | 16,000 | 100,000 | 3,600 | 18,000 | 2.9 | 32 | Yes |
| Native trade cog | 30 | 16,000 | 8,000 | 11,000 | 13,000 | 3.3 | 32 | No |
| Native war cog | 60 | 18,600 | 9,300 | 2,800 | 25,000 | 3.3 | 27 | No |
| DADG carrack | 100 | 24,000 | 100,000 | 3,000 | 36,000 | 2.8 | 36 | Yes |
| Native roundship | 108 | 42,000 | 21,000 | 4,000 | 72,000 | 3.2 | 38 | No |

Other discrepancies requiring design judgement:

- **Trade role:** DADG kogge is described as a trade workhorse, but omits `is_trade_ship` (schema default false), carries only 33% of a normal trade cog's cargo, and costs 38% more. The carrack also defaults to non-trade.
- **Cargo progression:** the carrack carries 3,000 versus the kogge's 3,600, despite being the larger ship class and carrying over three times the crew. This may be a warship-role decision.
- **Shallow water:** both kogge and carrack have access denied to native trade cog, war cog and roundship. A tall carrack's draft should be an explicit design decision.
- **Handling references:** the DADG kogge repeats the native war cog's declared 10.85 linear speed / 4.54 acceleration / 17.42 angular speed / 58.34 angular acceleration, despite a different hull, mass and no oars. The carrack declares 10 / 3.5 / 14 / 40 versus roundship 8.81 / 2.65 / 8.36 / 23.98. These XML references do not establish measured top speeds.
- **Mass progression:** kogge 67,778 kg versus carrack 46,974 kg, while native war cog is 60,000 kg and roundship 180,000 kg. Mass was derived from custom floater volumes; appearance/class alone does not prove the values wrong. Verify displaced volume and loaded stability before changing them.
- **No oars:** all DADG ships use `oar_count=0` and have no oar-machine components. This is internally consistent and may be historically intentional, but removes native rowing propulsion. Zero also violates the installed XSD's `positiveIntType`; earlier runs recorded this as a nonfatal warning. Do not add fake oars solely to silence validation.

## Correct wiring and superseded warnings

The following checks passed:

- Every custom hull resolves by name to a mission definition and a deployed prefab folder.
- Root physics mass matches mission mass: boat 1,334; kogge 67,778; carrack 46,974 kg.
- Sail definition and `sail_visual` counts agree: 1 / 1 / 5.
- Each root names a navmesh prefab whose `.bin` exists.
- All nine core root scripts found on the native comparison roots also exist on each custom root.
- All three DADG hulls are now listed under the English `empire` culture. The old report that they were only available through `vlandia` is superseded. Scottish cultures intentionally retain native northern hulls.
- English boat now references the **v4** sail skeleton/meshes. The old brain note saying it still uses the v1 sail is stale; its body still references the original body mesh.
- The September “no gameplay roots, no registration, no playable ships” handoff is superseded by current files and later battle evidence.
- Later playtest records contain the user's confirmation that the repainted sail cloth works as intended. The earlier missing/frozen-cloth failures should not be reported as current confirmed defects.
- There are boarding barriers embedded in attachment-machine prefabs. It would be inaccurate to claim no AI barriers exist merely because no top-level `barrier_holder.xml` exists.

## What remains unverified

No new battle, port, save/reload, cloth, gun, climbing or boarding test was performed on 1.5.7 / 1.3.7. Compiled TPAC geometry, material bindings and saved cloth payloads were not re-audited. Native prefab inheritance and alternative upgrade states are not flattened by the extraction script; serialized native entity/component counts are not runtime active-object counts.

The October deck-navigation note records rebuilt upper-deck navmeshes, steep (~62°) kogge stairs and a carrack stair/rail clipping concern. These are prior recorded risks, not newly reproduced failures. Navmesh file existence was checked; binary topology, agent clearance and boarding face placement were not revalidated.

Map `SubModule.xml` still declares Bannerlord 1.4.8 / NavalDLC 1.2.8 dependencies while the installed versions are 1.5.7 / 1.3.7. This is stale compatibility metadata and makes a fresh acceptance test especially useful.

Recommended order: resolve the hull buoyancy setting; choose normal sail HP; reconcile gun battery UI/ammunition/control with the 3/5-gun assemblies; finish fire/SDF/rudder presentation; then tune cargo, draft, handling and boarding density in a measured playtest.

Machine-readable extraction: [audit.json](<D:/SteamLibrary/steamapps/common/Mount & Blade II Bannerlord/Modules/DellarteDellaGuerra.Core/src/artifacts/ship-comparison-20261114/audit.json>). Reproducer: [audit.py](<D:/SteamLibrary/steamapps/common/Mount & Blade II Bannerlord/Modules/DellarteDellaGuerra.Core/src/artifacts/ship-comparison-20261114/audit.py>). Selected installed native prefab XML and installed-DLL decompiles are kept alongside this report.
