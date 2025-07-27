namespace DellarteDellaGuerra.Firearm.Reload
{
    public interface IWeaponEntityRepository
    {
        void SaveWeaponEntity(WeaponEntity entity, string id);

        WeaponEntity? GetWeaponEntity(string id);

        void Remove(string id);
    }
}