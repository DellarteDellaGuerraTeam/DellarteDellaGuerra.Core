using TaleWorlds.Engine;

namespace DellarteDellaGuerra.Firearm.Reload
{
    public interface IWeaponEntityRepository
    {
        void SaveWeaponEntity(MetaMesh entity, string id);

        MetaMesh? GetWeaponEntity(string id);
    }
}