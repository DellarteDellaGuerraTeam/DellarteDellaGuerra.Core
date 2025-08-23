using System.Reflection;
using DellarteDellaGuerra.Domain.ProjectileBounceBack;
using DellarteDellaGuerra.Domain.ProjectileBounceBack.Model;
using DellarteDellaGuerra.Domain.ProjectileBounceBack.Model.Mappers;
using DellarteDellaGuerra.Infrastructure.Patches;
using DellarteDellaGuerra.Patches;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using Agent = TaleWorlds.MountAndBlade.Agent;

namespace DellarteDellaGuerra.RBM
{
    public class ProjectileBounceBackLogicPatch : IPatch
    {
        private static BoneBodyPartMapper _boneBodyPartMapper;
        private static DamageTypeMapper _damageTypeMapper;
        private static AgentMapper _agentMapper;
        private static ShouldProjectileBounceBackUseCase _shouldProjectileBounceBackUseCase;

        public ProjectileBounceBackLogicPatch(IPatcher patcher,
            ShouldProjectileBounceBackUseCase shouldProjectileBounceBackUseCase,
            BoneBodyPartMapper boneBodyPartMapper,
            DamageTypeMapper damageTypeMapper,
            AgentMapper agentMapper)
        {
            _shouldProjectileBounceBackUseCase = shouldProjectileBounceBackUseCase;
            _boneBodyPartMapper = boneBodyPartMapper;
            _damageTypeMapper = damageTypeMapper;
            _agentMapper = agentMapper;
            patcher.AddPatch(this);
        }

        public MethodInfo? TargetMethod => typeof(Mission).GetMethod("DecideWeaponCollisionReaction", AccessTools.all);

        public MethodInfo? PatchMethod =>
            typeof(ProjectileBounceBackLogicPatch).GetMethod("DecideWeaponCollisionReactionMOD",
                AccessTools.all);

        public PatchType PatchType => PatchType.Postfix;

        private static void DecideWeaponCollisionReactionMOD(in AttackCollisionData collisionData, Agent defender,
            in MissionWeapon attackerWeapon,
            out MeleeCollisionReaction colReaction)
        {
            if (ShouldProjectileBounceBack(defender, collisionData, !attackerWeapon.IsEmpty))
                colReaction = MeleeCollisionReaction.Bounced;
            else
                colReaction = MeleeCollisionReaction.Stuck;
        }

        private static bool ShouldProjectileBounceBack(Agent defender, AttackCollisionData collisionData,
            bool hasWeapon)
        {
            BoneBodyPart boneBodyPart = _boneBodyPartMapper.Map(collisionData.VictimHitBodyPart);
            DamageType damageType = _damageTypeMapper.Map((DamageTypes)collisionData.DamageType);
            Domain.ProjectileBounceBack.Model.Agent agent = _agentMapper.Map(defender);

            return _shouldProjectileBounceBackUseCase.ShouldProjectileBounceBack(
                collisionData.InflictedDamage, agent,
                boneBodyPart, damageType, collisionData.IsAlternativeAttack, hasWeapon);
        }
    }
}