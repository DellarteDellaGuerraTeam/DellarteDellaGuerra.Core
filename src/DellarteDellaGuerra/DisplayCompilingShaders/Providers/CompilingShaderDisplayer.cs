using DellarteDellaGuerra.Domain.DisplayCompilingShaders.Ports;
using DellarteDellaGuerra.Utils;

namespace DellarteDellaGuerra.DisplayCompilingShaders.Providers
{
    public class CompilingShaderDisplayer : ICompilingShaderDisplayer
    {
        public void DisplayNumberOfRemainingShadersToCompile(int remainingShaders)
        {
            InfoPrinter.Display($"Shader compilation in progress. {remainingShaders} remaining.");
        }
    }
}
