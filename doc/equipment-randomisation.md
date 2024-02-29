# DADG Equipment Randomisation

## Introduction

This document describes how to control equipment randomisation in the Dell'arte Della Guerra mod. You will learn how Bannerlord's equipment randomisation works and its limits. Additionally, you will discover how DADG overcomes these limitations by introducing an expanded randomisation system.

## Bannerlord's Native Randomisation

### Troop Definition

Troops in Bannerlord are defined by a list of `NPCCharacter` in the xml files.
DADG uses the same system to define its troops.

For instance, a Levy Footman is defined in the [dadg_spnpccharacters.xml](https://github.com/DellarteDellaGuerraTeam/DellarteDellaGuerraMap/blob/main/ModuleData/dadg_spnpccharacters.xml#L997-L1222) file as follows:

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
			<EquipmentRoster>
				<equipment slot="Item0" id="Item.ddg_polearm_longspear"/>
				<equipment slot="Body" id="Item.simple_livery_coat_opened_over_jack"/>
				<equipment slot="Leg" id="Item.hosen_with_boots_a"/>
				<equipment slot="Head" id="Item.rounded_froissart_sallet_without_visor"/>
			</EquipmentRoster>
			<EquipmentRoster>
				<equipment slot="Item0" id="Item.ddg_poleaxe_poleaxea"/>
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

An `EquipmentRoster` is an equipment loadout. It has a list of `equipment` tags which define the equipment slots and the items to be equipped.
The slots are defined by the `slot` attribute which can be `Item0`, `Item1`, `Item2`, `Item3`, `Head`, `Cape`, `Body`, `Leg`, `Gloves`, `Horse` and `HorseHarness`.

The `id` attribute of the `equipment` tag is the item's id to be equipped. It must be prefixed by `Item.` followed by the item id.

#### EquipmentSet

On the other hand, an `EquipmentSet` is a set of `EquipmentRosters`. It is generally used to define civilian clothes for troops as most troops share the same cultural civilian clothes.
It may also be used to define a set of equipment for a specific troop type.

### Randomisation

Bannerlord uses a randomisation system at the start of a battle to equip troops upon their spawn. These include:
- Field battles
- Siege battles
- Garrison sally outs

Intuitively, you might think that the game randomly selects an `EquipmentRoster` from the list of `Equipments` and equips the troop with the defined items.  

However, it works differently; all `EquipmentRoster` and resolved `EquipmentSet` are grouped by slot type. It then selects a random item among the defined slot items for each slot.

The main drawback of this system is that it does not allow the definition of multiple unique equipment loadouts for a given troop, potentially resulting in helmets and armours that do not match each other.

## DADG Equipment Randomisation

In order to overcome the limitations of Bannerlord's native randomisation system, DADG introduces an expanded randomisation system which allows:
- The definition of multiple unique equipment loadouts for a given troop.
- To create multiple equipment pools resulting in troops with equipment loadouts composed of items of a random equipment pool. 
- To still use the native randomisation system.

Note that this system currently does not support civilian clothes.

### Expanded API

DADG now allows `EquipmentRoster` and `EquipmentSet` tags to have a `pool` attribute. This attribute takes a integer value to define the pool of a given equipment loadout.

In case, the `pool` attribute is not defined, the equipment loadout is considered to be part of the default pool.
The default pool has a value of `0`.

So if `pool="0"` is defined, it is considered to be part of the default pool.

### Unique Layouts

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
				<equipment slot="Item0" id="Item.ddg_polearm_longspear"/>
				<equipment slot="Body" id="Item.simple_livery_coat_opened_over_jack"/>
				<equipment slot="Leg" id="Item.hosen_with_boots_a"/>
				<equipment slot="Head" id="Item.rounded_froissart_sallet_without_visor"/>
			</EquipmentRoster>
			<EquipmentRoster pool="2">
				<equipment slot="Item0" id="Item.ddg_poleaxe_poleaxea"/>
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


So now, we have respectively added `pool="1"` and ``pool="2"`` to the first and second `EquipmentRoster` tags of the `Levy Footman` definition.

Let's see what the result is in game:

![2_groups](https://github.com/DellarteDellaGuerraTeam/DellarteDellaGuerraMap/assets/32904771/582536b6-7196-4f33-933b-8d30f2be2d91)

As you can see, the `Levy Footman` troops have two distinct equipment loadouts.

### Random Equipment Pool

DADG also introduces a random equipment pool system. This system allows the definition of multiple equipment pools and to randomly assign a pool to a troop.

It will then use the native randomisation system to equip the troop with items from the assigned pool.

Let's take once again the `Levy Footman` example and add multiple `EquipmentRoster` to a pool and let's define an `EquipmentRoster` in the default pool:

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
			<EquipmentRoster>
				<equipment slot="Item1" id="Item.wakefield_hanger"/>
				<equipment slot="Item0" id="Item.ddg_polearm_halberdb"/>
				<equipment slot="Body" id="Item.breastplate_with_mail"/>
				<equipment slot="Leg" id="Item.hosen_with_shoes_a"/>
				<equipment slot="Head" id="Item.open_decorated_helmet_with_orle"/>
				<equipment slot="Cape" id="Item.english_imported_bevor"/>
				<equipment slot="Gloves" id="Item.mitten_gauntlets"/>
			</EquipmentRoster>
			<EquipmentRoster pool="1">
				<equipment slot="Item0" id="Item.ddg_polearm_longspear"/>
				<equipment slot="Body" id="Item.simple_livery_coat_opened_over_jack"/>
				<equipment slot="Leg" id="Item.hosen_with_boots_a"/>
				<equipment slot="Head" id="Item.rounded_froissart_sallet_without_visor"/>
			</EquipmentRoster>            
			<EquipmentRoster pool="1">
				<equipment slot="Item0" id="Item.ddg_poleaxe_poleaxea"/>
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

So, the first `EquipmentRoster` is part of the default pool and the second and third `EquipmentRoster` are part of the pool `1`.

Let's see what the result is in game:

![default_and_2_eq_pool1](https://github.com/DellarteDellaGuerraTeam/DellarteDellaGuerraMap/assets/32904771/fd3ace8a-e144-4586-96dd-f450392262c4)

As you can see, the `Levy Footman` troops which had the two unique equipment loadouts now share the items of both layouts.

On the other hand, the `Levy Footman` troops which use the default equipment loadout have only the items of the default layout.

Note that if the first `EquipmentRoster` from the `Levy Footman` definition were removed and the `Levy Footman` troops were still assigned to the pool `1`, it would behave the same way as the native randomisation system.

#### Random factors

For now, the system does not support user-defined probabilities for the pools.

Currently, the number of `EquipmentRoster` a pool has is the probability of the pool to be selected.
So in our example, the pool `1` has a 2/3 chance to be selected and the default pool has a 1/3 chance to be selected.

So, the more `EquipmentRoster` a pool has, the more likely is the pool to be selected.
Note that civilian clothes are not taken into account in the probability calculation.

### Compatibility

The expanded randomisation system is fully compatible with the native randomisation system. It does not replace it, it complements it.

If for some reason, pool attributes are defined while DADG is not loaded, the game will ignore them and use the native randomisation system.