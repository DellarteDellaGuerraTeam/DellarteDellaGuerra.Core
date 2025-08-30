using System.Reflection;
using DellarteDellaGuerra.Domain.ProjectileBounceBack;
using DellarteDellaGuerra.Domain.ProjectileBounceBack.Model;
using DellarteDellaGuerra.Domain.ProjectileBounceBack.Model.Mappers;
using DellarteDellaGuerra.Infrastructure.Patches;
using DellarteDellaGuerra.Patches;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using Agent = TaleWorlds.MountAndBlade.Agent;

namespace DellarteDellaGuerra.RBM
{
    public class ProjectileBounceBackLogicPatch2 : IPatch
    {
        private static readonly FieldInfo MissileBlockedWithWeaponField =
            typeof(AttackCollisionData).GetField("_missileBlockedWithWeapon",
                BindingFlags.NonPublic | BindingFlags.Instance);

        private static BoneBodyPartMapper _boneBodyPartMapper;
        private static DamageTypeMapper _damageTypeMapper;
        private static AgentMapper _agentMapper;
        private static ShouldProjectileBounceBackUseCase _shouldProjectileBounceBackUseCase;

        public ProjectileBounceBackLogicPatch2(IPatcher patcher,
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

        public MethodInfo? TargetMethod => typeof(Mission).GetMethod("MissileHitCallback", AccessTools.all);

        public MethodInfo? PatchMethod =>
            typeof(ProjectileBounceBackLogicPatch2).GetMethod("Prefix",
                AccessTools.all);

        public PatchType PatchType => PatchType.Prefix;

        private static bool Prefix(
            ref bool __result,
            ref int extraHitParticleIndex,
            ref AttackCollisionData collisionData,
            Vec3 missileStartingPosition,
            Vec3 missilePosition,
            Vec3 missileAngularVelocity,
            Vec3 movementVelocity,
            MatrixFrame attachGlobalFrame,
            MatrixFrame affectedShieldGlobalFrame,
            int numDamagedAgents,
            Agent attacker,
            Agent victim,
            GameEntity hitEntity)
        {
            if (victim != null && ShouldProjectileBounceBack(victim, collisionData))
            {
                extraHitParticleIndex = -1;
                var tr = __makeref(collisionData);
                MissileBlockedWithWeaponField.SetValueDirect(tr, true);
            }

            return true;
        }

        private static bool ShouldProjectileBounceBack(Agent defender, AttackCollisionData collisionData)
        {
            BoneBodyPart boneBodyPart = _boneBodyPartMapper.Map(collisionData.VictimHitBodyPart);
            DamageType damageType = _damageTypeMapper.Map((DamageTypes)collisionData.DamageType);
            Domain.ProjectileBounceBack.Model.Agent agent = _agentMapper.Map(defender);

            return _shouldProjectileBounceBackUseCase.ShouldProjectileBounceBack(
                collisionData.InflictedDamage, agent,
                boneBodyPart, damageType, collisionData.IsAlternativeAttack);
        }
    }
}