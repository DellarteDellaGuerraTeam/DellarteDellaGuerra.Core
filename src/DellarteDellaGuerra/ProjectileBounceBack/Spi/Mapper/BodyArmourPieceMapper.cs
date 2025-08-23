using DellarteDellaGuerra.Domain.ProjectileBounceBack.Model;
using TaleWorlds.Core;

namespace DellarteDellaGuerra.ProjectileBounceBack.Spi.Mapper
{
    public class BodyArmourPieceMapper
    {
        public EquipmentIndex Map(BodyArmourPiece bodyArmourPiece)
        {
            return bodyArmourPiece switch
            {
                BodyArmourPiece.None => EquipmentIndex.None,
                BodyArmourPiece.Body => EquipmentIndex.Body,
                BodyArmourPiece.Gloves => EquipmentIndex.Gloves,
                BodyArmourPiece.Leg => EquipmentIndex.Leg,
                BodyArmourPiece.Head => EquipmentIndex.Head,
                BodyArmourPiece.Cape => EquipmentIndex.Cape,
                _ => EquipmentIndex.None
            };
        }
    }
}