using System;
using Bannerlord.Cannons.Api;
using Bannerlord.PrivateWars.Api;
using DellarteDellaGuerra.Infrastructure.SiegeEngines;
using Microsoft.Extensions.DependencyInjection;

namespace DellarteDellaGuerra.Infrastructure.DI;

public static class InfrastructureServiceContainer
{
    public static IServiceCollection AddDadgInfrastructure(this IServiceCollection services)
    {
        services.AddCannonInfrastructure();
        services.AddPrivateWarsInfrastructure();
        return services;
    }

    public static IServiceProvider InitializeDadgInfrastructure(this IServiceProvider provider)
    {
        // Eagerly initialize so LoggerFactoryProvider.Set is called before
        // Bannerlord.Cannons SubModule's OnSubModuleLoad reads it.
        provider.GetRequiredService<ICannonApi>();
        // Eagerly create the private-war API so its factory redirects the mechanism's logging to
        // DADG's logger factory before the Bannerlord.PrivateWars SubModule's OnSubModuleLoad reads it.
        provider.GetRequiredService<IPrivateWarsApi>();
        return provider;
    }

    private static IServiceCollection AddCannonInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton(sp =>
            CannonApiFactory.Create(sp.GetService<Microsoft.Extensions.Logging.ILoggerFactory>()));
        services.AddSingleton<ICannonRepository, CannonRepository>();
        return services;
    }

    private static IServiceCollection AddPrivateWarsInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton(sp =>
            PrivateWarsApiFactory.Create(sp.GetService<Microsoft.Extensions.Logging.ILoggerFactory>()));
        return services;
    }
}
