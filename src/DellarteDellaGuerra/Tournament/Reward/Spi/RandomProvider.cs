using DellarteDellaGuerra.Domain.Tournament.Reward.Port;
using TaleWorlds.Core;

namespace DellarteDellaGuerra.Tournament.Reward.Spi
{
    public class RandomProvider : IRandomProvider
    {
        public float NextDouble()
        {
            return MBRandom.RandomFloat;
        }
    }
}