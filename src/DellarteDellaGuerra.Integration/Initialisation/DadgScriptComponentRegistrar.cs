using System;
using System.Linq;
using DellarteDellaGuerra.Infrastructure.Utils;
using TaleWorlds.DotNet;

namespace DellarteDellaGuerra.Integration.Initialisation;

public class DadgScriptComponentRegistrar
{
    public void RegisterLoadedDadgTypes()
    {
        var integrationAssembly = typeof(DadgScriptComponentRegistrar).Assembly;
        var scriptTypes = AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly =>
                !assembly.IsDynamic &&
                assembly != integrationAssembly &&
                assembly.GetName().Name?.StartsWith(ModuleIdHelper.GetModuleIdPrefix()) == true)
            .SelectMany(assembly => assembly.GetTypes())
            .GroupBy(type => type.Name)
            .ToDictionary(group => group.Key, group => group.First());

        Managed.AddTypes(scriptTypes);
    }
}