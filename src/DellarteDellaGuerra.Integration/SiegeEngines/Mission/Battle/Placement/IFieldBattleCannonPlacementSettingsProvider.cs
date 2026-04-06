namespace DellarteDellaGuerra.Integration.SiegeEngines.Mission.Battle.Placement;

public interface IFieldBattleCannonPlacementSettingsProvider
{
    bool IsEnabled { get; }
    int PlacementLimit { get; }
}
