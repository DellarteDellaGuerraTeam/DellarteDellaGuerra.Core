using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Infrastructure.Patches;
using DellarteDellaGuerra.Infrastructure.Utils;
using HarmonyLib;

namespace DellarteDellaGuerra.Infrastructure.Poc.Patches
{
    /**
     * <summary>
     * This script patches the PocColor mod to override the config file path.
     * It is expected to be in the DellarteDellaGuerra config folder and named poc.config.json.
     * </summary>
     * <remarks>
     * It has to be called explicitly and can not rely on Harmony's automatic patching.
     * This is due to the fact that the PocColor mod is not a reference of the DellarteDellaGuerra mod.
     * </remarks>
     */
    public class PocConfigReaderOverriderPatch : IPatch
    {
        private static ILogger Logger;
        private readonly Harmony _harmony;

        public PocConfigReaderOverriderPatch(Harmony harmony, ILoggerFactory loggerFactory)
        {
            Logger = loggerFactory.CreateLogger<PocConfigReaderOverriderPatch>();
            _harmony = harmony;
        }

        /**
         * <summary>
         * Patches the PocColorMod assembly to read the config file from the DellarteDellaGuerra config folder.
         * If the PocColorMod assembly is not loaded, then this method does nothing.
         * </summary>
         */
        public void Patch()
        {
            MethodInfo? originalMethod = ResolveOriginalMethod();
            MethodInfo? patchMethod = ResolvePatchMethod();
            if (originalMethod == null || patchMethod == null)
            {
                Logger.Warn($"{nameof(PocConfigReaderOverriderPatch)} failed to resolve the original method or the patch method");
                return;
            }
            _harmony.Patch(originalMethod, transpiler: new HarmonyMethod(patchMethod));
        }

        private MethodInfo? ResolveOriginalMethod()
        {
            return AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault(assembly => assembly.GetName().Name == "PocColor")
                ?.GetType("PocColor.PocColorMod")
                ?.GetMethod("OnGameInitializationFinished", BindingFlags.Instance | BindingFlags.Public);
        }

        private MethodInfo? ResolvePatchMethod()
        {
            return typeof(PocConfigReaderOverriderPatch).GetMethod(nameof(Transpiler), BindingFlags.Static | BindingFlags.NonPublic);
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codeInstructions)
        {
            var instructions = codeInstructions.ToList();
            var isInstructionFound = false;

            foreach (var instruction in instructions)
            {
                if (instruction.opcode != OpCodes.Ldstr || !instruction.OperandIs("..\\..\\modules\\PocColor\\config.json"))
                {
                    continue;
                }
                string? configFolderPath = ResourceLocator.GetConfigurationFolderPath();
                if (configFolderPath == null)
                {
                    Logger.Error(
                        "Poc config could not be applied because the configuration folder could not be found.");
                    continue;
                }
                instruction.operand = Path.Combine(configFolderPath, "poc.config.json");
                isInstructionFound = true;
                break;
            }

            if (!isInstructionFound)
            {
                Logger.Error($"{nameof(PocConfigReaderOverriderPatch)} failed to find the config file path instruction");
            }
            return instructions;
        }
    }
}


