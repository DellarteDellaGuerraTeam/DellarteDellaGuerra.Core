using System.Collections.Generic;
using TaleWorlds.Engine;

namespace DellarteDellaGuerra.Firearm.Reload
{
    public class InMemoryWeaponEntityRepository : IWeaponEntityRepository
    {
        private readonly Dictionary<string, GameEntity> _weaponEntityByAgent = new();

        public void SaveWeaponEntity(GameEntity entity, string id)
        {
            _weaponEntityByAgent[id] = entity;
        }

        public GameEntity GetWeaponEntity(string id)
        {
            if (!_weaponEntityByAgent.ContainsKey(id))
                return null;
            return _weaponEntityByAgent[id];
        }
    }
}