using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Models.NpcCharacters;

namespace DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Repositories;

public interface INpcCharacterRepository
{
    /// <summary>
    ///     Gets the NPC characters.
    /// </summary>
    /// <returns>Bannerlord's NPCCharacters</returns>
    /// <throws>TechnicalException when an errors occurs while getting the data</throws>
    NpcCharacters GetNpcCharacters();
}