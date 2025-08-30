using System.Collections.Generic;

namespace DellarteDellaGuerra.Infrastructure.Utils;

public class ModuleIdHelper
{
    public static ISet<string> GetModuleIds()
    {
        return new HashSet<string>
        {
            "DellarteDellaGuerra.Core",
            "DellarteDellaGuerra",
            "DellarteDellaGuerraMap",
            "DellarteDellaGuerraScenes"
        };
    }
}