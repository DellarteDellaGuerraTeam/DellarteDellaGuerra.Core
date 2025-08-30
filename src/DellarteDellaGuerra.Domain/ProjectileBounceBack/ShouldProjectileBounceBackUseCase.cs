using DellarteDellaGuerra.Domain.ProjectileBounceBack.Model;
using DellarteDellaGuerra.Domain.ProjectileBounceBack.Port;

namespace DellarteDellaGuerra.Domain.ProjectileBounceBack
{
    public class ShouldProjectileBounceBackUseCase
    {
        private readonly IMaximumBounceDamageConfigProvider _maximumBounceDamageConfigProvider;
        private readonly IGetArmourPieceMaterial _getArmourPieceMaterial;

        public ShouldProjectileBounceBackUseCase(IMaximumBounceDamageConfigProvider maximumBounceDamageConfigProvider,
            IGetArmourPieceMaterial getArmourPieceMaterial)
        {
            _maximumBounceDamageConfigProvider = maximumBounceDamageConfigProvider;
            _getArmourPieceMaterial = getArmourPieceMaterial;
        }

        private ArmorMaterialType GetMaterialTypeofHitBodyPart(Agent defender,
            BoneBodyPart hitBodyPart)
        {
            BodyArmourPiece bodyArmourPiece = BodyArmourPiece.None;
            if (defender?.IsHuman ?? false)
            {
                if (hitBodyPart == BoneBodyPart.Head || hitBodyPart == BoneBodyPart.Neck)
                    bodyArmourPiece = BodyArmourPiece.Head;
                else if (hitBodyPart == BoneBodyPart.Chest || hitBodyPart == BoneBodyPart.Abdomen ||
                         hitBodyPart == BoneBodyPart.ShoulderLeft || hitBodyPart == BoneBodyPart.ShoulderRight)
                    bodyArmourPiece = BodyArmourPiece.Body;
                else if (hitBodyPart == BoneBodyPart.ArmLeft || hitBodyPart == BoneBodyPart.ArmRight)
                    bodyArmourPiece = BodyArmourPiece.Gloves;
                else if (hitBodyPart == BoneBodyPart.Legs) bodyArmourPiece = BodyArmourPiece.Leg;
                if (bodyArmourPiece != BodyArmourPiece.None)
                    return _getArmourPieceMaterial.GetBodyArmourPieceMaterial(defender.Id, bodyArmourPiece);
            }

            return ArmorMaterialType.None;
        }

        public bool ShouldProjectileBounceBack(int inflictedDamage, Agent defender,
            BoneBodyPart hitBodyPart, DamageType damageType, bool isAlternativeAttack)
        {
            if (!defender.IsHuman) return false;

            ArmorMaterialType mateirialTypeofHitBodyPart = GetMaterialTypeofHitBodyPart(defender, hitBodyPart);
            var config = _maximumBounceDamageConfigProvider.GetMaximumBounceDamageConfigProvider();

            if (damageType == DamageType.Cut &&
                ((mateirialTypeofHitBodyPart == ArmorMaterialType.None && inflictedDamage < config.NakedCutStick) ||
                 (mateirialTypeofHitBodyPart == ArmorMaterialType.Cloth && inflictedDamage < config.ClothCutStick) ||
                 (mateirialTypeofHitBodyPart == ArmorMaterialType.Leather &&
                  inflictedDamage < config.LeatherCutStick) ||
                 (mateirialTypeofHitBodyPart == ArmorMaterialType.Chainmail && inflictedDamage < config.MailCutStick) ||
                 (mateirialTypeofHitBodyPart == ArmorMaterialType.Plate && inflictedDamage < config.PlateCutStick)))
                return true;
            if (damageType == DamageType.Pierce &&
                ((mateirialTypeofHitBodyPart == ArmorMaterialType.None &&
                  inflictedDamage < config.NakedPierceStick) ||
                 (mateirialTypeofHitBodyPart == ArmorMaterialType.Cloth &&
                  inflictedDamage < config.ClothPierceStick) ||
                 (mateirialTypeofHitBodyPart == ArmorMaterialType.Leather &&
                  inflictedDamage < config.LeatherPierceStick) ||
                 (mateirialTypeofHitBodyPart == ArmorMaterialType.Chainmail &&
                  inflictedDamage < config.MailPierceStick) ||
                 (mateirialTypeofHitBodyPart == ArmorMaterialType.Plate &&
                  inflictedDamage < config.PlatePierceStick)))
                return true;
            if (damageType == DamageType.Blunt &&
                ((mateirialTypeofHitBodyPart == ArmorMaterialType.None && inflictedDamage < config.NakedBluntStick) ||
                 (mateirialTypeofHitBodyPart == ArmorMaterialType.Cloth &&
                  inflictedDamage < config.ClothBluntStick) ||
                 (mateirialTypeofHitBodyPart == ArmorMaterialType.Leather &&
                  inflictedDamage < config.LeatherBluntStick) ||
                 (mateirialTypeofHitBodyPart == ArmorMaterialType.Chainmail &&
                  inflictedDamage < config.MailBluntStick) ||
                 (mateirialTypeofHitBodyPart == ArmorMaterialType.Plate && inflictedDamage < config.PlateBluntStick)))
                return true;
            return false;
        }
    }
}