using DellarteDellaGuerra.Domain.DisplayCompilingShaders.Ports;
using TaleWorlds.Engine;

namespace DellarteDellaGuerra.DisplayCompilingShaders.Adapters
{
    public class CompilingShaderNumberProvider : ICompilingShaderNumberProvider
    {
        public int GetNumberOfRemainingShadersToCompile()
        {
            return Utilities.GetNumberOfShaderCompilationsInProgress();
        }
    }
}