using System.Collections.Concurrent;
using DellarteDellaGuerra.Domain.Combat;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using TaleWorlds.Core;

namespace DellarteDellaGuerra.Integration.Combat
{
    /// <summary>
    /// Takes an item's tier from the combat table. The tier also drives its price, and crafted
    /// weapons cannot carry a tier_override.
    /// </summary>
    public class DadgItemValueModel : ItemValueModel
    {
        private readonly CombatItemCatalog _catalog;
        private readonly ILogger _logger;
        // ItemObject.Tierf asks the model on every read, so each fallback is logged once.
        private readonly ConcurrentDictionary<string, byte> _loggedFallbacks = new();

        public DadgItemValueModel(CombatItemCatalog catalog, ILoggerFactory loggerFactory)
        {
            _catalog = catalog;
            _logger = loggerFactory.CreateLogger<DadgItemValueModel>();
        }

        public override float CalculateTier(ItemObject item)
        {
            if (_catalog.TryGetTier(item.StringId, out var tier)) return tier;

            var vanillaTier = BaseModel.CalculateTier(item);
            if ((item.WeaponComponent != null || item.ArmorComponent != null) && _loggedFallbacks.TryAdd(item.StringId, 0))
                _logger.Info($"{item.StringId} has no tier in dadg_combat_items.xml nor a tier_override: using the vanilla tier {vanillaTier:0.##}");
            return vanillaTier;
        }

        public override float GetEquipmentValueFromTier(float itemTierf)
        {
            return BaseModel.GetEquipmentValueFromTier(itemTierf);
        }

        public override int CalculateValue(ItemObject item)
        {
            return BaseModel.CalculateValue(item);
        }

        public override bool GetIsTransferable(ItemObject item)
        {
            return BaseModel.GetIsTransferable(item);
        }
    }
}
