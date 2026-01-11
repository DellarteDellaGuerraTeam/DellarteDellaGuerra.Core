using Bannerlord.Cannons.BattleMechanics.Artillery;
using TaleWorlds.MountAndBlade.Objects.Siege;

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