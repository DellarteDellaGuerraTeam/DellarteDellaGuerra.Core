using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Infrastructure.Patches;
using DellarteDellaGuerra.Infrastructure.Utils;
using HarmonyLib;
using SandBox;

namespace DellarteDellaGuerra.Infrastructure.Steam.Patches
{
    /**
     * <summary>
     * This script patches the SettlementPositionScript class to override the settlements file path and the settlements distance cache file path.
     * </summary>
     * <remarks>
     * This is necessary because the overriden paths point to wrong targets when using Steam Workshop.
     * </remarks>
     */
    public class FixSettlementFilePathPatch : IPatch
    {
        private static ILogger Logger;
        private readonly Harmony _harmony;

        private readonly MethodInfo? _settlementsXmlPathMethod =
            AccessTools.PropertyGetter(typeof(SettlementPositionScript), "SettlementsXmlPath");

        private readonly MethodInfo? _settlementsDistanceCacheFilePathMethod =
            AccessTools.PropertyGetter(typeof(SettlementPositionScript), "SettlementsDistanceCacheFilePath");

        private readonly MethodInfo _getSettlementsXmlPathPostfix =
            AccessTools.Method(typeof(FixSettlementFilePathPatch), nameof(OverrideSettlementsXmlPath));

        private readonly MethodInfo _getSettlementsDistanceCacheFilePathPostfix =
            AccessTools.Method(typeof(FixSettlementFilePathPatch),
                nameof(OverrideSettlementsDistanceCacheFilePath));

        public FixSettlementFilePathPatch(Harmony harmony, ILoggerFactory loggerFactory)
        {
            Logger = loggerFactory.CreateLogger<FixSettlementFilePathPatch>();
            _harmony = harmony;
        }

        public void Patch()
        {
            if (_settlementsXmlPathMethod is null || _settlementsDistanceCacheFilePathMethod is null)
            {
                Logger.Warn($"{nameof(FixSettlementFilePathPatch)} failed to resolve the original methods");
                return;
            }

            _harmony.Patch(_settlementsXmlPathMethod, postfix: new HarmonyMethod(_getSettlementsXmlPathPostfix));
            _harmony.Patch(_settlementsDistanceCacheFilePathMethod,
                postfix: new HarmonyMethod(_getSettlementsDistanceCacheFilePathPostfix));
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static void OverrideSettlementsXmlPath(ref string __result)
        {
            string? path = ResourceLocator.GetSettlementsFilePath();
            if (path == null)
            {
                Logger.Error("Could not find the settlement file");
                return;
            }

            __result = path;
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
}