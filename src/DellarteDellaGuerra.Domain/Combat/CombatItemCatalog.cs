using DellarteDellaGuerra.Domain.Combat.Port;

namespace DellarteDellaGuerra.Domain.Combat
{
    /// <summary>
    /// Holds the tier of each weapon and the materials of each armour, by item id, and the combat rules
    /// loaded with them.
    /// </summary>
    public class CombatItemCatalog
    {
        private readonly CombatItems _items;

        public CombatItemCatalog(ICombatItemRepository repository)
        {
            _items = repository.Load();
        }

        public ArmourAbsorption Absorption => _items.Absorption;
        public HalfSword HalfSword => _items.HalfSword;
        public NonTipThrust NonTipThrust => _items.NonTipThrust;

        public bool TryGetTier(string itemId, out int tier)
        {
            return _items.Tiers.TryGetValue(itemId, out tier);
        }

        public bool TryGetArmour(string itemId, out ArmourPiece piece)
        {
            return _items.Armours.TryGetValue(itemId, out piece);
        }
    }
}
