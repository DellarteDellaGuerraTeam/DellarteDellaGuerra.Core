namespace DellarteDellaGuerra.Domain.Combat.Port
{
    public interface ICombatFeatureToggle
    {
        bool IsCombatOverhaulEnabled { get; }
    }
}
