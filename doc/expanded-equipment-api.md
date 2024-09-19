# Expanded Equipment Randomisation

## Introduction

- Are you annoyed by seeing cavalry lances on the backs of knights during sieges?
- Do you want to make your pikemen more effective in sieges by equipping them with a one-handed weapon and a shield while keeping pikes in field battles?
- Do you want to add a new armor set to a unit, but it doesn't match with its other armor sets?

Follow this document to understand how you can configure your troop equipment to resolve these use cases. First, we will cover the basics and the limits of modding Bannerlord's troop equipment. Secondly, we will go into detail about the new modding features.

## Bannerlord's Native Randomisation

### Troop Definition

Troops in Bannerlord are defined by a list of `NPCCharacter` in the XML files.

For instance, a Levy Footman is defined in the [dadg_spnpccharacters.xml](https://github.com/DellarteDellaGuerraTeam/DellarteDellaGuerraMap/blob/main/ModuleData/dadg_spnpccharacters.xml#L997-L1222) file as follows:

<details>
  <summary>See the XML sample</summary>

```xml
<NPCCharacters>
	<NPCCharacter id="vlandian_recruit" default_group="Infantry" level="6" name="{=GEnwDYp1}Levy Footman" occupation="Soldier" is_basic_troop="true" culture="Culture.empire">
		<face>
			<face_key_template value="BodyProperty.fighter_england"/>
		</face>
		<skills>
			<skill id="Athletics" value="20"/>
			<skill id="Riding" value="5"/>
			<skill id="OneHanded" value="20"/>
			<skill id="TwoHanded" value="10"/>
			<skill id="Polearm" value="20"/>
			<skill id="Bow" value="5"/>
			<skill id="Crossbow" value="5"/>
			<skill id="Throwing" value="5"/>
		</skills>
		<upgrade_targets>
			<upgrade_target id="NPCCharacter.vlandian_crossbowman"/>
			<upgrade_target id="NPCCharacter.vlandian_footman"/>
		</upgrade_targets>
		<Equipments>
			<EquipmentRoster>
				<equipment slot="Item0" id="Item.polearm_longspear"/>
				<equipment slot="Body" id="Item.simple_livery_coat_opened_over_jack"/>
				<equipment slot="Leg" id="Item.hosen_with_boots_a"/>
				<equipment slot="Head" id="Item.rounded_froissart_sallet_without_visor"/>
			</EquipmentRoster>
			<EquipmentRoster>
				<equipment slot="Item0" id="Item.poleaxe_poleaxea"/>
				<equipment slot="Item1" id="Item.castillon_c"/>
				<equipment slot="Body" id="Item.brigandine_with_older_arms"/>
				<equipment slot="Leg" id="Item.imported_leg_harness"/>
				<equipment slot="Head" id="Item.beachamp_bascinet_b"/>
				<equipment slot="Gloves" id="Item.mitten_gauntlets"/>
			</EquipmentRoster>
			<EquipmentSet id="vlandia_troop_civilian_template_t1" civilian="true"/>
		</Equipments>
	</NPCCharacter>
</NPCCharacters>
```

</details>

### Equipment

The `NPCCharacter` definition requires an `Equipments` tag which contains a list of `EquipmentRoster` and `EquipmentSet` elements. It also accepts the following tags: `equipmentSet`, `equipmentRoster`.

#### EquipmentRoster

An `EquipmentRoster` is an equipment loadout. It has a list of `equipment` tags which define the equipment slots and the items to be equipped. The slots are defined by the `slot` attribute which can be `Item0`, `Item1`, `Item2`, `Item3`, `Head`, `Cape`, `Body`, `Leg`, `Gloves`, `Horse` and `HorseHarness`.

The `id` attribute of the `equipment` tag is the item's id to be equipped. It must be prefixed by `Item.` followed by the item id.

#### EquipmentSet

An `EquipmentSet` is a set of `EquipmentRosters`. It is generally used to define civilian clothes for troops as most troops share the same cultural civilian clothes. It may also be used to define a set of equipment for a specific troop type.

#### How EquipmentSet Nodes are Resolved

1. **Reference to EquipmentRosters**:
   The `EquipmentSet` defined in the `NPCCharacter` XML file links to equipment sets defined in the `EquipmentRosters` XML file by `id`. For example:

   ```xml
   <NPCCharacter id="vlandian_recruit" ...>
     ...
     <Equipments>
       <EquipmentSet id="vlandia_troop_civilian_template_t1" civilian="true"/>
     </Equipments>
   </NPCCharacter>
   ```

   Here, the `EquipmentSet` with `id="vlandia_troop_civilian_template_t1"` refers to the corresponding equipment sets in the `EquipmentRosters` file.

2. **Matching EquipmentRosters**:
   The `id` specified in the `EquipmentSet` node is matched with an entry in the `EquipmentRosters` XML file:

   ```xml
   <EquipmentRosters>
     <EquipmentRoster id="vlandia_troop_civilian_template_t1" culture="Culture.empire">
       <EquipmentSet civilian="true" pool="0">
         <Equipment slot="Head" id="Item.upturnedbrimmedhat"/>
         <Equipment slot="Item0" id="Item.vlandia_sword_2_t3"/>
         <Equipment slot="Body" id="Item.cloth_tunic"/>
         <Equipment slot="Leg" id="Item.fine_town_boots"/>
       </EquipmentSet>
       <EquipmentSet civilian="true" pool="1">
         <Equipment slot="Head" id="Item.upturnedbrimmedhat"/>
         <Equipment slot="Item0" id="Item.vlandia_sword_2_t3"/>
         <Equipment slot="Body" id="Item.fine_town_tunic"/>
         <Equipment slot="Leg" id="Item.fine_town_boots"/>
       </EquipmentSet>
       <EquipmentSet>
         <Equipment slot="Head" id="Item.upturnedbrimmedhat2"/>
         <Equipment slot="Item0" id="Item.vlandia_sword_1_t2"/>
         <Equipment slot="Head" id="Item.open_padded_coif"/>
         <Equipment slot="Body" id="Item.fine_town_tunic"/>
         <Equipment slot="Leg" id="Item.strapped_shoes"/>
       </EquipmentSet>
     </EquipmentRoster>
   </EquipmentRosters>
   ```

   The `NPCCharacter`'s `EquipmentSet` node will use the equipment sets defined in this `EquipmentRoster`.

3. **Civilian Attribute Filtering**:
   The `civilian` attribute in the `EquipmentSet` node in the `NPCCharacter` XML file filters the `EquipmentSet` nodes based on their `civilian` attribute in the `EquipmentRosters` file:

   - If `civilian="true"`, only `EquipmentSet` nodes with `civilian="true"` will be considered.
   - In the given example, the first two `EquipmentSet` nodes have `civilian="true"`, so they will be used while the last one will be filtered out.
   - Note that this flag is mutually exclusive meaning that if `civilian="true"` is not in the `EquipmentSet` node in the `NPCCharacter` XML file, then all `EquipmentSet` nodes in the `EquipmentRosters` file without exactly `civilian="true"` will be considered.

#### Summary

The `EquipmentSet` nodes allow for organizing different sets of equipment by referencing them through their `id` in the `EquipmentRosters` file. The `civilian` attribute filters the equipment sets based on their type, ensuring appropriate loadouts for different scenarios.

### Randomisation
#### Affected missions
Bannerlord uses a randomisation system at the start of a battle or friendly missions to equip troops and civilians upon their spawn. These include:
- Field battles
- Siege battles
- Garrison sally outs
- Hideout Ambushes
- Prison escapes
- Visiting villages/castles/towns
- Visiting taverns
- Visiting Lord's halls

However, some mission types use defined templates such as Tournaments.

#### Implementation
By looking at the NPCCharacters file, you might intuitively think that the game randomly selects an `EquipmentRoster` from the list of `Equipments` and equips the troop with the defined items.

However, it works differently; all `EquipmentRoster` and resolved `EquipmentSet` are grouped by slot type. It then selects a random item among the defined slot items for each slot.

The main drawback of this system is that it does not allow the definition of multiple unique equipment loadouts for a given troop, potentially resulting in helmets and armours that do not match each other.

## Expanded API

In order to overcome the limitations of Bannerlord's native randomisation system, we are introducing an expanded randomisation system which allows you:
- To define `EquipmentRoster` and `EquipmentSet` nodes for field battles, sieges, and "civilian" missions via the new `battle` and `siege` attributes.
- To define `EquipmentRoster` and `EquipmentSet` nodes in different equipment pools via a `pool` attribute.

### Equipment Pools
#### How does it work ?
An equipment pool is a set of `EquipmentRoster` and resolved `EquipmentSet` nodes. Before starting a battle or a friendly mission, the game will randomly select one of the troop's equipment pools which will be given to the native randomisation system. It will randomly select the equipment from that equipment pool, thus not considering the equipment from any other equipment pool.

#### Defining Equipment pools in XML

You can add the `pool` attribute in a `EquipmentRoster` node in the `NPCCharacters` file or in a `EquipmentSet` node in the `EquipmentRosters` file. Its value must an integer.

In case, the `pool` attribute is not defined, the equipment loadout is considered to be part of the default pool.
The default pool has a value of `0`.

So if `pool="0"` is defined, it is considered to be part of the default pool.

#### Unique Layouts

Let's take the `Levy Footman` example from above and add a `pool` attribute to the `EquipmentRoster` tags:

<details>
  <summary>See the xml sample</summary>

```xml
<NPCCharacters>
	<NPCCharacter id="vlandian_recruit" default_group="Infantry" level="6" name="{=GEnwDYp1}Levy Footman" occupation="Soldier" is_basic_troop="true" culture="Culture.empire">
		<face>
			<face_key_template value="BodyProperty.fighter_england"/>
		</face>
		<skills>
			<skill id="Athletics" value="20"/>
			<skill id="Riding" value="5"/>
			<skill id="OneHanded" value="20"/>
			<skill id="TwoHanded" value="10"/>
			<skill id="Polearm" value="20"/>
			<skill id="Bow" value="5"/>
			<skill id="Crossbow" value="5"/>
			<skill id="Throwing" value="5"/>
		</skills>
		<upgrade_targets>
			<upgrade_target id="NPCCharacter.vlandian_crossbowman"/>
			<upgrade_target id="NPCCharacter.vlandian_footman"/>
		</upgrade_targets>
		<Equipments>
			<EquipmentRoster pool="1">
				<equipment slot="Item0" id="Item.polearm_longspear"/>
				<equipment slot="Body" id="Item.simple_livery_coat_opened_over_jack"/>
				<equipment slot="Leg" id="Item.hosen_with_boots_a"/>
				<equipment slot="Head" id="Item.rounded_froissart_sallet_without_visor"/>
			</EquipmentRoster>
			<EquipmentRoster pool="2">
				<equipment slot="Item0" id="Item.poleaxe_poleaxea"/>
				<equipment slot="Item1" id="Item.castillon_c"/>
				<equipment slot="Body" id="Item.brigandine_with_older_arms"/>
				<equipment slot="Leg" id="Item.imported_leg_harness"/>
				<equipment slot="Head" id="Item.beachamp_bascinet_b"/>
				<equipment slot="Gloves" id="Item.mitten_gauntlets"/>
			</EquipmentRoster>
			<EquipmentSet id="vlandia_troop_civilian_template_t1" civilian="true"/>
		</Equipments>
	</NPCCharacter>
</NPCCharacters>
```

</details>


So now, we have respectively added `pool="1"` and ``pool="2"`` to the first and second `EquipmentRoster` nodes of the `Levy Footman` definition.

Let's see what the result is in game:

![2_groups](https://github.com/DellarteDellaGuerraTeam/DellarteDellaGuerraMap/assets/32904771/582536b6-7196-4f33-933b-8d30f2be2d91)

As you can see, the `Levy Footman` troops have two distinct equipment loadouts.

#### Equipment pool with multiple equipment loadouts

Let's take once again the `Levy Footman` example and define multiple `EquipmentRoster` nodes to a pool and have an `EquipmentRoster` in the default pool:

<details>
  <summary>See the xml sample</summary>

```xml
<NPCCharacters>
	<NPCCharacter id="vlandian_recruit" default_group="Infantry" level="6" name="{=GEnwDYp1}Levy Footman" occupation="Soldier" is_basic_troop="true" culture="Culture.empire">
		<face>
			<face_key_template value="BodyProperty.fighter_england"/>
		</face>
		<skills>
			<skill id="Athletics" value="20"/>
			<skill id="Riding" value="5"/>
			<skill id="OneHanded" value="20"/>
			<skill id="TwoHanded" value="10"/>
			<skill id="Polearm" value="20"/>
			<skill id="Bow" value="5"/>
			<skill id="Crossbow" value="5"/>
			<skill id="Throwing" value="5"/>
		</skills>
		<upgrade_targets>
			<upgrade_target id="NPCCharacter.vlandian_crossbowman"/>
			<upgrade_target id="NPCCharacter.vlandian_footman"/>
		</upgrade_targets>
		<Equipments>
			<EquipmentRoster pool="1">
				<equipment slot="Item0" id="Item.polearm_longspear"/>
				<equipment slot="Body" id="Item.simple_livery_coat_opened_over_jack"/>
				<equipment slot="Leg" id="Item.hosen_with_boots_a"/>
				<equipment slot="Head" id="Item.rounded_froissart_sallet_without_visor"/>
			</EquipmentRoster>            
			<EquipmentRoster pool="1">
				<equipment slot="Item0" id="Item.poleaxe_poleaxea"/>
				<equipment slot="Item1" id="Item.castillon_c"/>
				<equipment slot="Body" id="Item.brigandine_with_older_arms"/>
				<equipment slot="Leg" id="Item.imported_leg_harness"/>
				<equipment slot="Head" id="Item.beachamp_bascinet_b"/>
				<equipment slot="Gloves" id="Item.mitten_gauntlets"/>
			</EquipmentRoster>
			<EquipmentRoster>
				<equipment slot="Item1" id="Item.wakefield_hanger"/>
				<equipment slot="Item0" id="Item.polearm_halberdb"/>
				<equipment slot="Body" id="Item.breastplate_with_mail"/>
				<equipment slot="Leg" id="Item.hosen_with_shoes_a"/>
				<equipment slot="Head" id="Item.open_decorated_helmet_with_orle"/>
				<equipment slot="Cape" id="Item.english_imported_bevor"/>
				<equipment slot="Gloves" id="Item.mitten_gauntlets"/>
			</EquipmentRoster>
			<EquipmentSet id="vlandia_troop_civilian_template_t1" civilian="true"/>
		</Equipments>
	</NPCCharacter>
</NPCCharacters>
```
</details>

So, we changed the pool of the second `EquipmentRoster` node to pool `1` and added a third `EquipmentRoster` node to the default pool.

Let's see what the result is in game:

![default_and_2_eq_pool1](https://github.com/DellarteDellaGuerraTeam/DellarteDellaGuerraMap/assets/32904771/fd3ace8a-e144-4586-96dd-f450392262c4)

As you can see, the `Levy Footman` troops which had the two unique equipment loadouts now share the items of both layouts.

On the other hand, the `Levy Footman` troops which use the default equipment loadout have only the items of the default layout.

Note that if the third `EquipmentRoster` from the `Levy Footman` definition were removed and the `Levy Footman` troops were still assigned to the pool `1`, it would behave the same way as the native randomisation system.

#### Random factors

Currently, the system does not support user-defined probabilities for the pools. Instead, the probability of a pool being selected is based on the number of `EquipmentRoster` it contains. For example, in our case, pool `1` has a 2/3 chance of being selected, while the default pool has a 1/3 chance.

Thus, a pool with more `EquipmentRoster` has a higher likelihood of being selected. Note that civilian clothes are not considered in the probability calculation.

### Defining Equipment Types for Battle, Siege, and Civilian Missions

In the expanded randomisation system, you can assign equipment specifically for battle, siege, or civilian missions by using the `battle`, `siege`, and `civilian` flags in the `EquipmentRoster` or `EquipmentSet` nodes.

#### How Equipment Flags Work

- If no flags are set (`siege="true"` or `civilian="true"`), the equipment is automatically considered **battle** equipment.
- If the flag `siege="true"` is set, the equipment is for **siege** missions.
- If the flag `civilian="true"` is set, the equipment is for **civilian** missions.
- You can use `battle="true"` and combine it with the `siege`, and `civilian` flags on the same `EquipmentRoster` to avoid duplicating equipment setups.

#### Summary of Flags:
- **No flags**: Battle equipment.
- **`battle="true"`**: Explicitly defined battle equipment.
- **`siege="true"`**: Siege-specific equipment.
- **`civilian="true"`**: Civilian-specific equipment.
- **Combined flags**: Equipment shared across multiple mission types.

#### Example XML Configurations

1. **Battle Equipment**:
   ```xml
   <EquipmentRoster>
       <equipment slot="Item0" id="Item.sword_battle1"/>
       <equipment slot="Body" id="Item.armor_battle1"/>
   </EquipmentRoster>
   <EquipmentSet id="vlandia_troop_bqttle_template_t1" />
   <EquipmentRoster battle="true">
       <equipment slot="Item0" id="Item.sword_battle2"/>
       <equipment slot="Body" id="Item.armor_battle2"/>
   </EquipmentRoster>
   <EquipmentSet id="vlandia_troop_bqttle_template_t2" battle="true"/>
   ```
   `EquipmentRoster` with no flags are treated as **battle equipment** by default.
   The `EquipmentSet` also references only **battle equipment**.

   If the flag `battle="true"` is used, the nodes will be considered as **battle equipment**.

2. **Siege Equipment**:
   ```xml
   <EquipmentRoster siege="true">
       <equipment slot="Item0" id="Item.siege_sword"/>
       <equipment slot="Body" id="Item.siege_armor"/>
   </EquipmentRoster>
   <EquipmentSet id="vlandia_troop_siege_template_t1" siege="true"/>
   ```
   The `EquipmentRoster` defines equipment explicitly for **siege** scenarios.
   The `EquipmentSet` with the `siege="true"` flag functions the same way as the native civilian flag. It will only consider the referenced nodes with that flag. 

3. Civilian Equipment:
   ```xml
   <EquipmentRoster civilian="true">
       <equipment slot="Item0" id="Item.siege_sword"/>
       <equipment slot="Body" id="Item.siege_armor"/>
   </EquipmentRoster>
   <EquipmentSet id="vlandia_troop_civilian_template_t1" civilian="true"/>
   ```
   This defines equipment explicitly for **civilian** scenarios.
   This behaves exactly like native.

4. **Multi-Purpose Equipment**:

   ```xml
   <EquipmentRoster battle="true" siege="true">
       <equipment slot="Item0" id="Item.siege_sword"/>
       <equipment slot="Body" id="Item.siege_armor"/>
   </EquipmentRoster>
   <EquipmentSet id="vlandia_troop_civilian_template_t1" battle="true" siege="true"/>
   ```
   This `EquipmentRoster` will be used in both **battle** and **siege** scenarios.

   ```xml
   <EquipmentRoster battle="true" siege="true" civilian="true">
       <equipment slot="Item0" id="Item.multiuse_weapon"/>
       <equipment slot="Body" id="Item.multiuse_armor"/>
   </EquipmentRoster>
   <EquipmentSet id="vlandia_troop_civilian_template_t1" battle="true" siege="true" civilian="true"/>
   ```
   This loadout applies to **all mission types**.

### Conclusion

We have covered the creation of equipment pools and the definition of mission-specific equipment. These new API features provide more control over troop equipment randomization in Bannerlord.

Let's demonstrate how to make a pikeman unit more effective in siege battles. We are also introducing the new "Durham" armor set. 

#### Original XML Definition

<details>
  <summary>Expand this to see the XML pikeman definition</summary>
  ```xml
  <NPCCharacter id="vlandian_pikeman">
    ...
    <Equipments>
      <EquipmentRoster>
        <equipment slot="Item0" id="Item.pike_halfpike"/>
        <equipment slot="Body" id="Item.brigandine_with_coat"/>
        <equipment slot="Leg" id="Item.hosen_with_boots_a"/>
        <equipment slot="Head" id="Item.rounded_froissart_sallet_with_opened_visor"/>
        <equipment slot="Gloves" id="Item.mitten_gauntlets"/>
      </EquipmentRoster>
      <EquipmentRoster>
        <equipment slot="Item0" id="Item.pike_halfpike"/>
        <equipment slot="Body" id="Item.livery_coat_opened_over_brigandine"/>
        <equipment slot="Leg" id="Item.hosen_with_shoes_a"/>
        <equipment slot="Head" id="Item.opened_sallet"/>
        <equipment slot="Cape" id="Item.english_imported_bevor"/>
        <equipment slot="Gloves" id="Item.mitten_gauntlets"/>
      </EquipmentRoster>
      <EquipmentSet id="vlandia_troop_civilian_template_t1" civilian="true"/>
    </Equipments>
  </NPCCharacter>
  ```
</details>

The original definition provides two equipment layouts, both using a pike weapon. However, long weapons like pikes are less effective in siege battles. We'll modify the setup to use a shorter weapon like a billhook during sieges, while maintaining the pike for field battles. We'll also incorporate the new Durham armor set.

#### Updated XML Definition

<details>
  <summary>Expand this to see the updated XML pikeman definition</summary>
  ```xml
  <NPCCharacter id="vlandian_pikeman">
    ...
    <Equipments>
      <EquipmentRoster pool="1">
        <equipment slot="Item0" id="Item.pike_halfpike"/>
        <equipment slot="Body" id="Item.brigandine_with_coat"/>
        <equipment slot="Leg" id="Item.hosen_with_boots_a"/>
        <equipment slot="Head" id="Item.rounded_froissart_sallet_with_opened_visor"/>
        <equipment slot="Gloves" id="Item.mitten_gauntlets"/>
      </EquipmentRoster>
      <EquipmentRoster pool="1">
        <equipment slot="Item0" id="Item.pike_halfpike"/>
        <equipment slot="Body" id="Item.livery_coat_opened_over_brigandine"/>
        <equipment slot="Leg" id="Item.hosen_with_shoes_a"/>
        <equipment slot="Head" id="Item.Item.opened_sallet"/>
        <equipment slot="Cape" id="Item.english_imported_bevor"/>
        <equipment slot="Gloves" id="Item.mitten_gauntlets"/>
      </EquipmentRoster>
      <EquipmentRoster siege="true" pool="1">
        <equipment slot="Item0" id="Item.polearm_billhook"/>
        <equipment slot="Body" id="Item.brigandine_with_coat"/>
        <equipment slot="Leg" id="Item.hosen_with_boots_a"/>
        <equipment slot="Head" id="Item.rounded_froissart_sallet_with_opened_visor"/>
        <equipment slot="Gloves" id="Item.mitten_gauntlets"/>
      </EquipmentRoster>
      <EquipmentRoster siege="true" pool="1">
        <equipment slot="Item0" id="Item.polearm_billhook"/>
        <equipment slot="Body" id="Item.livery_coat_opened_over_brigandine"/>
        <equipment slot="Leg" id="Item.hosen_with_shoes_a"/>
        <equipment slot="Head" id="Item.Item.opened_sallet"/>
        <equipment slot="Cape" id="Item.english_imported_bevor"/>
        <equipment slot="Gloves" id="Item.mitten_gauntlets"/>
      </EquipmentRoster>
      <EquipmentRoster battle="true" siege="true" pool="2">
        <equipment slot="Item0" id="Item.polearm_billhook"/>
        <equipment slot="Body" id="Item.durham_brigandine_over_mail"/>
        <equipment slot="Leg" id="Item.durham_leg_harness"/>
        <equipment slot="Head" id="Item.durham_sallet_with_opened_half_visor"/>
        <equipment slot="Cape" id="Item.durham_bevor_opened"/>
        <equipment slot="Gloves" id="Item.durham_mitten_gauntlets"/>
      </EquipmentRoster>
      <EquipmentSet id="vlandia_troop_civilian_template_t1" civilian="true"/>
    </Equipments>
  </NPCCharacter>
  ```
</details>

In the updated XML:
- We introduced two new `EquipmentRoster` nodes for siege scenarios, each using a billhook instead of a pike.
- Added the `pool="1"` attribute to both existing and new `EquipmentRoster` nodes to consolidate them into one pool.
- Added a new `EquipmentRoster` with the Durham armor, set to `pool="2"`, and flagged with both `battle="true"` and `siege="true"` to apply the armor for both mission types.

These updates make the pikeman unit more effective in sieges and ensures that the new Durham armor set has always the intended esthetic design.
