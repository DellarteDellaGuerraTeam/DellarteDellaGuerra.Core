using System.Linq;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Domain.ProjectileBounceBack.Model;
using DellarteDellaGuerra.Domain.ProjectileBounceBack.Model.Mappers;
using DellarteDellaGuerra.Domain.ProjectileBounceBack.Port;
using DellarteDellaGuerra.ProjectileBounceBack.Spi.Mapper;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.ProjectileBounceBack.Spi
{
    public class GetArmourPieceMaterial : IGetArmourPieceMaterial
    {
        private readonly BodyArmourPieceMapper _bodyArmourPieceMapper;
        private readonly ArmourMaterialTypeMapper _armourMaterialTypeMapper;
        private readonly ILogger _logger;

        public GetArmourPieceMaterial(BodyArmourPieceMapper bodyArmourPieceMapper,
            ArmourMaterialTypeMapper armourMaterialTypeMapper, ILoggerFactory loggerFactory)
        {
            _bodyArmourPieceMapper = bodyArmourPieceMapper;
            _armourMaterialTypeMapper = armourMaterialTypeMapper;
            _logger = loggerFactory.CreateLogger<GetArmourPieceMaterial>();
        }

        public ArmorMaterialType GetBodyArmourPieceMaterial(string agentId, BodyArmourPiece armourPiece)
        {
            if (Mission.Current is null)
            {
                _logger.Error("Called GetBodyArmourPieceMaterial outside of a mission");
                return ArmorMaterialType.None;
            }

            EquipmentIndex equipmentIndex = _bodyArmourPieceMapper.Map(armourPiece);
            var agent = Mission.Current.Agents
                .First(agent => agent.Index.ToString().Equals(agentId));


            if (agent is null)
            {
                _logger.Error($"Could not find agent with index {agentId}");
                return ArmorMaterialType.None;
            }

            ArmorComponent.ArmorMaterialTypes armorMaterialType = agent.SpawnEquipment[equipmentIndex].Item?
                .ArmorComponent.MaterialType ?? ArmorComponent.ArmorMaterialTypes.None;

            return _armourMaterialTypeMapper.Map(armorMaterialType);
        }
    }
}