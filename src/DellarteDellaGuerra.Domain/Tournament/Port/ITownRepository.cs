using DellarteDellaGuerra.Domain.Tournament.Model;

namespace DellarteDellaGuerra.Domain.Tournament.Port
{
    public interface ITownRepository
    {
        Town? GetTown(string id);
    }
}