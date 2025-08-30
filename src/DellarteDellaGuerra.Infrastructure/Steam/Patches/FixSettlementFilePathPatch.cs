using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Infrastructure.Patches;
using DellarteDellaGuerra.Infrastructure.Utils;
using DellarteDellaGuerra.Patches;
using HarmonyLib;
using SandBox;

namespace DellarteDellaGuerra.Infrastructure.Steam.Patches
{
    /**
     * <summary>
     * This script patches the SettlementPositionScript class to override the settlement file path.
     * </summary>
     * <remarks>
     * This is necessary because the overriden paths point to wrong targets when using Steam Workshop.
     * </remarks>
     */
    public class FixSettlementFilePathPatch : IPatch
    {
        private static ILogger Logger;

        public MethodInfo? TargetMethod =>
            AccessTools.PropertyGetter(typeof(SettlementPositionScript), "SettlementsXmlPath");

        public MethodInfo? PatchMethod =>
            AccessTools.Method(typeof(FixSettlementFilePathPatch), nameof(OverrideSettlementsXmlPath));

        public PatchType PatchType => PatchType.Postfix;

        public FixSettlementFilePathPatch(IPatcher patcher, ILoggerFactory loggerFactory)
        {
            Logger = loggerFactory.CreateLogger<FixSettlementFilePathPatch>();
            patcher.AddPatch(this);
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
    }
}