using DellarteDellaGuerra.Domain.Combat;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ComponentInterfaces;

namespace DellarteDellaGuerra.Integration.Combat
{
    /// <summary>
    /// Scales half-sword swings and thrusts behind the point. Everything else stays with the wrapped model,
    /// so armour, shields, perks and the damage formula are untouched.
    /// </summary>
    public class DadgStrikeMagnitudeModel : StrikeMagnitudeCalculationModel
    {
        private readonly HalfSword _halfSword;
        private readonly NonTipThrust _nonTipThrust;

        public DadgStrikeMagnitudeModel(HalfSword halfSword, NonTipThrust nonTipThrust)
        {
            _halfSword = halfSword;
            _nonTipThrust = nonTipThrust;
        }

        public override float CalculateAdjustedArmorForBlow(in AttackInformation attackInformation,
            in AttackCollisionData collisionData, float baseArmor, BasicCharacterObject attackerCharacter,
            BasicCharacterObject attackerCaptainCharacter, BasicCharacterObject victimCharacter,
            BasicCharacterObject victimCaptainCharacter, WeaponComponentData weaponComponent)
        {
            return BaseModel.CalculateAdjustedArmorForBlow(in attackInformation, in collisionData, baseArmor,
                attackerCharacter, attackerCaptainCharacter, victimCharacter, victimCaptainCharacter, weaponComponent);
        }

        // Same conditions as the blunt override in MissionCombatMechanicsHelper.GetAttackCollisionResults.
        public static BlowType BlowTypeOf(in AttackInformation attackInformation,
            in AttackCollisionData collisionData, WeaponComponentData? weaponComponent)
        {
            if (attackInformation.AttackerWeapon.IsEmpty
                || MissionCombatMechanicsHelper.IsCollisionBoneDifferentThanWeaponAttachBone(
                    in collisionData, attackInformation.WeaponAttachBoneIndex)
                || collisionData.IsAlternativeAttack || collisionData.IsFallDamage || collisionData.IsHorseCharge)
                return BlowType.Blunt;

            return AgentArmourReader.BlowTypeOf((DamageTypes)collisionData.DamageType, weaponComponent?.WeaponClass);
        }

        public override float CalculateStrikeMagnitudeForMissile(in AttackInformation attackInformation,
            in AttackCollisionData collisionData, in MissionWeapon weapon, float missileSpeed)
        {
            return BaseModel.CalculateStrikeMagnitudeForMissile(in attackInformation, in collisionData, in weapon,
                missileSpeed);
        }

        public override float CalculateStrikeMagnitudeForSwing(in AttackInformation attackInformation,
            in AttackCollisionData collisionData, in MissionWeapon weapon, float swingSpeed,
            float impactPointAsPercent, float extraLinearSpeed)
        {
            var magnitude = BaseModel.CalculateStrikeMagnitudeForSwing(in attackInformation, in collisionData,
                in weapon, swingSpeed, impactPointAsPercent, extraLinearSpeed);

            return magnitude * _halfSword.SwingMagnitudeScale(weapon.CurrentUsageItem?.WeaponDescriptionId);
        }

        public override float CalculateStrikeMagnitudeForThrust(in AttackInformation attackInformation,
            in AttackCollisionData collisionData, in MissionWeapon weapon, float thrustSpeed,
            float extraLinearSpeed, bool isThrown = false)
        {
            var magnitude = BaseModel.CalculateStrikeMagnitudeForThrust(in attackInformation, in collisionData,
                in weapon, thrustSpeed, extraLinearSpeed, isThrown);

            return !isThrown && TakesNonTipPenalty(in attackInformation, in collisionData)
                ? magnitude * _nonTipThrust.MagnitudeScale(weapon.Item?.WeaponDesign?.Template.StringId)
                : magnitude;
        }

        // Same conditions as the penalty in MissionCombatMechanicsHelper.ComputeBlowMagnitudeMelee, which
        // multiplies the magnitude returned above.
        private static bool TakesNonTipPenalty(in AttackInformation attackInformation,
            in AttackCollisionData collisionData)
        {
            return !collisionData.ThrustTipHit && !collisionData.AttackBlockedWithShield
                && !MissionCombatMechanicsHelper.IsCollisionBoneDifferentThanWeaponAttachBone(
                    in collisionData, attackInformation.WeaponAttachBoneIndex);
        }

        public override float CalculateBaseBlowMagnitudeForPassiveUsage(in AttackInformation attackInformation,
            in AttackCollisionData collisionData, float extraLinearSpeed)
        {
            return BaseModel.CalculateBaseBlowMagnitudeForPassiveUsage(in attackInformation, in collisionData,
                extraLinearSpeed);
        }

        public override float ComputeRawDamage(DamageTypes damageType, float magnitude, float armorEffectiveness,
            float absorbedDamageRatio)
        {
            return BaseModel.ComputeRawDamage(damageType, magnitude, armorEffectiveness, absorbedDamageRatio);
        }

        public override float CalculateStrikeMagnitudeForUnarmedAttack(in AttackInformation attackInformation,
            in AttackCollisionData collisionData, float progressEffect, float momentumRemaining)
        {
            return BaseModel.CalculateStrikeMagnitudeForUnarmedAttack(in attackInformation, in collisionData,
                progressEffect, momentumRemaining);
        }

        public override float GetBluntDamageFactorByDamageType(DamageTypes damageType)
        {
            return BaseModel.GetBluntDamageFactorByDamageType(damageType);
        }

        public override float CalculateHorseArcheryFactor(BasicCharacterObject characterObject)
        {
            return BaseModel.CalculateHorseArcheryFactor(characterObject);
        }
    }
}
