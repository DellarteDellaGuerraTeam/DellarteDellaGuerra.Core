using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.Tournament.Reward.Model;
using DellarteDellaGuerra.Domain.Tournament.Reward.Port;
using DellarteDellaGuerra.Tournament.Reward.Spi.Mapper;
using TaleWorlds.CampaignSystem.Extensions;

namespace DellarteDellaGuerra.Tournament.Reward.Spi
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