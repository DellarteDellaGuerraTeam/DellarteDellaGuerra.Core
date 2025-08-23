using TaleWorlds.Core;

namespace DellarteDellaGuerra.Domain.ProjectileBounceBack.Model.Mappers
{
    public class ArmourMaterialTypeMapper
    {
        public ArmorMaterialType Map(ArmorComponent.ArmorMaterialTypes native)
        {
            return native switch
            {
                ArmorComponent.ArmorMaterialTypes.None => ArmorMaterialType.None,
                ArmorComponent.ArmorMaterialTypes.Cloth => ArmorMaterialType.Cloth,
                ArmorComponent.ArmorMaterialTypes.Leather => ArmorMaterialType.Leather,
                ArmorComponent.ArmorMaterialTypes.Chainmail => ArmorMaterialType.Chainmail,
                ArmorComponent.ArmorMaterialTypes.Plate => ArmorMaterialType.Plate,
                _ => ArmorMaterialType.None
            };
        }
    }
}