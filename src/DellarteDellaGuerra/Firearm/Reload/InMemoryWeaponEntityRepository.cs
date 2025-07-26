using System.Collections.Generic;
using TaleWorlds.Engine;

namespace DellarteDellaGuerra.Firearm.Reload
{
    public class InMemoryWeaponEntityRepository : IWeaponEntityRepository
    {
        private readonly Dictionary<string, MetaMesh> _weaponEntityByAgent = new();

        public void SaveWeaponEntity(MetaMesh entity, string id)
        {
            _weaponEntityByAgent[id] = entity;
        }

        public MetaMesh GetWeaponEntity(string id)
        {
            if (!_weaponEntityByAgent.ContainsKey(id))
                return null;
            return _weaponEntityByAgent[id];
        }
    }
}