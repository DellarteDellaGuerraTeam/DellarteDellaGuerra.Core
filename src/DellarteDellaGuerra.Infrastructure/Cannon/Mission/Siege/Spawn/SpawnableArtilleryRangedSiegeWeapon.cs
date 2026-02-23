using Bannerlord.Cannons.BattleMechanics.Artillery;
using TaleWorlds.MountAndBlade.Objects.Siege;

namespace DellarteDellaGuerra.Infrastructure.Cannon.Mission.Siege.Spawn
{
    public class SpawnableArtilleryRangedSiegeWeapon : ArtilleryRangedSiegeWeapon, ISpawnable
    {
        public void SetSpawnedFromSpawner()
        {
            _spawnedFromSpawner = true;
        }
    }
}