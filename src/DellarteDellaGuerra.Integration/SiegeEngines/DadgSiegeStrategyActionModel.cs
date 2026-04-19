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
        var attackerCannon = _cannonRepository.GetAllCannons().FirstOrDefault(cannon => cannon.IsAttackerSiegeWeapon);

        SiegeEngineType defaultAttackerSiegeEngineType = null;
        if (attackerCannon is not null)
            defaultAttackerSiegeEngineType = _mbObjectManager.GetObject<SiegeEngineType>(attackerCannon.Id);

        if (attackerCannon is null || defaultAttackerSiegeEngineType is null)
            _logger.Warn("No attacker cannon configured. Attackers will use Trebuchets instead");

        // attacker
        SetPrivateField("_prepareAssaultEngineList", new List<(SiegeEngineType, int)>
        {
            (DefaultSiegeEngineTypes.Ram, 1),
            (defaultAttackerSiegeEngineType is null ? DefaultSiegeEngineTypes.Trebuchet : defaultAttackerSiegeEngineType,
                4)
        });
        SetPrivateField("_breachWallsEngineList", new List<(SiegeEngineType, int)>
        {
            (DefaultSiegeEngineTypes.Ram, 1),
            (defaultAttackerSiegeEngineType is null ? DefaultSiegeEngineTypes.Trebuchet : defaultAttackerSiegeEngineType,
                4)
        });
        SetPrivateField("_wearOutDefendersEngineList", new List<(SiegeEngineType, int)>
        {
            (DefaultSiegeEngineTypes.Ram, 1),
            (DefaultSiegeEngineTypes.Trebuchet, 4)
        });

        var defenderCannon = _cannonRepository.GetAllCannons().FirstOrDefault(cannon => cannon.IsDefensiveSiegeWeapon);

        SiegeEngineType defaultDefenderSiegeEngineType = null;
        if (defenderCannon is not null)
            defaultDefenderSiegeEngineType = _mbObjectManager.GetObject<SiegeEngineType>(defenderCannon.Id);

        if (defenderCannon is null || defaultDefenderSiegeEngineType is null)
            _logger.Warn("No defender cannon configured. Defenders won't have any siege weapons");

        // defender
        SetPrivateField("_prepareAgainstAssaultEngineList", defaultDefenderSiegeEngineType is null
            ? new List<(SiegeEngineType, int)>()
            : new List<(SiegeEngineType, int)>
        {
            (defaultDefenderSiegeEngineType, 4)
        });
        SetPrivateField("_counterBombardmentEngineList", defaultDefenderSiegeEngineType is null
            ? new List<(SiegeEngineType, int)>()
            : new List<(SiegeEngineType, int)>
        {
            (defaultDefenderSiegeEngineType, 4)
        });
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