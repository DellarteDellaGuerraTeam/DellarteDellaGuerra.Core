using DellarteDellaGuerra.Domain.Tournament.Model;
using TaleWorlds.Core;

namespace DellarteDellaGuerra.Tournament.Spi.Mapper
{
    public interface IItemTierMapper
    {
        ItemObject.ItemTiers Map(ItemTier itemTier);
    }
}