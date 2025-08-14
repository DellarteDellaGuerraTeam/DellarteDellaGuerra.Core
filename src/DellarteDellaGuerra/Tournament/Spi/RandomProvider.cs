using DellarteDellaGuerra.Domain.Tournament.Port;
using TaleWorlds.Core;

namespace DellarteDellaGuerra.Tournament.Spi
{
    public class RandomProvider : IRandomProvider
    {
        public float NextDouble()
        {
            return MBRandom.RandomFloat;
        }
    }
}