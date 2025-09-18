using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Infrastructure.Utils;
using Harmony.DependencyInjection.Patches;
using HarmonyLib;
using SandBox.View.Map;

namespace DellarteDellaGuerra.Infrastructure.Steam.Patches;

/**
 * <summary>
 *     This script patches the FixSettlementDistanceCacheFilePathPatch class to override the settlement distance cache
 *     file path.
 * </summary>
 * <remarks>
 *     This is necessary because the overriden paths point to wrong targets when using Steam Workshop.
 * </remarks>
 */
public class FixSettlementDistanceCacheFilePathPatch : IPatch
{
    private static ILogger Logger;

    public MethodInfo? TargetMethod =>
        AccessTools.PropertyGetter(typeof(SettlementPositionScript), "SettlementsDistanceCacheFilePath");

    public MethodInfo? PatchMethod => AccessTools.Method(typeof(FixSettlementDistanceCacheFilePathPatch),
        nameof(OverrideSettlementsDistanceCacheFilePath));

    public PatchType PatchType => PatchType.Postfix;

    public FixSettlementDistanceCacheFilePathPatch(ILoggerFactory loggerFactory)
    {
        Logger = loggerFactory.CreateLogger<FixSettlementDistanceCacheFilePathPatch>();
    }


    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private static void OverrideSettlementsDistanceCacheFilePath(ref string __result)
    {
        string? path = ResourceLocator.GetSettlementDistanceCacheFilePath();
        if (path == null)
        {
            Logger.Error("Could not find the settlement distance cache file");
            return;
        }

        __result = path;
    }
}