using System.Collections.Generic;

namespace DellarteDellaGuerra.Domain.Combat
{
    /// <summary>
    /// Softens the vanilla penalty for a thrust that lands behind the point, for spears and pikes only.
    /// They fight too close in a press to land the tip. Pikes stay lower because their shaft clips heads.
    /// Keyed on the crafting template, as spears, pikes and half-swords share weapon classes and usages.
    /// </summary>
    public class NonTipThrust
    {
        // ManagedParametersEnum.NonTipThrustHitDamageMultiplier in Native's managed_core_parameters.xml.
        public const float VanillaMultiplier = 0.15f;

        public static readonly NonTipThrust None = new(new Dictionary<string, float>());

        private readonly IReadOnlyDictionary<string, float> _multipliers;

        /// <param name="multipliers">The multiplier that replaces the vanilla one, per crafting template.</param>
        public NonTipThrust(IReadOnlyDictionary<string, float> multipliers)
        {
            _multipliers = multipliers;
        }

        /// <summary>The factor that turns the vanilla penalty into this weapon's, applied on top of it.</summary>
        public float MagnitudeScale(string? craftingTemplate)
        {
            return craftingTemplate != null && _multipliers.TryGetValue(craftingTemplate, out var multiplier)
                ? multiplier / VanillaMultiplier
                : 1f;
        }
    }
}
