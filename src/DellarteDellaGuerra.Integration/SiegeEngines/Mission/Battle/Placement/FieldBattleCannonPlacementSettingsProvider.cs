using System;
using DellarteDellaGuerra.Infrastructure.Configuration.Models;
using DellarteDellaGuerra.Infrastructure.Configuration.Providers;

namespace DellarteDellaGuerra.Integration.SiegeEngines.Mission.Battle.Placement;

public class FieldBattleCannonPlacementSettingsProvider : IFieldBattleCannonPlacementSettingsProvider
{
    private const bool DefaultEnabled = true;
    private const int DefaultPlacementLimit = 2;

    private readonly IConfigurationProvider<DadgConfig> _configProvider;

    public FieldBattleCannonPlacementSettingsProvider(IConfigurationProvider<DadgConfig> configProvider)
    {
        _configProvider = configProvider;
    }

    public bool IsEnabled => _configProvider.Config?.EnableFieldBattleCannonPlacement ?? DefaultEnabled;

    public int PlacementLimit => Math.Max(0, _configProvider.Config?.FieldBattleCannonPlacementLimit ?? DefaultPlacementLimit);
}
