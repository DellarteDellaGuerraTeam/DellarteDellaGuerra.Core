using System.Collections.Generic;
using System.Reflection;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Domain.SiegeEngines;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace DellarteDellaGuerra.Infrastructure.SiegeEngines;

public class DadgSiegeStrategyActionModel : SiegeStrategyActionModel
{
    private readonly DefaultSiegeStrategyActionModel _baseSiegeStrategyActionModel;
    private readonly MBObjectManager _mbObjectManager;
    private readonly ILogger _logger;
    private readonly GetDefaultSiegeEngine _defaultSiegeEngine;
    private bool _isInitialized;

    public DadgSiegeStrategyActionModel(DefaultSiegeStrategyActionModel baseSiegeStrategyActionModel,
        MBObjectManager mbObjectManager, ILoggerFactory loggerFactory, GetDefaultSiegeEngine defaultSiegeEngine)
    {
        _baseSiegeStrategyActionModel = baseSiegeStrategyActionModel;
        _mbObjectManager = mbObjectManager;
        _defaultSiegeEngine = defaultSiegeEngine;
        _logger = loggerFactory.CreateLogger<DadgSiegeStrategyActionModel>();
    }

    private void OverrideSiegeEngineStrategies()
    {
        SiegeEngineType defaultSiegeEngineType =
            _mbObjectManager.GetObject<SiegeEngineType>(_defaultSiegeEngine.GetSiegeEngine().Id);

        // attacker
        SetPrivateField("_prepareAssaultEngineList", new List<(SiegeEngineType, int)>
        {
            (DefaultSiegeEngineTypes.Ram, 1),
            (defaultSiegeEngineType, 4)
        });
        SetPrivateField("_breachWallsEngineList", new List<(SiegeEngineType, int)>
        {
            (DefaultSiegeEngineTypes.Ram, 1),
            (defaultSiegeEngineType, 4)
        });
        SetPrivateField("_wearOutDefendersEngineList", new List<(SiegeEngineType, int)>
        {
            (DefaultSiegeEngineTypes.Ram, 1),
            (DefaultSiegeEngineTypes.Trebuchet, 4)
        });

        // defender
        SetPrivateField("_prepareAgainstAssaultEngineList", new List<(SiegeEngineType, int)>
        {
            (defaultSiegeEngineType, 4)
        });
        SetPrivateField("_counterBombardmentEngineList", new List<(SiegeEngineType, int)>
        {
            (defaultSiegeEngineType, 4)
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