using DellarteDellaGuerra.Domain.DisplayCompilingShaders.Ports;
using TaleWorlds.Engine;

namespace DellarteDellaGuerra.DisplayCompilingShaders.Providers
{
    public class CompilingShaderNumberProvider : ICompilingShaderNumberProvider
    {
        public int GetNumberOfRemainingShadersToCompile()
        {
            return Utilities.GetNumberOfShaderCompilationsInProgress();
        }
    }
}