using DellarteDellaGuerra.Domain.Tournament.Reward.Model;
using TaleWorlds.Core;

namespace DellarteDellaGuerra.Tournament.Reward.Spi.Mapper
{
    public interface IItemTierMapper
    {
        ItemObject.ItemTiers Map(ItemTier itemTier);
    }
}