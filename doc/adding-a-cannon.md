# Adding a New Cannon to DADG

The cannon system is entirely data-driven. Adding a new cannon requires no C# code changes — only XML configuration, 3D assets, UI sprites, and scene editor work.

## How It Works

At startup the mod reads `ModuleData/CustomXml/cannons.xml`, validates each entry, and dynamically registers every valid cannon. The system then:

1. Injects cannons into Bannerlord's siege engine framework (replacing native engines on the campaign map)
2. Creates a distinct script type (`GenericCannon_<id>`) at runtime so the scene editor can reference it
3. Links that script type back to the XML configuration for icons, prefabs, and animations

The falconet is the reference implementation. When in doubt, copy it and replace `falconet` with your cannon's id.

---

## Prerequisites

- **Bannerlord Modding Kit** — required for scene editing (placing spawners) and asset compilation (`.tpac` files)
- **BannerEdge** — required for editing the UI sprite atlas (`dadg_ui_cannon_1_tex.tpac`)
- **FBX-compatible 3D software** — Blender, Maya, or equivalent (for meshes and physics shapes)
- **Image editor** — for creating the three cannon icon PNGs at exact required dimensions
- **Audio editor** — only if adding unique sounds
- Familiarity with Bannerlord's scene editor

---

## Step 1 — Register the Cannon

**File:** `Modules/DellarteDellaGuerra/ModuleData/CustomXml/cannons.xml`

Every cannon must have an entry here. This is the root of the system — all other data links back to the `Id` defined here.

```xml
<Cannons>
    <Cannon>
        <Id>culverin</Id>
        <DisplayName>Culverin</DisplayName>
        <SiegeDeploymentSelectionIconSpriteId>Order\SiegeIcons\culverin</SiegeDeploymentSelectionIconSpriteId>
        <MapSiegeMarkerSpriteId>SPGeneral\MapSiege\culverin</MapSiegeMarkerSpriteId>
        <CampaignMapSelectionIconSpriteId>SPGeneral\Siege\culverin</CampaignMapSelectionIconSpriteId>
        <CampaignMapPrefabName>dadg_culverin_mapicon</CampaignMapPrefabName>
        <CampaignMapProjectilePrefabName>cannonball_mapicon_projectile</CampaignMapProjectilePrefabName>
        <CampaignMapReloadAnimationName>ballista_a_mapicon_reload</CampaignMapReloadAnimationName>
        <CampaignMapFireAnimationName>ballista_a_mapicon_fire</CampaignMapFireAnimationName>
        <MachineType>9</MachineType>
        <CampaignMapProjectileBoneIndex>0</CampaignMapProjectileBoneIndex>
    </Cannon>
</Cannons>
```

### Parameter Reference

| Field | Type | Constraints | Description |
|---|---|---|---|
| `Id` | string | Start with a letter; `[a-zA-Z0-9_]` only | Unique internal key. Cross-referenced across all other files. |
| `DisplayName` | string | Non-empty | Human-readable name shown in UI tooltips. |
| `SiegeDeploymentSelectionIconSpriteId` | string | Non-empty | Sprite path shown in the in-battle deployment order panel. |
| `MapSiegeMarkerSpriteId` | string | Non-empty | Sprite path shown on the campaign map siege overlay. |
| `CampaignMapSelectionIconSpriteId` | string | Non-empty | Sprite path shown in the campaign map siege engine selection panel. |
| `CampaignMapPrefabName` | string | Non-empty; must exist in Prefabs XML | Name of the campaign map 3D prefab entity (the `_mapicon` prefab). |
| `CampaignMapProjectilePrefabName` | string | Non-empty | Name of the projectile prefab used in the campaign map fire animation. `cannonball_mapicon_projectile` is shared and can be reused. |
| `CampaignMapReloadAnimationName` | string | Non-empty | Skeleton animation played on the campaign map icon during reload. `ballista_a_mapicon_reload` works for most cannons. |
| `CampaignMapFireAnimationName` | string | Non-empty | Skeleton animation played on the campaign map icon when firing. `ballista_a_mapicon_fire` works for most cannons. |
| `MachineType` | int | > 7; unique per cannon | Numeric identifier used by the native Bannerlord UI to look up machine icons. Bannerlord reserves 0–7 for its native siege engine types in the campaign map UI (0 = wall, 1 = broken_wall, 2 = ballista, 3 = trebuchet, 4 = ladder, 5 = ram, 6 = tower, 7 = mangonel). Custom cannons must use 8 or higher. The falconet uses 8; each additional cannon must use a unique integer (9, 10, …). |
| `CampaignMapProjectileBoneIndex` | int | >= 0 | Bone index on the campaign map icon skeleton that the projectile entity's transform is derived from during the fire animation. Bannerlord's native engines use: Onager/Catapult → 2, Ballista → 7, Trebuchet → 4, Bricole → 20; unknown types default to −1. DADG cannons use `0` (root bone of `trebuchet_a_mapicon_skeleton`), which is correct when reusing the shared skeleton and ballista animations — the `projectile_position` child entity in the mapicon prefab already defines the visual spawn point relative to the root. Only use a non-zero value if you create a custom skeleton with a dedicated muzzle bone. |

### Validation

The system validates every entry at startup. Invalid cannons are logged and silently skipped — the game still loads. Check the Bannerlord log for lines like:

```
[WARN] Cannon 'culverin': MachineType must be greater than 7.
[INFO] Loaded cannon: culverin
```

Validation rules:
- `Id` must match `^[a-zA-Z][a-zA-Z0-9_]*$`
- All string fields must be non-null and non-empty
- `MachineType > 7`
- `CampaignMapProjectileBoneIndex >= 0`

---

## Step 2 — Define Siege Engine Campaign Stats

**File:** `Modules/DellarteDellaGuerra/ModuleData/Cannons/siege_engines.xml`

Controls how the cannon behaves on the campaign map during sieges. The `id` must exactly match the `Id` from Step 1.

```xml
<SiegeEngineTypes>
    <SiegeEngineType
        id="culverin"
        name="Culverin"
        description="The culverin is a long-barrelled cannon effective at range, capable of punishing troops and lightly damaging fortifications."
        is_ranged="true"
        damage="300"
        max_hit_points="1000"
        hit_chance="0.45"
        is_anti_personnel="true"
        anti_personnel_hit_chance="0.20"
        man_day_cost="18"
        difficulty="55"
        campaign_rate_of_fire_per_day="15.0"/>
</SiegeEngineTypes>
```

### Parameter Reference

| Attribute | Type | Description |
|---|---|---|
| `id` | string | Must match `Id` in `cannons.xml`. |
| `name` | string | Display name in the siege UI. |
| `description` | string | Tooltip text shown to the player. |
| `is_ranged` | bool | Always `true` for cannons. |
| `damage` | int | Siege damage dealt to fortifications per shot on the campaign map. |
| `max_hit_points` | int | Cannon durability; destroyed when this reaches zero. |
| `hit_chance` | float (0–1) | Base probability of hitting the target per shot (0.52 = 52%). |
| `is_anti_personnel` | bool | Whether the cannon can target troop formations. |
| `anti_personnel_hit_chance` | float (0–1) | Hit probability specifically when targeting troops. |
| `man_day_cost` | int | Campaign supply cost per day. Higher values make the cannon more expensive to maintain. |
| `difficulty` | int | AI skill threshold required to operate this engine. Higher values mean only skilled AI will use it. |
| `campaign_rate_of_fire_per_day` | float | Shots per in-game day on the campaign map. |

**Falconet reference values:** `damage=200`, `hit_chance=0.52`, `man_day_cost=12`, `campaign_rate_of_fire_per_day=20.0`

---

## Step 3 — Define the Battle Projectile

**File:** `Modules/DellarteDellaGuerra/ModuleData/Cannons/cannon_items.xml`

Defines the cannonball as a game item that the cannon fires during battle missions. The `id` here is referenced as `MissileItemID` in the prefab (Step 7).

```xml
<Items>
  <Item id="culverin_cannonball" name="{=!}Culverin Cannonball"
        is_merchandise="false"
        body_name="bo_projectile_rock"
        holster_body_name="bo_axe_short"
        flying_mesh="small_cannonball"
        mesh="small_cannonball"
        holster_mesh=""
        subtype="arrows"
        weight="20"
        difficulty="0"
        appearance="1"
        Type="Thrown"
        item_holsters="">
    <ItemComponent>
      <Weapon
        weapon_class="Boulder"
        ammo_class="Boulder"
        stack_amount="1"
        position="0.05, -0.15, 0"
        physics_material="cannonball"
        thrust_speed="100"
        speed_rating="0"
        missile_speed="8"
        weapon_length="95"
        thrust_damage="150"
        item_usage="heavy_stone"
        flying_sound_code="event:/mission/combat/boulder/passby"
        trail_particle_name="psys_cannonball_trail"
        rotation_speed="2, 0.5, 0">
        <WeaponFlags
          RangedWeapon="true"
          NotUsableWithOneHand="true"
          TwoHandIdleOnMount="true"
          CanPenetrateShield="true"
          MissileWithPhysics="true"
          UseHandAsThrowBase="true"
          Consumable="true"
          LeavesTrail="true"
          CanKnockDown="true"
          MultiplePenetration="true"
          AffectsArea="true"/>
      </Weapon>
    </ItemComponent>
    <Flags QuickFadeOut="true" CannotBePickedUp="true" DropOnWeaponChange="true" DropOnAnyAction="true"/>
  </Item>
</Items>
```

### Key Attributes

| Attribute | Description |
|---|---|
| `id` | Must be set as `MissileItemID` in the prefab script (Step 7). Convention: `<cannon_id>_cannonball`. |
| `physics_material` | References a `physics_materials.xml` entry. Use `cannonball` to share existing physics with the falconet. |
| `thrust_damage` | Direct impact damage on hit. Falconet uses `125`. |
| `missile_speed` | Projectile flight speed multiplier. |
| `weapon_class` / `ammo_class` | Always `Boulder` for cannon projectiles. |
| `flying_mesh` / `mesh` | The 3D mesh shown in flight and when loaded. `small_cannonball` is shared. |
| `trail_particle_name` | Particle system trailing the projectile in flight. `psys_cannonball_trail` is shared. |
| `WeaponFlags.AffectsArea` | Enables area-of-effect splash damage on impact. |
| `WeaponFlags.MultiplePenetration` | Allows the ball to pass through multiple agents. |
| `WeaponFlags.CanKnockDown` | Knocks agents to the ground on hit. |

Unless the new cannon needs a significantly different projectile (e.g. explosive shell), `small_cannonball` mesh and `cannonball` physics material can be reused without modification.

---

## Step 4 — Physics Material (Optional)

**File:** `Modules/DellarteDellaGuerra/ModuleData/Cannons/physics_materials.xml`

Only needed if the cannon's projectile should have different bounce or friction characteristics. All current cannons share the `cannonball` material, so this step can be skipped.

```xml
<physics_material
    id="cannonball"
    dont_stick_missiles="true"
    static_friction="0.400"
    dynamic_friction="0.200"
    restitution="0.600"
    softness="0.000"
    linear_damping="0.500"
    angular_damping="0.100"
    display_color="255, 98, 79, 166" />
```

| Attribute | Description |
|---|---|
| `dont_stick_missiles` | Projectile bounces off surfaces instead of embedding. |
| `restitution` | Bounciness: `0` = dead stop, `1` = perfect bounce. `0.6` gives a moderate roll. |
| `linear_damping` | How quickly the ball slows after each bounce. |
| `angular_damping` | How quickly the spin decays. |
| `static_friction` / `dynamic_friction` | Surface friction when stationary or sliding. |

---

## Step 5 — Collision Impact Effects (Optional)

**File:** `Modules/DellarteDellaGuerra/ModuleData/Cannons/collision_infos.xml`

Specifies the particle effect and decal spawned when the projectile hits each material type. All cannons currently share the `cannonball` physics material identifier, so they share these definitions too.

No changes are needed unless you want a distinct visual impact effect (e.g. fire, sparks instead of explosion). If you define a new `physics_material` id in Step 4, add a matching `<material id="...">` block here.

```xml
<material id="cannonball">
    <collision_info second_material="stone">
        <collision_effect particle="cannon_explosion" decal="fire_damage"/>
    </collision_info>
    <collision_info second_material="wood">
        <collision_effect particle="cannon_explosion" decal="fire_damage_a_mat"/>
    </collision_info>
    <!-- one entry per surface type -->
</material>
```

Supported surface types: `metal_weapon`, `metal`, `wood_shield`, `wood`, `wood_weapon`, `stone`, `mud`, `grass`, `default`, `path`, `snow`, `sand`, `flesh`, `straw`, `lambert1`, `adobe`, `soil`, `wood_nonstick`.

---

## Step 6 — Create the 3D Assets

The cannon needs Bannerlord-format compiled assets.

### File Locations

| Location | Purpose |
|---|---|
| `Modules/DellarteDellaGuerra/AssetSources/cannons/cannons.fbx` | Source FBX — add your meshes here |
| `Modules/DellarteDellaGuerra/Assets/cannons/cannons_geo.tpac` | Compiled geometry — regenerated by the Modding Kit |

Open `cannons.fbx` in your 3D application, add your meshes, export, and then use the Bannerlord Modding Kit asset compiler to regenerate the `.tpac` files.

### Required Meshes

Follow the falconet naming pattern, replacing `falconet` with your cannon's `id`:

| Mesh name | Description |
|---|---|
| `dadg_siege_<id>_base` | Carriage and wheeled frame — the main body. |
| `dadg_siege_<id>_barrel` | The barrel; this entity pivots on the elevation axis. |
| `dadg_siege_<id>_wheel_r` | Right wheel. |
| `dadg_siege_<id>_wheel_l` | Left wheel. |

These are shared and do not need to be recreated:

| Mesh name | Description |
|---|---|
| `small_cannonball` | Projectile visual (spherical iron ball). |
| `mangonel_rock_pile` | Ammo pile placed next to the cannon. |

### Required Physics Shape

| Shape name | Description |
|---|---|
| `bo_dadg_siege_<id>` | Collision geometry for the cannon body. Referenced in the prefab's `<physics shape="...">` attribute. |

Physics shapes are low-polygon collision meshes created alongside the visual mesh in the FBX and compiled the same way.

### Particle Systems

These already exist and can be reused directly:

| Name | Description |
|---|---|
| `psys_cannon_shot_1` | Muzzle flash and smoke on firing. |
| `psys_cannonball_trail` | Smoke trail following the projectile in flight. |
| `cannon_explosion` | Impact explosion effect. |

---

## Step 7 — Define the Prefabs

**File:** `Modules/DellarteDellaGuerra/Prefabs/cannons.xml`

Three prefab entities are required per cannon. Copy the falconet blocks and replace every instance of `falconet` with your cannon's `id`.

### 7a. Main Battle Cannon — `dadg_<id>`

The fully interactive cannon used in siege and battle missions. The script name `GenericCannon_<id>` is created automatically at runtime based on the `Id` in `cannons.xml` — you do not need to write any C#.

**Hierarchy:**

```
dadg_<id>                               root entity
│   tag: machine_parent
│   script: SynchedMissionObject
│
└── <id>_cannon_body
        script: GenericCannon_<id>      (auto-created; configure via variables below)
        script: DestructableComponent
    │
    ├── clean                           operational state (visible during battle)
    │   tag: operational
    │   tag: Battery_Base
    │   rotation_euler: 0, 0, 3.14159   ← required; see AI orientation note in Step 8
    │   physics shape: bo_dadg_siege_<id>
    │   mesh: dadg_siege_<id>_base
    │   script: SynchedMissionObject
    │   │
    │   ├── barrel
    │   │   tag: Barrel
    │   │   mesh: dadg_siege_<id>_barrel
    │   │   script: SynchedMissionObject
    │   │   │
    │   │   ├── projectile_leaving_position     muzzle exit point
    │   │   │       position: at muzzle end, rotation Z=3.141 (faces forward)
    │   │   │
    │   │   └── projectile_boulder              loaded cannonball visual
    │   │           tag: projectile
    │   │           tag: <id>_cannonball
    │   │           mesh: small_cannonball
    │   │
    │   ├── use_reload_fire_l           operator standing point
    │   │   tag: Pilot
    │   │   visibility: editor-only
    │   │
    │   ├── waiting_pos                 crew standby position
    │   │   tag: Wait
    │   │   tag: can_pick_up_ammo
    │   │   visibility: editor-only
    │   │
    │   ├── targeting_volume            AI targeting volume (invisible sphere)
    │   │   tag: targeting_entity
    │   │   scale: 4, 4, 4
    │   │   mesh: barrier_sphere (material: ghost)
    │   │
    │   ├── use_load                    ammo loader position
    │   │   tag: ammoload
    │   │   visibility: editor-only
    │   │
    │   ├── wheel_R
    │   │   tag: Wheel_R
    │   │   mesh: dadg_siege_<id>_wheel_r
    │   │
    │   └── wheel_L
    │       tag: Wheel_L
    │       mesh: dadg_siege_<id>_wheel_l
    │
    └── destroyed                       destroyed state (hidden by default)
        tag: destroyed
        visible: false
        │
        ├── particles                   destruction effect particle
        ├── destroyed_base              tipped-over base (transform: rotated/offset)
        ├── barrel                      fallen barrel
        ├── wheel_R                     fallen right wheel
        └── wheel_L                     fallen left wheel

projectile_pile                         ammo pile (sibling of <id>_cannon_body)
    mesh: mangonel_rock_pile
    physics override_material: wood
    script: CannonBallPile
    │
    └── ammo_pos_g
            tag: ammopickup
            script: AmmoPickUpStandingPoint
```

#### `GenericCannon_<id>` Script Variables

| Variable | Example Value | Description |
|---|---|---|
| `SiegeEngineId` | `culverin` | Must match `id` in `siege_engines.xml`. |
| `DisplayName` | `{=!}Culverin` | Name shown in the mission HUD. Use `{=!}` prefix to skip localisation lookup. |
| `MissileItemID` | `culverin_cannonball` | Must match `id` in `cannon_items.xml`. |
| `BaseMuzzleVelocity` | `120.0` | Launch speed in m/s. Higher = flatter trajectory and longer range. |
| `startingAmmoCount` | `20` | Rounds available at start before the ammo pile runs out. |
| `TopReleaseAngleRestriction` | `0.200` | Maximum elevation angle in radians (~11.5°). |
| `BottomReleaseAngleRestriction` | `-0.100` | Maximum depression angle in radians (~-5.7°). |
| `FireSoundID` | `mortar_shot_1` | Primary fire sound event name. |
| `FireSoundID2` | `mortar_shot_2` | Alternate fire sound (randomly selected). |
| `CannonShotExplosionEffect` | `psys_cannon_shot_1` | Muzzle particle system. |
| `Focus` | `Troops` | AI targeting preference: `Troops` or `Structures`. |
| `PreferHighAngle` | `false` | `true` enables plunging/mortar-style arc; `false` uses flat direct fire. |
| `RecoilDuration` | `0.100` | Seconds for the initial recoil kick. |
| `Recoil2Duration` | `0.800` | Seconds for the secondary settle-back motion. |
| `SlideBackFrameFactor` | `0.600` | Recoil intensity: `0` = none, `1` = maximum. |
| `WheelRotationAxis` | `X` | Axis the wheels rotate around during movement. |
| `PilotStandingPointTag` | `Pilot` | Tag of the operator's standing point entity. |
| `AmmoPickUpTag` | `ammopickup` | Tag of the ammo pickup standing point entity. |
| `WaitStandingPointTag` | `Wait` | Tag of the crew wait standing point entity. |
| `IdleActionName` | `act_usage_mangonel_big_idle` | Crew idle animation. |
| `ShootActionName` | `act_usage_mangonel_big_shoot` | Crew fire animation. |
| `Reload1ActionName` | `act_usage_mangonel_big_reload` | Crew reload animation (phase 1). |
| `Reload2ActionName` | `act_usage_mangonel_reload_2` | Crew reload animation (phase 2). |
| `RotateLeftActionName` | `act_usage_mangonel_rotate_left` | Crew rotate animation. |
| `RotateRightActionName` | `act_usage_mangonel_rotate_right` | Crew rotate animation. |
| `LoadAmmoBeginActionName` | `act_usage_mangonel_big_load_ammo_begin` | Ammo loader start animation. |
| `LoadAmmoEndActionName` | `act_usage_mangonel_big_load_ammo_end` | Ammo loader finish animation. |
| `Reload2IdleActionName` | `act_usage_mangonel_reload_2_idle` | Crew idle during reload phase 2. |

All animation names above are shared from the native Bannerlord mangonel rig and work for all wheeled field cannons.

#### `DestructableComponent` Script Variables

| Variable | Example Value | Description |
|---|---|---|
| `DestructionStates` | `destroyed` | Name of the sub-entity that becomes visible when destroyed. Must match the child entity named `destroyed`. |
| `MaxHitPoint` | `350.0` | HP before destruction. Falconet uses `350`. |
| `SoundEffectOnDestroy` | `event:/mission/siege/mangonel/break` | Audio event played on destruction. |
| `DestroyedByStoneOnly` | `false` | `true` = only stone projectiles can destroy it. |
| `CanBeDestroyedInitially` | `false` | Whether the cannon can spawn pre-destroyed. Always `false`. |
| `PassHitOnToParent` | `false` | Always `false` for cannon bodies. |

---

### 7b. Campaign Map Icon — `dadg_<id>_mapicon`

A lightweight representation shown on the campaign map during sieges. It shares the same meshes as the battle prefab but is scaled to 0.5x and uses a skeleton for animation.

**Hierarchy:**

```
dadg_<id>_mapicon
│
└── scaler
        rotation Z: 3.141 (faces the correct direction)
    │
    ├── projectile_position         where the projectile spawns during the fire animation
    │   tag: projectile_position
    │
    └── clean
            tag: operational
            tag: Battery_Base
            tag: siege_machine_mapicon_skeleton
            scale: 0.5, 0.5, 0.5
            skeleton: trebuchet_a_mapicon_skeleton
            mesh: dadg_siege_<id>_base
            script: SynchedMissionObject
        │
        └── barrel
                tag: Barrel
                mesh: dadg_siege_<id>_barrel
                script: SynchedMissionObject
            │
            ├── wheel_R     mesh: dadg_siege_<id>_wheel_r
            │               script: SynchedMissionObject
            └── wheel_L     mesh: dadg_siege_<id>_wheel_l
                            script: SynchedMissionObject
```

The `trebuchet_a_mapicon_skeleton` skeleton drives the fire and reload animations defined in `cannons.xml`. The `CampaignMapProjectileBoneIndex` value identifies which skeleton bone the projectile entity's world-space transform is derived from during the fire animation. Bone `0` is the root bone — the correct choice when reusing the shared skeleton, since the `projectile_position` child entity in the prefab hierarchy already defines the visual spawn point relative to that root.

---

### 7c. Ghost Preview — `dadg_<id>_ghost`

A simplified, non-interactive copy of the cannon shown as a placement preview in the deployment UI. It is a child of the spawner prefab.

Mirror the structure of `dadg_<id>` with these omissions:
- No `GenericCannon_<id>` script
- No `DestructableComponent` script
- No `projectile_pile` / `CannonBallPile`
- No ammo pickup scripts
- Keep `SynchedMissionObject` on the root only

The ghost entity should be tagged `machine_parent` and flagged `visible_only_when_editing` at the root so it does not appear in final play.

---

### 7d. Cannonball Map Icon — `cannonball_mapicon_projectile`

Already defined and shared across all cannons. No changes needed. Reference it in `cannons.xml` as `CampaignMapProjectilePrefabName`.

---

### 7e. Spawner Prefab — `dadg_<id>_spawner`

Controls which team owns the cannon and is the entity placed in scene files by the level designer.

```xml
<game_entity name="dadg_<id>_spawner" old_prefab_name="" mobility="1">
    <scripts>
        <script name="GenericCannonSpawner">
            <variables>
                <variable name="Team" value="Attacker"/>
                <variable name="ToBeSpawnedOverrideName" value=""/>
                <variable name="ToBeSpawnedOverrideNameForFireVersion" value=""/>
                <variable name="NavMeshPrefabName" value=""/>
            </variables>
        </script>
    </scripts>
    <children>
        <!-- ghost prefab as child for deployment preview -->
        <game_entity name="dadg_<id>_ghost" old_prefab_name="dadg_<id>_ghost" mobility="1"/>
    </children>
</game_entity>
```

One spawner prefab is enough for both sides. The `Team` variable is set per-placement in the scene editor — drag the same `dadg_<id>_spawner` into the scene and set `Team` to `Attacker` or `Defender` in the Entity Inspector for each instance. No separate defender variant is needed.

`ToBeSpawnedOverrideName` can reference a different cannon prefab name if you want the spawner to place something other than the default `dadg_<id>`.

---

## Step 8 — Place Spawners in Scenes

**Files:** `Modules/DellarteDellaGuerraScenes/SceneObj/<SceneName>/scene.xscene`

The workflow is identical to placing vanilla Bannerlord siege engines. Open the scene in the Bannerlord scene editor and place the spawner using the Prefabs panel.

**All three of the following conditions must be met or the cannon will not function in battle.**

### Rule 1 — Place the spawner prefab

Drag `dadg_<id>_spawner` from the Prefabs panel into the scene at the desired position and rotation.

- **Attacker spawners:** place in the siege camp area, in open ground with line of sight to the walls.
- **Defender spawners:** place on battlements, towers, or courtyard areas with clear sight lines over the walls.
- Rotation is set on the Z axis (yaw) in the inspector, displayed in **degrees**.
- Avoid steep slopes or geometry intersections — the deployment ghost will clip through terrain.

### Rule 2 — Proximity to a `siege_deployment_placeholder`

The spawner must be within range of an existing `siege_deployment_placeholder` entity in the scene. To verify:

1. Select the `siege_deployment_placeholder` in the scene.
2. The cannon spawner should be highlighted in red in the viewport — this confirms it has been detected as a valid siege weapon.

If it is not highlighted, the cannon will not be available during the battle. Move the spawner closer to the placeholder, or add a new placeholder if the intended placement area has none.

### Rule 3 — Visibility settings

In the Entity Inspector for the spawner, configure the **Upgrade Level Visibilities** exactly as follows. This is the same requirement as all vanilla siege engines — the cannon must be hidden during peacetime scene views.

| Level | Setting |
|---|---|
| `siege` | ✓ enabled |
| `level_1` | ✓ enabled |
| `level_2` | ✓ enabled |
| `level_3` | ✓ enabled |
| `base` | ✗ disabled |
| `civilian` | ✗ disabled |

Also verify **Mobility** is set to `Dynamic` in the inspector.

### Rule 4 — Spawner forward axis must face away from the walls

This is the most counter-intuitive requirement and the easiest source of a hard-to-diagnose bug.

Bannerlord's native `RangedSiegeWeapon` AI applies a hardcoded 180° flip to the cannon root entity's frame when computing the local aim angle (`CalculateLocalAnglesFromGlobalDirection`). The consequence is:

> **The spawner's forward axis (`f`, the blue arrow in the scene editor) must point toward the siege camp — away from the enemy walls.**

After placing the spawner, rotate it on the Z axis until its blue forward arrow points away from the walls (toward the attacker's starting position).

#### Why Battery_Base also needs a π rotation in the prefab

Because the spawner faces away from walls, the AI correctly computes aim angle ≈ 0 for a wall target. But angle 0 means "Battery_Base at its initial local orientation." With no rotation on `clean`, Battery_Base's local forward aligns with the spawner's forward — which points away from walls. The cannon would visually face backward even though the AI thinks it's aimed correctly.

The fix — already applied in `cannons.xml` — is `rotation_euler="0.000, 0.000, 3.14159"` on the `clean` (Battery_Base) entity. This π flip means that at AI angle 0, Battery_Base's global forward = spawner(away from walls) × Battery_Base(flipped) = toward walls. The cannon looks right and the AI is satisfied.

If you copy the falconet prefab as instructed, this rotation is inherited automatically. If you build a prefab from scratch, do not forget it.

#### Symptom table

| Spawner orientation | `clean` rotation_euler Z | Result |
|---|---|---|
| Away from walls ✓ | `3.14159` ✓ | Cannon aims and faces walls correctly |
| Away from walls ✓ | `0` (missing) | Cannon initialises facing walls, then immediately rotates 180° away on the first AI tick |
| Toward walls | `3.14159` | Cannon stays frozen facing away from walls — AI angle is always π, which `AimAtTarget` blocks |
| Toward walls | `0` (missing) | Cannon initialises facing away from walls, AI blocked, stays facing away permanently |

**Visible symptom for all broken configurations:** the cannon snaps 180° to face away from the walls at the very start of the battle and never rotates toward them. The ballistic Harmony patch still fires the projectile in the correct direction, so the cannon shoots at the walls despite visually pointing the wrong way — making this bug especially confusing.

#### Blender / modelling convention

Model the cannon with the barrel pointing along the mesh's +Y axis (Bannerlord's forward). Export and import as normal. The world-space orientation is then handled entirely in the scene editor: rotate the spawner so the +Y axis points away from the walls. The `clean` entity's π rotation in the prefab takes care of the rest.

---

### Testing the placement

After saving the scene:

1. On the campaign map, build the cannon siege engine and start the attack.
2. Entering the siege battle, the cannon should spawn at exactly the position and rotation set in the editor.
3. The AI will use the cannon automatically. The player can also operate it directly.

---

## Step 9 — Audio (Optional)

**File:** `Modules/DellarteDellaGuerra/ModuleData/Cannons/sounds.xml`

**Sound files:** `Modules/DellarteDellaGuerra/ModuleSounds/Cannon/`

The falconet firing sounds (`mortar_shot_1`, `mortar_shot_2`) can be reused directly by referencing them in the prefab script variables `FireSoundID` and `FireSoundID2`. No changes to `sounds.xml` are needed unless you want a distinct firing sound.

To add a unique sound:

```xml
<module_sound name="culverin_shot_1"
              sound_category="mission_siege_loud"
              min_pitch_multiplier="0.85"
              max_pitch_multiplier="1.05">
    <variation path="../ModuleSounds/Cannon/YourFile_1.ogg" weight="1.0"/>
    <variation path="../ModuleSounds/Cannon/YourFile_2.ogg" weight="1.0"/>
</module_sound>
```

Then set `FireSoundID` to `culverin_shot_1` in the prefab.

### Sound Categories

Cannons must use `mission_siege_loud`. Sounds without a valid category are silently dropped.

| Attribute | Description |
|---|---|
| `name` | Event name referenced in the prefab variable or code. |
| `sound_category` | Must be a valid Bannerlord category. Use `mission_siege_loud` for cannon sounds. |
| `min_pitch_multiplier` / `max_pitch_multiplier` | Pitch variation range for natural randomness. |
| `<variation path="...">` | Path to an OGG file relative to the ModuleData folder. Multiple variations are randomly selected. |
| `weight` | Relative probability of selecting this variation. Equal weights mean equal probability. |

---

## Step 10 — Add UI Sprites

**File:** `Modules/DellarteDellaGuerraMap/GUI/DadgCannonsSpriteData.xml`

**Atlas:** `Modules/DellarteDellaGuerraMap/Assets/GauntletUI/dadg_ui_cannon_1_tex.tpac`

Three sprites are required per cannon. The pixel dimensions are **strict** — a texture with wrong dimensions will appear bugged in the UI.

| Sprite name | Purpose | Required dimensions |
|---|---|---|
| `Order\SiegeIcons\<id>` | Deployment order panel shown during in-battle siege deployment | **146 × 146 px** |
| `SPGeneral\MapSiege\<id>` | Shown when pressing Alt in battle to display cannon positions; also shown when the cannon is deployed on the campaign siege map | **30 × 23 px** |
| `SPGeneral\Siege\<id>` | Campaign map siege engine selection panel (shown when choosing which engine to build) | **52 × 52 px** |

These three names must exactly match the values in `cannons.xml`:
- `SiegeDeploymentSelectionIconSpriteId` ← `Order\SiegeIcons\<id>`
- `MapSiegeMarkerSpriteId` ← `SPGeneral\MapSiege\<id>`
- `CampaignMapSelectionIconSpriteId` ← `SPGeneral\Siege\<id>`

### How the Atlas Works

All cannon sprites live in a single sprite category named `dadg_ui_cannon`, which is a 256×256 pixel atlas image compiled to `dadg_ui_cannon_1_tex.tpac`. The `DadgCannonsSpriteData.xml` file maps each sprite name to its (SheetX, SheetY) position and (Width, Height) within that 256×256 grid.

The source PNG for the atlas is not committed to the repository — only the compiled `.tpac` is tracked. To add new sprites, use BannerEdge to decompose and repack the atlas.

### Workflow

1. **Create three PNG files** at the exact dimensions listed above.
2. **Open BannerEdge** and import `dadg_ui_cannon_1_tex.tpac`. BannerEdge will split it back into individual source PNGs inside a `SpriteParts`-style folder.
3. **Add your three PNGs** to the appropriate paths within the resulting folder structure, matching the sprite names:
   - `Order/SiegeIcons/<id>.png`
   - `SPGeneral/MapSiege/<id>.png`
   - `SPGeneral/Siege/<id>.png`
4. **Re-pack the atlas** using BannerEdge. It assigns `SheetX`/`SheetY` coordinates automatically and produces an updated `SpriteData.xml`.
5. **Recompile the atlas** with the Bannerlord Modding Kit to regenerate `dadg_ui_cannon_1_tex.tpac`.
6. **Update `DadgCannonsSpriteData.xml`** with the new `<SpritePart>` and `<GenericSprite>` entries using the coordinates from BannerEdge's output.

### SpriteData XML Format

If you are editing `DadgCannonsSpriteData.xml` manually after packing the atlas yourself:

```xml
<!-- Add inside <SpriteParts> -->
<SpritePart>
    <SheetID>1</SheetID>
    <Name>Order\SiegeIcons\culverin</Name>
    <Width>146</Width>
    <Height>146</Height>
    <SheetX>4</SheetX>      <!-- x pixel offset within the 256×256 atlas, assigned by BannerEdge -->
    <SheetY>155</SheetY>    <!-- y pixel offset — must not overlap existing sprites -->
    <CategoryName>dadg_ui_cannon</CategoryName>
</SpritePart>
<SpritePart>
    <SheetID>1</SheetID>
    <Name>SPGeneral\MapSiege\culverin</Name>
    <Width>30</Width>
    <Height>23</Height>
    <SheetX>218</SheetX>
    <SheetY>155</SheetY>
    <CategoryName>dadg_ui_cannon</CategoryName>
</SpritePart>
<SpritePart>
    <SheetID>1</SheetID>
    <Name>SPGeneral\Siege\culverin</Name>
    <Width>52</Width>
    <Height>52</Height>
    <SheetX>158</SheetX>
    <SheetY>155</SheetY>
    <CategoryName>dadg_ui_cannon</CategoryName>
</SpritePart>

<!-- Add inside <Sprites> -->
<GenericSprite>
    <Name>Order\SiegeIcons\culverin</Name>
    <SpritePartName>Order\SiegeIcons\culverin</SpritePartName>
</GenericSprite>
<GenericSprite>
    <Name>SPGeneral\MapSiege\culverin</Name>
    <SpritePartName>SPGeneral\MapSiege\culverin</SpritePartName>
</GenericSprite>
<GenericSprite>
    <Name>SPGeneral\Siege\culverin</Name>
    <SpritePartName>SPGeneral\Siege\culverin</SpritePartName>
</GenericSprite>
```

`SheetX`/`SheetY` are the top-left pixel coordinates of each sprite within the 256×256 atlas. Sprites must not overlap each other. The atlas has 256×256 pixels total — if the current sprites fill it, you will need to create a new `<SpriteCategory>` with its own atlas.

### Note on DadgMainUISpriteParts

`Modules/DellarteDellaGuerraMap/GUI/DadgMainUISpriteParts/` is the source sprite-parts folder for the **main DADG UI** (character creation screens, campaign map panels, mission overlays). It was renamed from `GUI/SpriteParts` specifically to avoid interfering with BannerEdge when editing the cannon sprite atlas. Do **not** place cannon icon PNGs inside `DadgMainUISpriteParts`.

---

## File Checklist

**Module: `DellarteDellaGuerra`**

| File | Required | Action |
|---|---|---|
| `ModuleData/CustomXml/cannons.xml` | Yes | Add a `<Cannon>` block |
| `ModuleData/Cannons/siege_engines.xml` | Yes | Add a `<SiegeEngineType>` block |
| `ModuleData/Cannons/cannon_items.xml` | Yes | Add an `<Item>` block for the cannonball |
| `ModuleData/Cannons/physics_materials.xml` | No | Add only if using custom projectile physics |
| `ModuleData/Cannons/collision_infos.xml` | No | Add only if using a new physics material id |
| `ModuleData/Cannons/sounds.xml` | No | Add only if using unique firing sounds |
| `AssetSources/cannons/cannons.fbx` | Yes | Add cannon and physics meshes |
| `Assets/cannons/cannons_geo.tpac` | Yes | Recompile via Modding Kit after editing FBX |
| `Prefabs/cannons.xml` | Yes | Add `dadg_<id>`, `dadg_<id>_mapicon`, `dadg_<id>_ghost`, `dadg_<id>_spawner` |
| `ModuleSounds/Cannon/` | No | Add OGG files only if using unique sounds |
| Scene `.xscene` files | Yes | Place `dadg_<id>_spawner` entities |

**Module: `DellarteDellaGuerraMap`**

| File | Required | Action |
|---|---|---|
| `GUI/DadgCannonsSpriteData.xml` | Yes | Add 3 `<SpritePart>` + 3 `<GenericSprite>` entries |
| `Assets/GauntletUI/dadg_ui_cannon_1_tex.tpac` | Yes | Recompile after adding sprites via BannerEdge |

---

## Verification Checklist

1. Launch the game and start a campaign.
2. Confirm startup log shows `[INFO] Loaded cannon: <id>` (not a warning or error).
3. Initiate a siege at a settlement whose scene contains your spawner.
4. On the campaign map: confirm the cannon icon appears in the siege engine panel with the correct sprite.
5. Enter the siege battle: confirm the spawner entity is visible at the expected position.
6. Deploy the cannon: confirm the ghost preview appears during deployment and the cannon spawns correctly.
7. Operate the cannon: confirm firing animation, muzzle flash particle, sound, and projectile behaviour.
8. Allow the cannon to be destroyed: confirm the `destroyed` state entity becomes visible.
9. Return to the campaign map: confirm the map icon animates fire and reload during the siege.
