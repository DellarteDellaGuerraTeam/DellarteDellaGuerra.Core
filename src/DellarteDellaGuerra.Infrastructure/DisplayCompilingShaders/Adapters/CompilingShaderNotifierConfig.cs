using DellarteDellaGuerra.Domain.DisplayCompilingShaders.Models;
using DellarteDellaGuerra.Domain.DisplayCompilingShaders.Ports;
using DellarteDellaGuerra.Infrastructure.Configuration.Models;
using DellarteDellaGuerra.Infrastructure.Configuration.Providers;

namespace DellarteDellaGuerra.Infrastructure.DisplayCompilingShaders.Adapters
{
    public class CompilingShaderNotifierConfig : ICompilingShaderNotifierConfig
    {
        private readonly IConfigurationProvider<DadgConfig> _configProvider;

        public CompilingShaderNotifierConfig(IConfigurationProvider<DadgConfig> configProvider)
        {
            _configProvider = configProvider;
        }

        public ShaderNumberNotifierConfiguration GetShaderNumberNotifierConfiguration()
        {
            var config = new ShaderNumberNotifierConfiguration();
            config.IsShaderNumberNotifierEnabled = _configProvider.Config?.EnableShaderCompilationNotifications ?? false;
            return config;
        }
    }
}
