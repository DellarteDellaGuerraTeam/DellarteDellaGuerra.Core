using System;
using Bannerlord.Cannons.Api;
using DellarteDellaGuerra.Infrastructure.SiegeEngines;
using Microsoft.Extensions.DependencyInjection;

namespace DellarteDellaGuerra.Infrastructure.DI;

public static class InfrastructureServiceContainer
{
    public static IServiceCollection AddDadgInfrastructure(this IServiceCollection services)
    {
        services.AddCannonInfrastructure();
        return services;
    }

    public static IServiceProvider InitializeDadgInfrastructure(this IServiceProvider provider)
    {
        // Eagerly initialize so LoggerFactoryProvider.Set is called before
        // Bannerlord.Cannons SubModule's OnSubModuleLoad reads it.
        provider.GetRequiredService<ICannonApi>();
        return provider;
    }

    private static IServiceCollection AddCannonInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton(sp =>
            CannonApiFactory.Create(sp.GetService<Microsoft.Extensions.Logging.ILoggerFactory>()));
        services.AddSingleton<ICannonRepository, CannonRepository>();
        return services;
    }
}
