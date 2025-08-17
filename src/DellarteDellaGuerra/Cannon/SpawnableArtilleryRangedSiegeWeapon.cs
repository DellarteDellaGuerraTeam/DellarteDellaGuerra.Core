using TaleWorlds.MountAndBlade.Objects.Siege;
using TOR_Core.BattleMechanics.Artillery;

namespace DellarteDellaGuerra.Cannon
{
    public class SpawnableArtilleryRangedSiegeWeapon : ArtilleryRangedSiegeWeapon, ISpawnable
    {
        public void SetSpawnedFromSpawner()
        {
            _spawnedFromSpawner = true;
        }
    }
}