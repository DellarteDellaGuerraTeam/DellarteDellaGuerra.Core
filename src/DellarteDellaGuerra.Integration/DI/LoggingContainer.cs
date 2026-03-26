using System;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Infrastructure.Configuration.Providers;
using DellarteDellaGuerra.Infrastructure.Logging;
using Microsoft.Extensions.DependencyInjection;
using NLog.Extensions.Logging;

namespace DellarteDellaGuerra.Integration.DI;

public class LoggingContainer
{
    public IServiceProvider Build()
    {
        var services = new ServiceCollection();
        services.AddLogging(b => { b.AddNLog(new LoggerConfigPathProvider().Config); });
        services.AddSingleton<ILoggerFactory>(sp =>
            new LoggerFactory(
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>()));

        return services.BuildServiceProvider();
    }

}