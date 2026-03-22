using System.Collections.Generic;
using DellarteDellaGuerra.Domain.Tournament.Reward.Model;

namespace DellarteDellaGuerra.Domain.Tournament.Reward.Port
{
    public interface IItemRepository
    {
        List<Item> GetItems(ItemTier itemTier);
    }
}