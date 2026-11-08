using System.Collections.Concurrent;
using DellarteDellaGuerra.Domain.Combat;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.Integration.Combat
{
    /// <summary>
    /// Finds the armour material a blow meets on an agent.
    /// </summary>
    public class AgentArmourReader
    {
        private readonly CombatItemCatalog _catalog;
        private readonly ILogger _logger;
        // Read on every hit, so each fallback is logged once.
        private readonly ConcurrentDictionary<string, byte> _loggedFallbacks = new();

        public AgentArmourReader(CombatItemCatalog catalog, ILoggerFactory loggerFactory)
        {
            _catalog = catalog;
            _logger = loggerFactory.CreateLogger<AgentArmourReader>();
        }

        public ArmourMaterial MaterialAt(Agent victim, BoneBodyPartType bodyPart)
        {
            return WornBy(victim.SpawnEquipment).MaterialAt(LocationOf(bodyPart));
        }

        public ItemObject? ItemAt(Agent victim, BoneBodyPartType bodyPart)
        {
            var equipment = victim.SpawnEquipment;
            switch (LocationOf(bodyPart))
            {
                case HitLocation.Head:
                    return equipment[EquipmentIndex.Head].Item;
                case HitLocation.Arm:
                    return equipment[EquipmentIndex.Gloves].Item ?? equipment[EquipmentIndex.Body].Item;
                case HitLocation.Legs:
                    return PieceIn(equipment, EquipmentIndex.Body).MaterialAt(HitLocation.Legs)
                           > PieceIn(equipment, EquipmentIndex.Leg).MaterialAt(HitLocation.Legs)
                        ? equipment[EquipmentIndex.Body].Item
                        : equipment[EquipmentIndex.Leg].Item;
                default:
                    return equipment[EquipmentIndex.Body].Item;
            }
        }

        public static CombatWeaponClass WeaponClassOf(WeaponComponentData? usage)
        {
            // Half-sword usages are polearms in native data, but still use a sword blade.
            if (usage?.WeaponDescriptionId == HalfSword.WeaponDescription) return CombatWeaponClass.TwoHandedSword;

            switch (usage?.WeaponClass)
            {
                case WeaponClass.OneHandedSword:
                    return CombatWeaponClass.OneHandedSword;
                case WeaponClass.TwoHandedSword:
                    return CombatWeaponClass.TwoHandedSword;
                case WeaponClass.TwoHandedPolearm:
                    return CombatWeaponClass.TwoHandedPolearm;
                default:
                    return CombatWeaponClass.Default;
            }
        }

        public static BlowType BlowTypeOf(DamageTypes damageType, WeaponClass? weaponClass)
        {
            if (weaponClass is WeaponClass.Cartridge or WeaponClass.Musket or WeaponClass.Pistol)
                return BlowType.Firearm;

            switch (damageType)
            {
                case DamageTypes.Cut:
                    return BlowType.Cut;
                case DamageTypes.Pierce:
                    return BlowType.Pierce;
                default:
                    return BlowType.Blunt;
            }
        }

        private WornArmour WornBy(Equipment equipment)
        {
            return new WornArmour(
                PieceIn(equipment, EquipmentIndex.Head),
                PieceIn(equipment, EquipmentIndex.Body),
                PieceIn(equipment, EquipmentIndex.Gloves),
                PieceIn(equipment, EquipmentIndex.Leg));
        }

        private ArmourPiece PieceIn(Equipment equipment, EquipmentIndex slot)
        {
            var item = equipment[slot].Item;
            if (item == null) return ArmourPiece.Unarmoured;
            if (_catalog.TryGetArmour(item.StringId, out var piece)) return piece;

            var material = MaterialOf(item.ArmorComponent?.MaterialType);
            if (_loggedFallbacks.TryAdd(item.StringId, 0))
                _logger.Info($"{item.StringId} has no armour row in dadg_combat_items.xml: using its material_type " +
                             $"{item.ArmorComponent?.MaterialType.ToString() ?? "(none)"} as {material} on {string.Join("/", LocationsOf(slot))}");
            return ArmourPiece.Covering(material, LocationsOf(slot));
        }

        private static HitLocation[] LocationsOf(EquipmentIndex slot)
        {
            switch (slot)
            {
                case EquipmentIndex.Head:
                    return new[] { HitLocation.Head };
                case EquipmentIndex.Gloves:
                    return new[] { HitLocation.Arm };
                case EquipmentIndex.Leg:
                    return new[] { HitLocation.Legs };
                default:
                    return new[] { HitLocation.Torso, HitLocation.Shoulder, HitLocation.Arm };
            }
        }

        private static ArmourMaterial MaterialOf(ArmorComponent.ArmorMaterialTypes? xmlMaterial)
        {
            switch (xmlMaterial)
            {
                case ArmorComponent.ArmorMaterialTypes.Plate:
                    return ArmourMaterial.Plate;
                case ArmorComponent.ArmorMaterialTypes.Chainmail:
                    return ArmourMaterial.Mail;
                case ArmorComponent.ArmorMaterialTypes.Leather:
                    return ArmourMaterial.Padded;
                default:
                    return ArmourMaterial.Cloth;
            }
        }

        private static HitLocation LocationOf(BoneBodyPartType bodyPart)
        {
            switch (bodyPart)
            {
                case BoneBodyPartType.Head:
                case BoneBodyPartType.Neck:
                    return HitLocation.Head;
                case BoneBodyPartType.ShoulderLeft:
                case BoneBodyPartType.ShoulderRight:
                    return HitLocation.Shoulder;
                case BoneBodyPartType.ArmLeft:
                case BoneBodyPartType.ArmRight:
                    return HitLocation.Arm;
                case BoneBodyPartType.Legs:
                    return HitLocation.Legs;
                default:
                    return HitLocation.Torso;
            }
        }
    }
}
