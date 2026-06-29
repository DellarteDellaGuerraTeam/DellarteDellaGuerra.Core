using System.Collections.Generic;

namespace DellarteDellaGuerra.Infrastructure.SiegeEngines;

public interface ICannonRepository
{
    ISet<Cannon> GetAllCannons();
}