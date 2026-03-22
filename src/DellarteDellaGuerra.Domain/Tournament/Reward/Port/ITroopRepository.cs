using DellarteDellaGuerra.Domain.Tournament.Reward.Model;

namespace DellarteDellaGuerra.Domain.Tournament.Reward.Port
{
    public interface ITroopRepository
    {
        Troop? GetTroop(string id);
    }
}