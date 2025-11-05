using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.Tournament.Model;
using DellarteDellaGuerra.Domain.Tournament.Port;

namespace DellarteDellaGuerra.Domain.Tournament
{
    public class GetTournamentRewardUseCase : IGetTournamentRewardUseCase
    {
        private readonly IItemRepository _itemRepository;
        private readonly ITroopRepository _troopRepository;
        private readonly ITownRepository _townRepository;
        private readonly IRandomProvider _randomProvider;
        private readonly IHighestTownProsperityProvider _highestTownProsperityProvider;

        public GetTournamentRewardUseCase(
            IItemRepository itemRepository,
            ITroopRepository troopRepository,
            ITownRepository townRepository,
            IRandomProvider randomProvider, IHighestTownProsperityProvider highestTownProsperityProvider)
        {
            _itemRepository = itemRepository;
            _troopRepository = troopRepository;
            _townRepository = townRepository;
            _randomProvider = randomProvider;
            _highestTownProsperityProvider = highestTownProsperityProvider;
        }

        public TournamentReward GetTournamentReward(string townId, List<string> participantTroopIds)
        {
            int heroesCount = participantTroopIds
                .Select(id => _troopRepository.GetTroop(id))
                .Count(troop => troop.IsLord);

            var town = _townRepository.GetTown(townId);
            float townProsperity = town.Prosperity;
            float prosperityRatio = townProsperity / _highestTownProsperityProvider.GetHighestTownProsperity();

            if (heroesCount >= 4)
            {
                if (prosperityRatio > 0.8f) return GetRandomTierItem(ItemTier.Tier6);

                float rand = _randomProvider.NextDouble();
                var targetTier = rand switch
                {
                    < 0.7f => ItemTier.Tier4,
                    < 0.9f => ItemTier.Tier5,
                    _ => ItemTier.Tier6
                };
                return GetRandomTierItem(targetTier);
            }
            else
            {
                float rand = _randomProvider.NextDouble();
                var lowerTier = rand < 0.7f ? ItemTier.Tier2 : ItemTier.Tier3;
                return GetRandomTierItem(lowerTier);
            }
        }

        private TournamentReward GetRandomTierItem(ItemTier itemTier)
        {
            var items = _itemRepository.GetItems(itemTier);
            if (items == null || items.Count == 0)
                return null;

            int index = (int)(_randomProvider.NextDouble() * items.Count);
            return new TournamentReward(items[index].Id);
        }
    }
}