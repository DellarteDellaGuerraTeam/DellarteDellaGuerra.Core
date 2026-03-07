using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace DellarteDellaGuerra.Infrastructure.SiegeEngines;

public class SiegeEngineTypeProvider
{
    private readonly MBObjectManager _mbObjectManager;

    public SiegeEngineTypeProvider(MBObjectManager mbObjectManager)
    {
        _mbObjectManager = mbObjectManager;
    }

    public SiegeEngineType GetSiegeEngineType(string cannonId)
    {
        return _mbObjectManager.GetObject<SiegeEngineType>(cannonId);
    }
}