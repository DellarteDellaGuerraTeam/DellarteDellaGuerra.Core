using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace DellarteDellaGuerra.Cannon
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