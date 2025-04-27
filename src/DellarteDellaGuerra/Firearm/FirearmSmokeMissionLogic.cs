using DellarteDellaGuerra.Domain.Common.Logging.Port;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.Firearm
{
    public class FirearmSmokeMissionLogic : MissionLogic
    {
        private static readonly string FirearmSmokeParticleId = "psys_firearm_panflash";

        private readonly ILogger _logger;

        public FirearmSmokeMissionLogic(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<FirearmSmokeMissionLogic>();
        }
        
        public override void OnAgentShootMissile(Agent shooterAgent, EquipmentIndex weaponIndex, Vec3 position,
            Vec3 velocity, Mat3 orientation,
            bool hasRigidBody, int forcedMissileIndex)
        {
            base.OnAgentShootMissile(shooterAgent, weaponIndex, position, velocity, orientation, hasRigidBody,
                forcedMissileIndex);

            if (shooterAgent is null) return;

            if (!IsValidSmokeParticle()) return;
            
            var weapon = shooterAgent.Equipment[weaponIndex];

            if (weapon.CurrentUsageItem.WeaponClass == WeaponClass.Musket)
            {
                SpawnSmokeParticle(orientation, position);
            }
        }

        private bool IsValidSmokeParticle()
        {
            if (ParticleSystemManager.GetRuntimeIdByName(FirearmSmokeParticleId) < 0)
            {
                _logger.Error(
                    $"Could not apply smoke particle '{FirearmSmokeParticleId}' because it could not be found.");
                return false;
            }

            return true;
        }

        private void SpawnSmokeParticle(Mat3 orientation, Vec3 position)
        {
            var particleParentEntity = GameEntity.CreateEmpty(Mission.Current.Scene);

            MatrixFrame particleFrame = MatrixFrame.Identity;
            RotateSmokeParticleFrame(ref particleFrame);
            CreateSmokeParticle(particleParentEntity, ref particleFrame);

            PositionSmokeParticleAtFirearmButt(particleParentEntity, orientation, position);
        }

        private void RotateSmokeParticleFrame(ref MatrixFrame particleFrame)
        {
            particleFrame.rotation.RotateAboutSide(-MathF.PI / 2);
        }

        private void CreateSmokeParticle(GameEntity particleParentEntity, ref MatrixFrame particleFrame)
        {
            ParticleSystem.CreateParticleSystemAttachedToEntity(FirearmSmokeParticleId, particleParentEntity,
                ref particleFrame);
        }

        private void PositionSmokeParticleAtFirearmButt(GameEntity particleParentEntity, Mat3 orientation, Vec3 position)
        {
            var globalFrame = new MatrixFrame(orientation, position);
            globalFrame.Advance(1f);
            particleParentEntity.SetGlobalFrame(globalFrame);
        }
    }
}