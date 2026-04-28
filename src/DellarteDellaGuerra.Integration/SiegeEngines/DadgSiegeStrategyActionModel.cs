using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Infrastructure.SiegeEngines;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace DellarteDellaGuerra.Integration.SiegeEngines;

public class DadgSiegeStrategyActionModel : SiegeStrategyActionModel
{
    private const string PrepareAssaultEngineListField = "_prepareAssaultEngineList";
    private const string BreachWallsEngineListField = "_breachWallsEngineList";
    private const string WearOutDefendersEngineListField = "_wearOutDefendersEngineList";
    private const string PrepareAgainstAssaultEngineListField = "_prepareAgainstAssaultEngineList";
    private const string CounterBombardmentEngineListField = "_counterBombardmentEngineList";

    private readonly DefaultSiegeStrategyActionModel _baseSiegeStrategyActionModel;
    private readonly MBObjectManager _mbObjectManager;
    private readonly ILogger _logger;
    private readonly ICannonRepository _cannonRepository;
    private bool _isInitialized;

    public DadgSiegeStrategyActionModel(ICannonRepository cannonRepository, ILoggerFactory loggerFactory,
        MBObjectManager mbObjectManager, DefaultSiegeStrategyActionModel baseSiegeStrategyActionModel)
    {
        _cannonRepository = cannonRepository;
        _logger = loggerFactory.CreateLogger<DadgSiegeStrategyActionModel>();
        _mbObjectManager = mbObjectManager;
        _baseSiegeStrategyActionModel = baseSiegeStrategyActionModel;
    }

    private void OverrideSiegeEngineStrategies()
    {
        var cannons = _cannonRepository.GetAllCannons().ToList();

        var defaultAttackerSiegeEngineType = ResolveConfiguredSiegeEngineType(
            cannons.FirstOrDefault(cannon => cannon.IsAttackerSiegeWeapon));

        if (defaultAttackerSiegeEngineType is null)
            _logger.Warn("No attacker cannon configured. Attackers will use Trebuchets instead");

        var attackerArtillery = defaultAttackerSiegeEngineType ?? DefaultSiegeEngineTypes.Trebuchet;
        SetPrivateField(PrepareAssaultEngineListField, CreateRamAndArtilleryList(attackerArtillery));
        SetPrivateField(BreachWallsEngineListField, CreateRamAndArtilleryList(attackerArtillery));
        SetPrivateField(WearOutDefendersEngineListField, new List<(SiegeEngineType, int)>
        {
            (DefaultSiegeEngineTypes.Ram, 1),
            (DefaultSiegeEngineTypes.Trebuchet, 4)
        });

        var defaultDefenderSiegeEngineType = ResolveConfiguredSiegeEngineType(
            cannons.FirstOrDefault(cannon => cannon.IsDefensiveSiegeWeapon));

        if (defaultDefenderSiegeEngineType is null)
            _logger.Warn("No defender cannon configured. Defenders won't have any siege weapons");

        var defenderEngineList = CreateDefenderEngineList(defaultDefenderSiegeEngineType);
        SetPrivateField(PrepareAgainstAssaultEngineListField, defenderEngineList);
        SetPrivateField(CounterBombardmentEngineListField, defenderEngineList);
    }

    private SiegeEngineType? ResolveConfiguredSiegeEngineType(Cannon? cannon)
    {
        return cannon is null ? null : _mbObjectManager.GetObject<SiegeEngineType>(cannon.Id);
    }

    private static List<(SiegeEngineType, int)> CreateRamAndArtilleryList(SiegeEngineType artillery)
    {
        return new List<(SiegeEngineType, int)>
        {
            (DefaultSiegeEngineTypes.Ram, 1),
            (artillery, 4)
        };
    }

    private static List<(SiegeEngineType, int)> CreateDefenderEngineList(SiegeEngineType? defenderSiegeEngineType)
    {
        return defenderSiegeEngineType is null
            ? new List<(SiegeEngineType, int)>()
            : new List<(SiegeEngineType, int)>
            {
                (defenderSiegeEngineType, 4)
            };
    }

    private void SetPrivateField(string fieldName, object value)
    {
        var field = typeof(DefaultSiegeStrategyActionModel)
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);

        if (field is null) _logger.Error($"Could not find field {fieldName} in  DefaultSiegeStrategyActionModel");

        field?.SetValue(_baseSiegeStrategyActionModel, value);
    }

    public override void GetLogicalActionForStrategy(ISiegeEventSide side, out SiegeAction siegeAction,
        out SiegeEngineType siegeEngineType,
        out int deploymentIndex, out int reserveIndex)
    {
        if (!_isInitialized)
        {
            OverrideSiegeEngineStrategies();
            _isInitialized = true;
        }

        _baseSiegeStrategyActionModel.GetLogicalActionForStrategy(side, out siegeAction, out siegeEngineType,
            out deploymentIndex, out reserveIndex);
    }
}
