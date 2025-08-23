using System;
using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Infrastructure.Utils;
using DellarteDellaGuerra.Patches;
using HarmonyLib;

namespace DellarteDellaGuerra.Infrastructure.Patches
{
    public class HarmonyPatcher : IPatcher
    {
        private readonly ILogger _logger;
        private readonly Harmony _harmony = new ("com.dellartedellaguerra.harmony");

        private readonly List<IPatch> _manualPatches;
        
        public HarmonyPatcher(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<HarmonyPatcher>();
            _manualPatches = new List<IPatch>();
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

        public void AddPatch(IPatch patch)
        {
            _manualPatches.Add(patch);
        }

        private void PatchAllManualPatches()
        {
            _manualPatches.ForEach(patch =>
            {
                if (patch.PatchMethod is null || patch.TargetMethod is null)
                {
                    _logger.Error(
                        $"Could not apply patch '{patch.GetType()}' because its target or patch methods could not be resolved");
                    return;
                }

                switch (patch.PatchType)
                {
                    case PatchType.Transpiler:
                        _harmony.Patch(
                            patch.TargetMethod,
                            transpiler: new HarmonyMethod(patch.PatchMethod)
                        );
                        break;
                    case PatchType.Prefix:
                        _harmony.Patch(
                            patch.TargetMethod,
                            new HarmonyMethod(patch.PatchMethod)
                        );
                        break;
                    case PatchType.Postfix:
                        _harmony.Patch(
                            patch.TargetMethod,
                            postfix: new HarmonyMethod(patch.PatchMethod)
                        );
                        break;
                    default:
                        _logger.Warn($"Unknown Patch type for {patch.GetType()}");
                        break;
                }
            });
        }

        private void PatchAllAutoPatches()
        {
            AppDomain.CurrentDomain.GetAssemblies()
                .Where(assembly => ModuleIdHelper.GetModuleIds().Contains(assembly.GetName().Name))
                .ToList()
                .ForEach(assembly => _harmony.PatchAll(assembly));
        }
    }
}