using System;
using System.Collections.Generic;

namespace DellarteDellaGuerra.Domain.Combat
{
    /// <summary>
    /// The fraction of a blow's final damage the material at the hit location soaks up, on top of the
    /// vanilla formula's armour value. A weapon-class entry replaces the blow's Default fraction.
    /// Firearms meet only the armour value.
    /// </summary>
    public class ArmourAbsorption
    {
        public static readonly ArmourAbsorption None =
            new(new Dictionary<(ArmourMaterial, BlowType, CombatWeaponClass), float>());

        private readonly IReadOnlyDictionary<(ArmourMaterial, BlowType, CombatWeaponClass), float> _fractions;

        /// <param name="fractions">Fractions per material, blow and weapon class; missing classes use Default.</param>
        public ArmourAbsorption(IReadOnlyDictionary<(ArmourMaterial, BlowType, CombatWeaponClass), float> fractions)
        {
            foreach (var fraction in fractions.Values)
                if (float.IsNaN(fraction) || fraction < 0f || fraction >= 1f)
                    throw new ArgumentOutOfRangeException(nameof(fractions), "Absorption must be at least 0 and below 1.");

            _fractions = fractions;
        }

        public float Absorbed(ArmourMaterial material, BlowType blow,
            CombatWeaponClass weaponClass = CombatWeaponClass.Default)
        {
            if (blow == BlowType.Firearm) return 0f;

            if (_fractions.TryGetValue((material, blow, weaponClass), out var fraction)) return fraction;

            return _fractions.TryGetValue((material, blow, CombatWeaponClass.Default), out fraction) ? fraction : 0f;
        }
    }
}
