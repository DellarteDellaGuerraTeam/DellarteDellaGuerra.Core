using DellarteDellaGuerra.Domain.ProjectileBounceBack.Model;

namespace DellarteDellaGuerra.Domain.ProjectileBounceBack.Port
{
    public interface IGetArmourPieceMaterial
    {
        ArmorMaterialType GetBodyArmourPieceMaterial(string agentId, BodyArmourPiece armourPiece);
    }
}