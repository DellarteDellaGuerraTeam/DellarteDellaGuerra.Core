namespace DellarteDellaGuerra.Domain.Tournament.Reward.Port
{
    public interface IRandomProvider
    {
        float NextDouble();
        int NextInt(int maxExclusive);
    }
}