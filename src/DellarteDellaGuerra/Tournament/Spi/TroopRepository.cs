using DellarteDellaGuerra.Domain.Tournament.Model;
using DellarteDellaGuerra.Domain.Tournament.Port;
using TaleWorlds.CampaignSystem;
using TaleWorlds.ObjectSystem;

namespace DellarteDellaGuerra.Tournament.Spi
{
    public class TroopRepository : ITroopRepository
    {
        public Troop? GetTroop(string id)
        {
            var characterObject = MBObjectManager.Instance.GetObject<CharacterObject>(id);

            if (characterObject is not null) return new Troop(id, characterObject.IsHero);

            return null;
        }
    }
}