using DellarteDellaGuerra.Domain.Combat;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.Integration.Combat
{
    /// <summary>
    /// Temporary diagnostic for balancing the combat overhaul. Logs every blow that lands on a person,
    /// then one table per weapon and one per armour piece when the mission ends. Armour is the item value
    /// before perks. Raw is what the blow would have dealt with no armour, so it
    /// puts back the absorption as well.
    /// Remove once the balance is settled.
    /// </summary>
    public class CombatBalanceMissionLogic : MissionLogic
    {
        private readonly ILogger _logger;
        private readonly CombatItemCatalog _catalog;
        private readonly AgentArmourReader _armourReader;
        private readonly CombatBalanceTally _weapons = new();
        private readonly CombatBalanceTally _armour = new();

        public CombatBalanceMissionLogic(ILoggerFactory loggerFactory, CombatItemCatalog catalog,
            AgentArmourReader armourReader)
        {
            _logger = loggerFactory.CreateLogger<CombatBalanceMissionLogic>();
            _catalog = catalog;
            _armourReader = armourReader;
        }

        public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent, in MissionWeapon affectorWeapon,
            in Blow blow, in AttackCollisionData attackCollisionData)
        {
            base.OnAgentHit(affectedAgent, affectorAgent, in affectorWeapon, in blow, in attackCollisionData);

            if (affectedAgent == null || !affectedAgent.IsHuman || affectorAgent == null) return;
            if (attackCollisionData.CollisionResult != CombatCollisionResult.StrikeAgent) return;
            if (attackCollisionData.IsHorseCharge || attackCollisionData.IsFallDamage) return;
            if (attackCollisionData.AttackBlockedWithShield || attackCollisionData.CollidedWithShieldOnBack) return;

            var bodyPart = blow.VictimBodyPart;
            var usage = affectorWeapon.IsEmpty ? null : affectorWeapon.CurrentUsageItem;
            var blowType = AgentArmourReader.BlowTypeOf(blow.DamageType, usage?.WeaponClass);
            var material = _armourReader.MaterialAt(affectedAgent, bodyPart);
            var armourItem = _armourReader.ItemAt(affectedAgent, bodyPart)?.StringId ?? "none";
            var baseArmour = affectedAgent.GetBaseArmorEffectivenessForBodyPart(bodyPart);
            var item = WeaponOf(affectorAgent, in affectorWeapon, in blow);
            var weapon = item?.StringId ?? "unarmed";
            var tier = item != null && _catalog.TryGetTier(item.StringId, out var listedTier) ? $"T{listedTier}" : "T?";
            var weaponClass = usage?.WeaponClass.ToString() ?? "Unarmed";
            var absorption = _catalog.Absorption.Absorbed(material, blowType, AgentArmourReader.WeaponClassOf(usage));
            var rawDamage = (int)(blow.InflictedDamage / (1f - absorption)) + (int)blow.AbsorbedByArmor;
            var killed = affectedAgent.Health < 1f;
            var skill = usage?.RelevantSkill == null
                ? 0
                : MissionGameModels.Current.AgentStatCalculateModel.GetEffectiveSkill(affectorAgent, usage.RelevantSkill);
            var tip = blow.StrikeType == StrikeType.Thrust && !blow.IsMissile
                ? $" tip={attackCollisionData.ThrustTipHit}"
                : "";
            var gap = affectorAgent.Position.Distance(affectedAgent.Position);

            _weapons.Add($"{weaponClass} {weapon} {tier} {blowType} vs {material}", rawDamage, baseArmour,
                blow.InflictedDamage, killed);
            _armour.Add($"{armourItem} {material} vs {blowType}", rawDamage, baseArmour, blow.InflictedDamage, killed);
            _logger.Debug($"[combat-balance t={Mission.CurrentTime:F1}] {weaponClass} {weapon} {tier} {blowType} " +
                          $"raw={rawDamage} -> {armourItem} {material} {NameOf(bodyPart)} armour={baseArmour:F0} " +
                          $"damage={blow.InflictedDamage} hp={affectedAgent.Health:F0} skill={skill}{tip} " +
                          $"by={affectorAgent.Index} gap={gap:F2} at={attackCollisionData.CollisionDistanceOnWeapon:F2} absorbed={absorption:F2}");
        }

        // A missile's weapon is its ammunition, so a bow is found on the shooter. The shooter may be dead by
        // the time the missile lands, or crewing a machine with a sword in hand.
        private ItemObject? WeaponOf(Agent attacker, in MissionWeapon weapon, in Blow blow)
        {
            if (weapon.IsEmpty) return null;
            if (!blow.IsMissile || _catalog.TryGetTier(weapon.Item.StringId, out _)) return weapon.Item;

            var wielded = attacker.IsActive() ? attacker.WieldedWeapon : MissionWeapon.Invalid;
            var firesThisAmmo = !wielded.IsEmpty
                                && wielded.CurrentUsageItem?.AmmoClass == weapon.CurrentUsageItem?.WeaponClass;
            return firesThisAmmo ? wielded.Item : weapon.Item;
        }

        // The enum gives Head a second name, which ToString prefers.
        private static string NameOf(BoneBodyPartType bodyPart)
        {
            return bodyPart == BoneBodyPartType.Head ? "Head" : bodyPart.ToString();
        }

        protected override void OnEndMission()
        {
            base.OnEndMission();

            foreach (var row in _weapons.Report()) _logger.Info($"[combat-balance weapon] {row}");
            foreach (var row in _armour.Report()) _logger.Info($"[combat-balance armour] {row}");
        }
    }
}
