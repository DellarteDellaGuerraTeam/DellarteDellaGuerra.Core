using System;
using System.Reflection;
using System.Reflection.Emit;

namespace DellarteDellaGuerra.Integration.SiegeEngines.Mission.Siege.Spawn;

public static class CannonTypeEmitter
{
    private static readonly ModuleBuilder _module = AssemblyBuilder
        .DefineDynamicAssembly(new AssemblyName("DadgDynamicCannons"), AssemblyBuilderAccess.Run)
        .DefineDynamicModule("DadgDynamicCannonsModule");

    public static string GetTypeName(string cannonId) => "GenericCannon_" + cannonId;

    public static Type EmitCannonType(string cannonId)
    {
        var typeBuilder = _module.DefineType(
            GetTypeName(cannonId),
            TypeAttributes.Public | TypeAttributes.Class,
            typeof(GenericCannon)
        );

        return typeBuilder.CreateTypeInfo().AsType();
    }
}
