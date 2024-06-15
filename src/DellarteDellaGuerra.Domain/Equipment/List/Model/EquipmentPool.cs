using System.Collections.Generic;
using System.Linq;

namespace DellarteDellaGuerra.Domain.Equipment.List.Model
{
    public class EquipmentPool
    {
        private readonly int _poolId;
        private readonly IList<Equipment> _equipment;

        public EquipmentPool(IList<Equipment> equipment, int poolId)
        {
            _equipment = equipment;
            _poolId = poolId;
        }

        public IList<Equipment> GetEquipmentLoadouts()
        {
            return _equipment;
        }

        public int GetPoolId()
        {
            return _poolId;
        }

        public bool IsEmpty()
        {
            return _equipment.Count == 0;
        }

        private bool Equals(EquipmentPool other)
        {
            return _equipment.SequenceEqual(other._equipment) && _poolId == other._poolId;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((EquipmentPool)obj);
        }

        public override int GetHashCode()
        {
            return _equipment.GetHashCode() ^ _poolId.GetHashCode();
        }
    }
}
