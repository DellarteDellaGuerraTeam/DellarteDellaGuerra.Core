using TaleWorlds.Engine;

namespace DellarteDellaGuerra.Firearm.Reload
{
    public interface IWeaponEntityRepository
    {
        void SaveWeaponEntity(GameEntity entity, string id);

        GameEntity? GetWeaponEntity(string id);
    }
}