using DellarteDellaGuerra.Integration.DI;
using Microsoft.Extensions.DependencyInjection;

namespace DellarteDellaGuerra.Integration.Tests.DI
{
    /// <summary>
    /// Guards the composition root against the one mistake it invites: adding a dependency to a
    /// service and forgetting to register it, which the game only reports as a crash on load.
    /// </summary>
    public class DadgServiceContainerTests
    {
        [Fact]
        public void EveryServiceTheContainerRegistersCanBeBuiltFromWhatElseItRegisters()
        {
            // Composition reads nothing from the running game, so the whole graph can be validated
            // out of process. The logger's config path is the one exception and is handed in; null
            // is what the game itself passes when the file is missing.
            IServiceCollection services = new DadgServiceContainer().Compose(loggerConfigPath: null);

            services.BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            });
        }
    }
}
