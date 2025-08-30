using System.Reflection;
using DellarteDellaGuerra.Domain.ProjectileBounceBack;
using DellarteDellaGuerra.Domain.ProjectileBounceBack.Model;
using DellarteDellaGuerra.Domain.ProjectileBounceBack.Model.Mappers;
using DellarteDellaGuerra.Infrastructure.Patches;
using DellarteDellaGuerra.Patches;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using Agent = TaleWorlds.MountAndBlade.Agent;

namespace DellarteDellaGuerra.RBM
{
    public class NoBloodOnBounceBackPatch : IPatch
    {
        private static BoneBodyPartMapper _boneBodyPartMapper;
        private static DamageTypeMapper _damageTypeMapper;
        private static AgentMapper _agentMapper;
        private static ShouldProjectileBounceBackUseCase _shouldProjectileBounceBackUseCase;

        public NoBloodOnBounceBackPatch(IPatcher patcher,
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

        public MethodInfo? TargetMethod => typeof(Mission).GetMethod("DecideAgentHitParticles", AccessTools.all);

        public MethodInfo? PatchMethod =>
            typeof(NoBloodOnBounceBackPatch).GetMethod("DecideAgentHitParticlesMOD", AccessTools.all);

        public PatchType PatchType => PatchType.Postfix;

        private static void DecideAgentHitParticlesMOD(Blow blow, Agent victim, ref AttackCollisionData collisionData,
            ref HitParticleResultData hprd)
        {
            if (victim == null || (blow.InflictedDamage <= 0 && !(victim.Health <= 0f))) return;
            if (!blow.WeaponRecord.HasWeapon() || blow.WeaponRecord.WeaponFlags.HasFlag(WeaponFlags.NoBlood) ||
                collisionData.IsAlternativeAttack || collisionData.CollidedWithShieldOnBack)
            {
                hprd.StartHitParticleIndex = ParticleSystemManager.GetRuntimeIdByName("psys_game_sweat_sword_enter");
                hprd.ContinueHitParticleIndex = ParticleSystemManager.GetRuntimeIdByName("psys_game_sweat_sword_enter");
                hprd.EndHitParticleIndex = ParticleSystemManager.GetRuntimeIdByName("psys_game_sweat_sword_enter");
                return;
            }

            if (ShouldProjectileBounceBack(victim, collisionData, blow.WeaponRecord.HasWeapon()))
            {
                hprd.StartHitParticleIndex = ParticleSystemManager.GetRuntimeIdByName("psys_game_sweat_sword_enter");
                hprd.ContinueHitParticleIndex =
                    ParticleSystemManager.GetRuntimeIdByName("psys_game_blood_sword_inside");
                hprd.EndHitParticleIndex = ParticleSystemManager.GetRuntimeIdByName("psys_game_sweat_sword_enter");
            }
            else
            {
                hprd.StartHitParticleIndex = ParticleSystemManager.GetRuntimeIdByName("psys_game_blood_sword_enter");
                hprd.ContinueHitParticleIndex =
                    ParticleSystemManager.GetRuntimeIdByName("psys_game_blood_sword_inside");
                hprd.EndHitParticleIndex = ParticleSystemManager.GetRuntimeIdByName("psys_game_blood_sword_exit");
            }
        }

        private static bool ShouldProjectileBounceBack(Agent defender, AttackCollisionData collisionData,
            bool hasWeapon)
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