using System.Collections.Generic;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace DellarteDellaGuerra.Integration.SiegeEngines.Campaign;

internal static class CampaignMapSiegePrefabEntityCacheState
{
    internal static readonly Dictionary<string, MatrixFrame> SiegeLaunchFrames = new();
    internal static readonly Dictionary<string, Vec3> SiegeProjectileScales = new();
}
