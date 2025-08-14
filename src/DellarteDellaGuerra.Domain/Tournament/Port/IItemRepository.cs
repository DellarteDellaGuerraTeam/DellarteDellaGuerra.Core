using System.Collections.Generic;
using DellarteDellaGuerra.Domain.Tournament.Model;

namespace DellarteDellaGuerra.Domain.Tournament.Port
{
    public interface IItemRepository
    {
        List<Item> GetItems(ItemTier itemTier);
    }
}