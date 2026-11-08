using DellarteDellaGuerra.Domain.Combat;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ComponentInterfaces;

namespace DellarteDellaGuerra.Integration.Combat
{
    /// <summary>
    /// Takes the armour's absorption off the final damage. AgentApplyDamageModel.CalculateDamage ends with
    /// ApplyGeneralDamageModifiers, so the absorbed fraction comes off last. That step also runs for blocked hits, which
    /// never touched the armour. Everything else stays with the wrapped model.
    /// </summary>
    public class DadgAgentApplyDamageModel : AgentApplyDamageModel
    {
        private readonly AgentArmourReader _armourReader;
        private readonly ArmourAbsorption _absorption;

        public DadgAgentApplyDamageModel(AgentArmourReader armourReader, ArmourAbsorption absorption)
        {
            _armourReader = armourReader;
            _absorption = absorption;
        }

        public override float ApplyGeneralDamageModifiers(in AttackInformation attackInformation,
            in AttackCollisionData collisionData, float baseDamage)
        {
            var damage = BaseModel.ApplyGeneralDamageModifiers(in attackInformation, in collisionData, baseDamage);

            var victim = attackInformation.VictimAgent;
            if (attackInformation.IsVictimAgentMount || victim == null || !victim.IsHuman) return damage;
            if (collisionData.AttackBlockedWithShield || collisionData.CollidedWithShieldOnBack) return damage;
            if (collisionData.IsFallDamage) return damage;

            var weapon = attackInformation.AttackerWeapon;
            var blow = DadgStrikeMagnitudeModel.BlowTypeOf(in attackInformation, in collisionData,
                weapon.IsEmpty ? null : weapon.CurrentUsageItem);
            var material = _armourReader.MaterialAt(victim, collisionData.VictimHitBodyPart);

            return damage * (1f - _absorption.Absorbed(material, blow,
                AgentArmourReader.WeaponClassOf(weapon.IsEmpty ? null : weapon.CurrentUsageItem)));
        }

        public override bool IsDamageIgnored(in AttackInformation attackInformation,
            in AttackCollisionData collisionData)
        {
            return BaseModel.IsDamageIgnored(in attackInformation, in collisionData);
        }

        public override float ApplyDamageAmplifications(in AttackInformation attackInformation,
            in AttackCollisionData collisionData, float baseDamage)
        {
            return BaseModel.ApplyDamageAmplifications(in attackInformation, in collisionData, baseDamage);
        }

        public override float ApplyDamageScaling(in AttackInformation attackInformation,
            in AttackCollisionData collisionData, float baseDamage)
        {
            return BaseModel.ApplyDamageScaling(in attackInformation, in collisionData, baseDamage);
        }

        public override float ApplyDamageReductions(in AttackInformation attackInformation,
            in AttackCollisionData collisionData, float baseDamage)
        {
            return BaseModel.ApplyDamageReductions(in attackInformation, in collisionData, baseDamage);
        }

        public override void DecideMissileWeaponFlags(Agent attackerAgent, in MissionWeapon missileWeapon,
            ref WeaponFlags missileWeaponFlags)
        {
            BaseModel.DecideMissileWeaponFlags(attackerAgent, in missileWeapon, ref missileWeaponFlags);
        }

        public override void CalculateDefendedBlowStunMultipliers(Agent attackerAgent, Agent defenderAgent,
            CombatCollisionResult collisionResult, WeaponComponentData attackerWeapon,
            WeaponComponentData defenderWeapon, ref float attackerStunPeriod, ref float defenderStunPeriod)
        {
            BaseModel.CalculateDefendedBlowStunMultipliers(attackerAgent, defenderAgent, collisionResult,
                attackerWeapon, defenderWeapon, ref attackerStunPeriod, ref defenderStunPeriod);
        }

        public override float CalculateStaggerThresholdDamage(Agent defenderAgent, in Blow blow)
        {
            return BaseModel.CalculateStaggerThresholdDamage(defenderAgent, in blow);
        }

        public override float CalculateAlternativeAttackDamage(in AttackInformation attackInformation,
            in AttackCollisionData collisionData, WeaponComponentData weapon)
        {
            return BaseModel.CalculateAlternativeAttackDamage(in attackInformation, in collisionData, weapon);
        }

        public override float CalculatePassiveAttackDamage(BasicCharacterObject attackerCharacter,
            in AttackCollisionData collisionData, float baseDamage)
        {
            return BaseModel.CalculatePassiveAttackDamage(attackerCharacter, in collisionData, baseDamage);
        }

        public override MeleeCollisionReaction DecidePassiveAttackCollisionReaction(Agent attacker, Agent defender,
            bool isFatalHit)
        {
            return BaseModel.DecidePassiveAttackCollisionReaction(attacker, defender, isFatalHit);
        }

        public override void DecideWeaponCollisionReaction(in Blow registeredBlow, in AttackCollisionData collisionData,
            Agent attacker, Agent defender, in MissionWeapon attackerWeapon, bool isFatalHit, bool isShruggedOff,
            float momentumRemaining, out MeleeCollisionReaction colReaction)
        {
            BaseModel.DecideWeaponCollisionReaction(in registeredBlow, in collisionData, attacker, defender,
                in attackerWeapon, isFatalHit, isShruggedOff, momentumRemaining, out colReaction);
        }

        public override float CalculateShieldDamage(in AttackInformation attackInformation, float baseDamage)
        {
            return BaseModel.CalculateShieldDamage(in attackInformation, baseDamage);
        }

        public override float CalculateSailFireDamage(Agent attackerAgent, IShipOrigin shipOrigin, float baseDamage,
            bool damageFromShipMachine)
        {
            return BaseModel.CalculateSailFireDamage(attackerAgent, shipOrigin, baseDamage, damageFromShipMachine);
        }

        public override float CalculateHullFireDamage(float baseFireDamage, IShipOrigin shipOrigin)
        {
            return BaseModel.CalculateHullFireDamage(baseFireDamage, shipOrigin);
        }

        public override float GetDamageMultiplierForBodyPart(BoneBodyPartType bodyPart, DamageTypes type, bool isHuman,
            bool isMissile)
        {
            return BaseModel.GetDamageMultiplierForBodyPart(bodyPart, type, isHuman, isMissile);
        }

        public override bool CanWeaponIgnoreFriendlyFireChecks(WeaponComponentData weapon)
        {
            return BaseModel.CanWeaponIgnoreFriendlyFireChecks(weapon);
        }

        public override bool CanWeaponDealSneakAttack(in AttackInformation attackInformation,
            WeaponComponentData weapon)
        {
            return BaseModel.CanWeaponDealSneakAttack(in attackInformation, weapon);
        }

        public override bool CanWeaponDismount(Agent attackerAgent, WeaponComponentData attackerWeapon, in Blow blow,
            in AttackCollisionData collisionData)
        {
            return BaseModel.CanWeaponDismount(attackerAgent, attackerWeapon, in blow, in collisionData);
        }

        public override bool CanWeaponKnockback(Agent attackerAgent, WeaponComponentData attackerWeapon, in Blow blow,
            in AttackCollisionData collisionData)
        {
            return BaseModel.CanWeaponKnockback(attackerAgent, attackerWeapon, in blow, in collisionData);
        }

        public override bool CanWeaponKnockDown(Agent attackerAgent, Agent victimAgent,
            WeaponComponentData attackerWeapon, in Blow blow, in AttackCollisionData collisionData)
        {
            return BaseModel.CanWeaponKnockDown(attackerAgent, victimAgent, attackerWeapon, in blow, in collisionData);
        }

        public override bool DecideCrushedThrough(Agent attackerAgent, Agent defenderAgent, float totalAttackEnergy,
            Agent.UsageDirection attackDirection, StrikeType strikeType, WeaponComponentData defendItem,
            bool isPassiveUsageHit)
        {
            return BaseModel.DecideCrushedThrough(attackerAgent, defenderAgent, totalAttackEnergy, attackDirection,
                strikeType, defendItem, isPassiveUsageHit);
        }

        public override float CalculateRemainingMomentum(float originalMomentum, in Blow b,
            in AttackCollisionData collisionData, Agent attacker, Agent victim, in MissionWeapon attackerWeapon,
            bool isCrushThrough)
        {
            return BaseModel.CalculateRemainingMomentum(originalMomentum, in b, in collisionData, attacker, victim,
                in attackerWeapon, isCrushThrough);
        }

        public override bool DecideAgentShrugOffBlow(Agent victimAgent, in AttackCollisionData collisionData,
            in Blow blow)
        {
            return BaseModel.DecideAgentShrugOffBlow(victimAgent, in collisionData, in blow);
        }

        public override bool DecideAgentDismountedByBlow(Agent attackerAgent, Agent victimAgent,
            in AttackCollisionData collisionData, WeaponComponentData attackerWeapon, in Blow blow)
        {
            return BaseModel.DecideAgentDismountedByBlow(attackerAgent, victimAgent, in collisionData, attackerWeapon,
                in blow);
        }

        public override bool DecideAgentKnockedBackByBlow(Agent attackerAgent, Agent victimAgent,
            in AttackCollisionData collisionData, WeaponComponentData attackerWeapon, in Blow blow)
        {
            return BaseModel.DecideAgentKnockedBackByBlow(attackerAgent, victimAgent, in collisionData, attackerWeapon,
                in blow);
        }

        public override bool DecideAgentKnockedDownByBlow(Agent attackerAgent, Agent victimAgent,
            in AttackCollisionData collisionData, WeaponComponentData attackerWeapon, in Blow blow)
        {
            return BaseModel.DecideAgentKnockedDownByBlow(attackerAgent, victimAgent, in collisionData, attackerWeapon,
                in blow);
        }

        public override bool DecideMountRearedByBlow(Agent attackerAgent, Agent victimAgent,
            in AttackCollisionData collisionData, WeaponComponentData attackerWeapon, in Blow blow)
        {
            return BaseModel.DecideMountRearedByBlow(attackerAgent, victimAgent, in collisionData, attackerWeapon,
                in blow);
        }

        public override bool ShouldMissilePassThroughAfterShieldBreak(Agent attackerAgent,
            WeaponComponentData attackerWeapon)
        {
            return BaseModel.ShouldMissilePassThroughAfterShieldBreak(attackerAgent, attackerWeapon);
        }

        public override float GetDismountPenetration(Agent attackerAgent, WeaponComponentData attackerWeapon,
            in Blow blow, in AttackCollisionData collisionData)
        {
            return BaseModel.GetDismountPenetration(attackerAgent, attackerWeapon, in blow, in collisionData);
        }

        public override float GetKnockBackPenetration(Agent attackerAgent, WeaponComponentData attackerWeapon,
            in Blow blow, in AttackCollisionData collisionData)
        {
            return BaseModel.GetKnockBackPenetration(attackerAgent, attackerWeapon, in blow, in collisionData);
        }

        public override float GetKnockDownPenetration(Agent attackerAgent, WeaponComponentData attackerWeapon,
            in Blow blow, in AttackCollisionData collisionData)
        {
            return BaseModel.GetKnockDownPenetration(attackerAgent, attackerWeapon, in blow, in collisionData);
        }

        public override float GetHorseChargePenetration()
        {
            return BaseModel.GetHorseChargePenetration();
        }

    }
}
