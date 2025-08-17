using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace DellarteDellaGuerra.Cannon
{
    public class Falconet : SpawnableArtilleryRangedSiegeWeapon
    {
        private const string SiegeEngineId = "falconet";
        private readonly MBObjectManager _mbObjectManager;

        public Falconet(MBObjectManager mbObjectManager)
        {
            _mbObjectManager = mbObjectManager;
        }

        public override SiegeEngineType GetSiegeEngineType()
        {
            return _mbObjectManager.GetObject<SiegeEngineType>(SiegeEngineId);
        }
    }
}