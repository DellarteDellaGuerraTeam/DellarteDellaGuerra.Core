using DellarteDellaGuerra.Logging;
using DellarteDellaGuerra.Utils;
using HarmonyLib;
using NLog;
using SandBox;

namespace DellarteDellaGuerra.Patches
{
    /**
     * <summary>
     * This script patches the SettlementPositionScript class to override the settlements file path and the settlements distance cache file path.
     * </summary>
     * <remarks>
     * This is necessary because the overriden paths point to wrong targets when using Steam Workshop.
     * </remarks>
     */
    [HarmonyPatch]
    public class SettlementPositionScriptPatch : SettlementPositionScript
    {
        private static readonly Logger Logger = LoggerFactory.GetLogger<SettlementPositionScriptPatch>();
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(SettlementPositionScript), "get_SettlementsXmlPath")]
        private static void OverrideSettlementsXmlPath(ref string __result)
        {
            string? path = ResourceLocator.GetModuleDataSettlementFilePath();
            if (path == null)
            {
                Logger.Error("Could not find the settlement file");
                return;
            }
            __result = path;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(SettlementPositionScript), "get_SettlementsDistanceCacheFilePath")]
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
