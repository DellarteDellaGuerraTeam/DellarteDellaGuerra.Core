using System.Collections.Generic;

namespace DellarteDellaGuerra.Domain.Combat
{
    /// <summary>
    /// The material of one worn item at each location it covers. Elsewhere it counts as cloth.
    /// </summary>
    public readonly struct ArmourPiece
    {
        public static readonly ArmourPiece Unarmoured = default;

        private readonly IReadOnlyDictionary<HitLocation, ArmourMaterial> _materials;

        public ArmourPiece(IReadOnlyDictionary<HitLocation, ArmourMaterial> materials)
        {
            _materials = materials;
        }

        public static ArmourPiece Covering(ArmourMaterial material, params HitLocation[] locations)
        {
            var materials = new Dictionary<HitLocation, ArmourMaterial>();
            foreach (var location in locations) materials[location] = material;

            return new ArmourPiece(materials);
        }

        public ArmourMaterial MaterialAt(HitLocation location)
        {
            return _materials != null && _materials.TryGetValue(location, out var material)
                ? material
                : ArmourMaterial.Cloth;
        }
    }
}
