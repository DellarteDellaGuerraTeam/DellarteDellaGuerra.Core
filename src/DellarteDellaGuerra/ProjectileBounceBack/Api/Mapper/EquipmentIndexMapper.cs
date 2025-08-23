using TaleWorlds.Core;

namespace DellarteDellaGuerra.Domain.ProjectileBounceBack.Model.Mappers
{
    public class EquipmentIndexMapper
    {
        public BodyArmourPiece Map(EquipmentIndex native)
        {
            return native switch
            {
                EquipmentIndex.Head => BodyArmourPiece.Head,
                EquipmentIndex.Body => BodyArmourPiece.Body,
                EquipmentIndex.Leg => BodyArmourPiece.Leg,
                EquipmentIndex.Gloves => BodyArmourPiece.Gloves,
                EquipmentIndex.Cape => BodyArmourPiece.Cape,
                _ => BodyArmourPiece.None
            };
        }
    }
}