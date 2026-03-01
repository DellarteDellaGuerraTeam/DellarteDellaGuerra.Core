using System;
using System.Linq;
using System.Reflection;
using DellarteDellaGuerra.Infrastructure.Cannon.Infra.Repo;
using Harmony.DependencyInjection.Patches;
using HarmonyLib;
using SandBox.ViewModelCollection.MapSiege;

namespace DellarteDellaGuerra.Infrastructure.Cannon.Campaign.UI;

public class MapSiegePOIVMPatch : IPatch
{
    private static IMapSiegeEngineIconRepository _iconRepository;

    public MapSiegePOIVMPatch(IMapSiegeEngineIconRepository iconRepository)
    {
        _iconRepository = iconRepository;
    }

    private static int? GetCustomMachineId(string stringId)
    {
        return _iconRepository.MapSiegeEngineIcons.FirstOrDefault(siegeEngineIcon =>
            siegeEngineIcon.SpriteId.Equals(stringId, StringComparison.InvariantCultureIgnoreCase))?.MachineType;
    }

    public MethodInfo? TargetMethod => ResolveOriginalMethod();

    public MethodInfo? PatchMethod => ResolvePatchMethod();

    public PatchType PatchType => PatchType.Postfix;

    private static void PostfixRefreshMachineType(MapSiegePOIVM __instance)
    {
        var machine = __instance.Machine;
        
        if (machine is null) return;
        
        var machineType = GetCustomMachineId(machine.SiegeEngine.StringId);

        if (machineType is null) return;

        AccessTools.Field(typeof(MapSiegePOIVM), "_bindMachineType")
            .SetValue(__instance, machineType);
    }

    private static MethodInfo? ResolveOriginalMethod()
    {
        return typeof(MapSiegePOIVM).GetMethod(
            "RefreshMachineType",
            BindingFlags.Instance | BindingFlags.NonPublic
        );
    }

    private static MethodInfo? ResolvePatchMethod()
    {
        return typeof(MapSiegePOIVMPatch).GetMethod(
            nameof(PostfixRefreshMachineType),
            BindingFlags.Static | BindingFlags.NonPublic
        );
    }
}