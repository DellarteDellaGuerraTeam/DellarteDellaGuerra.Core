using DellarteDellaGuerra.Domain.Tournament.Reward.Model;

namespace DellarteDellaGuerra.Domain.Tournament.Reward.Port
{
    public interface ITownRepository
    {
        Town? GetTown(string id);
    }
}