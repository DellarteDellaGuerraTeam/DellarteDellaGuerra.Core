using DellarteDellaGuerra.Domain.Combat;
using DellarteDellaGuerra.Domain.Combat.Port;
using DellarteDellaGuerra.Infrastructure.Combat;
using DellarteDellaGuerra.Infrastructure.Combat.Spi;
using DellarteDellaGuerra.Infrastructure.Configuration.Providers;
using DellarteDellaGuerra.Integration.Combat;
using Microsoft.Extensions.DependencyInjection;

namespace DellarteDellaGuerra.Integration.DI;

public static class CombatServiceContainer
{
    public static IServiceCollection AddDadgCombat(this IServiceCollection services)
    {
        services.AddSingleton<ICombatFeatureToggle>(sp =>
            new CombatFeatureToggleConfig(sp.GetRequiredService<DadgConfigWatcher>()));
        services.AddSingleton<ICombatItemRepository, CombatItemXmlRepository>();
        services.AddSingleton<CombatItemCatalog>();
        services.AddSingleton(sp => sp.GetRequiredService<CombatItemCatalog>().Absorption);
        services.AddSingleton(sp => sp.GetRequiredService<CombatItemCatalog>().HalfSword);
        services.AddSingleton(sp => sp.GetRequiredService<CombatItemCatalog>().NonTipThrust);
        services.AddSingleton<AgentArmourReader>();
        services.AddSingleton<CombatFeatureInstaller>();
        services.AddTransient<CombatBalanceMissionLogic>();
        return services;
    }
}
