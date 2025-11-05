using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.Tournament.Model;
using DellarteDellaGuerra.Domain.Tournament.Port;
using DellarteDellaGuerra.Tournament.Spi.Mapper;
using TaleWorlds.CampaignSystem.Extensions;

namespace DellarteDellaGuerra.Tournament.Spi
{
    public class ItemRepository : IItemRepository
    {
        private readonly IItemTierMapper _itemTierMapper;

        public ItemRepository(IItemTierMapper itemTierMapper)
        {
            _itemTierMapper = itemTierMapper;
        }

        public List<Item> GetItems(ItemTier itemTier)
        {
            return Items.All.Where(item => _itemTierMapper.Map(itemTier).Equals(item.Tier))
                .Select(item => new Item(item.StringId)).ToList();
        }
    }
}