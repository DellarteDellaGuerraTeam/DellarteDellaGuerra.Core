using System;
using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Infrastructure.Poc.Patches;
using DellarteDellaGuerra.Infrastructure.Steam.Patches;
using DellarteDellaGuerra.Infrastructure.Utils;
using HarmonyLib;

namespace DellarteDellaGuerra.Infrastructure.Patches
{
    public class HarmonyPatcher
    {
        private readonly ILogger _logger;
        private readonly Harmony _harmony = new ("com.dellartedellaguerra.harmony");

        private readonly List<IPatch> _manualPatches;
        
        public HarmonyPatcher(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<HarmonyPatcher>();
            _manualPatches = new List<IPatch>
            {
                new PocConfigReaderOverriderPatch(_harmony, loggerFactory),
                new FixSettlementFilePathPatch(_harmony, loggerFactory)
            };
        }

        public void PatchAll()
        {
            try
            {
                PatchAllManualPatches();
                PatchAllAutoPatches();
            } catch (Exception e)
            {
                _logger.Error($"Harmony patches failed: {e}");
            }
        }

        private void PatchAllManualPatches()
        {
            _manualPatches.ForEach(patch => patch.Patch());
        }

        private void PatchAllAutoPatches()
        {
            AppDomain.CurrentDomain.GetAssemblies()
                .Where(assembly => assembly.GetName().Name.StartsWith(ModuleId.DellarteDellaGuerra.ToString()))
                .ToList()
                .ForEach(assembly => _harmony.PatchAll(assembly));
        }
    }
}