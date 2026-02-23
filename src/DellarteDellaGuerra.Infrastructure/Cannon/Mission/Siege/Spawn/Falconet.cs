using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace DellarteDellaGuerra.Infrastructure.Cannon.Mission.Siege.Spawn
{
    public class Falconet : SpawnableArtilleryRangedSiegeWeapon
    {
        private const string SiegeEngineId = "falconet";

        public override SiegeEngineType GetSiegeEngineType()
        {
            return MBObjectManager.Instance.GetObject<SiegeEngineType>(SiegeEngineId);
        }
    }
}