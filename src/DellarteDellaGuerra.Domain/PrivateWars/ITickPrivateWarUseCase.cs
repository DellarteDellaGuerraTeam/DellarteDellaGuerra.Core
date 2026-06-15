using DellarteDellaGuerra.Domain.PrivateWars.Model;

namespace DellarteDellaGuerra.Domain.PrivateWars
{
    public interface ITickPrivateWarUseCase
    {
        TickResult Execute(PrivateWar war, PrivateWarObservations observations, float currentDay);
    }
}
