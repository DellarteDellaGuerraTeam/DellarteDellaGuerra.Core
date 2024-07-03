using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.EquipmentPool.Model;

namespace DellarteDellaGuerra.Domain.EquipmentPool.Util
{
    public class EquipmentPoolPoolPicker : IEquipmentPoolPicker
    {
        private readonly IRandom _random;

        public EquipmentPoolPoolPicker(IRandom random)
        {
            _random = random;
        }

        public Model.EquipmentPool PickEquipmentPool(IList<Model.EquipmentPool> equipmentPools)
        {
            if (equipmentPools is null) return CreateEmptyPool();

            // Creates a list of equipment pools where each pool is present as many times as the number of equipments it contains 
            var equipmentWeightedPools =
                equipmentPools
                    .SelectMany(pool => pool.GetEquipmentLoadouts(), (pool, equipment) => (pool, equipment))
                    .Select(pool => pool.pool)
                    .ToList();

            // If no equipment pools are defined for the troop, then the native roster should be used
            if (equipmentWeightedPools.Count <= 0)
                return CreateEmptyPool();

            var randomIndex = _random.Next(0, equipmentWeightedPools.Count);
            return equipmentWeightedPools.ElementAt(randomIndex);
        }

        private Model.EquipmentPool CreateEmptyPool()
        {
            return new Model.EquipmentPool(new List<Equipment>(), 0);
        }
    }
}