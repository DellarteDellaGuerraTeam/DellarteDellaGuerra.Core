using DellarteDellaGuerra.Domain.DisplayCompilingShaders.Ports;
using DellarteDellaGuerra.Utils;

namespace DellarteDellaGuerra.DisplayCompilingShaders.Adapters
{
    public class CompilingShaderDisplayer : ICompilingShaderDisplayer
    {
        public void DisplayNumberOfRemainingShadersToCompile(int remainingShaders)
        {
            InfoPrinter.Display($"Shader compilation in progress. {remainingShaders} remaining.");
        }
    }
}
