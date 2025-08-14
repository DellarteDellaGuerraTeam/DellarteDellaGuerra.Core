using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Domain.Tournament.Model;
using TaleWorlds.Core;

namespace DellarteDellaGuerra.Tournament.Spi.Mapper
{
    public class ItemTierMapper : IItemTierMapper
    {
        private readonly ILogger _logger;

        public ItemTierMapper(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ItemTierMapper>();
        }

        public ItemObject.ItemTiers Map(ItemTier itemTier)
        {
            switch (itemTier)
            {
                case ItemTier.Tier1: return ItemObject.ItemTiers.Tier1;
                case ItemTier.Tier2: return ItemObject.ItemTiers.Tier2;
                case ItemTier.Tier3: return ItemObject.ItemTiers.Tier3;
                case ItemTier.Tier4: return ItemObject.ItemTiers.Tier4;
                case ItemTier.Tier5: return ItemObject.ItemTiers.Tier5;
                case ItemTier.Tier6: return ItemObject.ItemTiers.Tier6;
                default:
                    _logger.Error($"Unknown item tier {itemTier}");
                    return ItemObject.ItemTiers.Tier1;
            }
        }
    }
}