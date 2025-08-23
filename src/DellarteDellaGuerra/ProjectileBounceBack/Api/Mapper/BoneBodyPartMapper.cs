using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.Domain.ProjectileBounceBack.Model.Mappers
{
    public class BoneBodyPartMapper
    {
        public BoneBodyPart Map(BoneBodyPartType native)
        {
            return native switch
            {
                BoneBodyPartType.Head => BoneBodyPart.Head,
                BoneBodyPartType.Neck => BoneBodyPart.Neck,
                BoneBodyPartType.Chest => BoneBodyPart.Chest,
                BoneBodyPartType.Abdomen => BoneBodyPart.Abdomen,
                BoneBodyPartType.ShoulderLeft => BoneBodyPart.ShoulderLeft,
                BoneBodyPartType.ShoulderRight => BoneBodyPart.ShoulderRight,
                BoneBodyPartType.ArmLeft => BoneBodyPart.ArmLeft,
                BoneBodyPartType.ArmRight => BoneBodyPart.ArmRight,
                BoneBodyPartType.Legs => BoneBodyPart.Legs,
                _ => BoneBodyPart.Head
            };
        }
    }
}