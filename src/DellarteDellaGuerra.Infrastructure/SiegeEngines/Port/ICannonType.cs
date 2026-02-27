using TaleWorlds.Core;

namespace DellarteDellaGuerra.Infrastructure.SiegeEngines.Port;

public interface ICannonType : Domain.SiegeEngines.Model.ICannonType
{
    SiegeEngineType GetSiegeEngineType();
}