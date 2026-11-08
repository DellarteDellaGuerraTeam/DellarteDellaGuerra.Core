using System.Collections.Generic;

namespace DellarteDellaGuerra.Domain.Combat
{
    /// <summary>
    /// The per-item combat data the game's XML has no field for, and the rules that use it.
    /// </summary>
    public class CombatItems
    {
        public static readonly CombatItems Empty = new(new Dictionary<string, int>(),
            new Dictionary<string, ArmourPiece>(), ArmourAbsorption.None, HalfSword.None, NonTipThrust.None);

        public CombatItems(IReadOnlyDictionary<string, int> tiers, IReadOnlyDictionary<string, ArmourPiece> armours,
            ArmourAbsorption absorption, HalfSword halfSword, NonTipThrust nonTipThrust)
        {
            Tiers = tiers;
            Armours = armours;
            Absorption = absorption;
            HalfSword = halfSword;
            NonTipThrust = nonTipThrust;
        }

        public IReadOnlyDictionary<string, int> Tiers { get; }
        public IReadOnlyDictionary<string, ArmourPiece> Armours { get; }
        public ArmourAbsorption Absorption { get; }
        public HalfSword HalfSword { get; }
        public NonTipThrust NonTipThrust { get; }
    }
}
