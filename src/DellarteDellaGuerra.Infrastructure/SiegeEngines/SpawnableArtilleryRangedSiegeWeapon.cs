using Bannerlord.Cannons.BattleMechanics.Artillery;
using TaleWorlds.Engine;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Objects.Siege;

namespace DellarteDellaGuerra.Infrastructure.SiegeEngines
{
    public class SpawnableArtilleryRangedSiegeWeapon : ArtilleryRangedSiegeWeapon, ISpawnable
    {
        public override TextObject GetDescriptionText(GameEntity gameEntity = null)
        {
            return new TextObject(string.Empty);
        }

        public void SetSpawnedFromSpawner()
        {
            _spawnedFromSpawner = true;
        }
    }
}
