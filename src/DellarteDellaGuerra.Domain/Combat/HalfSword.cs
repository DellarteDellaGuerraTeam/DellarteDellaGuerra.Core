namespace DellarteDellaGuerra.Domain.Combat
{
    /// <summary>
    /// Weakens swings in the half-sword grip. The usage takes its speed from the two-handed grip and its
    /// damage from the blade, but a hand on the blade cannot cut at full strength.
    /// </summary>
    public class HalfSword
    {
        public const string WeaponDescription = "dadg_Halfsword";

        public static readonly HalfSword None = new(1f);

        private readonly float _swingMultiplier;

        public HalfSword(float swingMultiplier)
        {
            _swingMultiplier = swingMultiplier;
        }

        /// <summary>The factor applied to a swing's magnitude.</summary>
        public float SwingMagnitudeScale(string? weaponDescription)
        {
            return weaponDescription == WeaponDescription ? _swingMultiplier : 1f;
        }
    }
}
