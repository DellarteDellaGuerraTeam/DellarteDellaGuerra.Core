using DellarteDellaGuerra.Domain.DisplayCompilingShaders.Models;

namespace DellarteDellaGuerra.Domain.DisplayCompilingShaders.Ports
{
    public interface ICompilingShaderNotifierConfig
    {
        ShaderNumberNotifierConfiguration GetShaderNumberNotifierConfiguration();
    }
}
