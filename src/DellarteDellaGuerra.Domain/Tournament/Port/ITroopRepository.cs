using DellarteDellaGuerra.Domain.Tournament.Model;

namespace DellarteDellaGuerra.Domain.Tournament.Port
{
    public interface ITroopRepository
    {
        Troop? GetTroop(string id);
    }
}