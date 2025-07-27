using System.Collections.Generic;

namespace DellarteDellaGuerra.Firearm.Reload
{
    public class InMemoryWeaponEntityRepository : IWeaponEntityRepository
    {
        private readonly Dictionary<string, WeaponEntity> _weaponEntityByAgent = new();

        public void SaveWeaponEntity(WeaponEntity entity, string id)
        {
            _weaponEntityByAgent[id] = entity;
        }

        public WeaponEntity? GetWeaponEntity(string id)
        {
            _weaponEntityByAgent.TryGetValue(id, out var weaponEntity);
            return weaponEntity;
        }

        public void Remove(string id)
        {
            _weaponEntityByAgent.Remove(id);
        }
    }
}