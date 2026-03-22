using DellarteDellaGuerra.Domain.Tournament.Reward.Model;
using DellarteDellaGuerra.Domain.Tournament.Reward.Port;
using TaleWorlds.CampaignSystem;
using TaleWorlds.ObjectSystem;

namespace DellarteDellaGuerra.Tournament.Reward.Spi
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