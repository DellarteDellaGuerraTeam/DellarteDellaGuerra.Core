using DellarteDellaGuerra.Domain.DisplayCompilingShaders.Models;
using DellarteDellaGuerra.Domain.DisplayCompilingShaders.Ports;

namespace DellarteDellaGuerra.Domain.DisplayCompilingShaders
{
    public class DisplayShaderNumber
    {
        private readonly ICompilingShaderNotifierConfig _compilingShaderNotifierConfig;
        private readonly ICompilingShaderNumberProvider _compilingShaderNumberProvider;
        private readonly ICompilingShaderDisplayer _compilingShaderDisplayer;

        public DisplayShaderNumber(ICompilingShaderNotifierConfig compilingShaderNotifierConfig,
            ICompilingShaderNumberProvider compilingShaderNumberProvider, ICompilingShaderDisplayer compilingShaderDisplayer)
        {
            _compilingShaderNotifierConfig = compilingShaderNotifierConfig;
            _compilingShaderNumberProvider = compilingShaderNumberProvider;
            _compilingShaderDisplayer = compilingShaderDisplayer;
        }

        public bool DisplayNumberOfRemainingCompilingShaders()
        {
            ShaderNumberNotifierConfiguration config =
                _compilingShaderNotifierConfig.GetShaderNumberNotifierConfiguration();

            if (!config.IsShaderNumberNotifierEnabled)
            {
                return false;
            }

            int shaders = _compilingShaderNumberProvider.GetNumberOfRemainingShadersToCompile();
            if (shaders <= 0)
            {
                return false;
            }

            _compilingShaderDisplayer.DisplayNumberOfRemainingShadersToCompile(shaders);
            return true;
        }
    }
}
