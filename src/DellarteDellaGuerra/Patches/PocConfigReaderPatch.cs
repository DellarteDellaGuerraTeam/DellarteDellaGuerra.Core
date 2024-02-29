using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using DellarteDellaGuerra.Logging;
using DellarteDellaGuerra.Utils;
using HarmonyLib;
using NLog;

namespace DellarteDellaGuerra.Patches
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
    public class PocConfigReaderPatch
    {
        private static readonly Logger Logger = LoggerFactory.GetLogger<PocConfigReaderPatch>();

        private PocConfigReaderPatch() { }

        /**
         * <summary>
         * Patches the PocColorMod assembly to read the config file from the DellarteDellaGuerra config folder.
         * If the PocColorMod assembly is not loaded, then this method does nothing.
         * </summary>
         */
        public static void Patch(Harmony harmony)
        {
            MethodInfo? originalMethod = ResolveOriginalMethod();
            MethodInfo? patchMethod = ResolvePatchMethod();
            if (originalMethod == null || patchMethod == null)
            {
                Logger.Warn($"{nameof(PocConfigReaderPatch)} failed to resolve the original method or the patch method");
                return;
            }
            harmony.Patch(originalMethod, transpiler: new HarmonyMethod(patchMethod));
        }

        private static MethodInfo? ResolveOriginalMethod()
        {
            return AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault(assembly => assembly.GetName().Name == "PocColor")
                ?.GetType("PocColor.PocColorMod")
                ?.GetMethod("OnGameInitializationFinished", BindingFlags.Instance | BindingFlags.Public);
        }

        private static MethodInfo? ResolvePatchMethod()
        {
            return typeof(PocConfigReaderPatch).GetMethod(nameof(Transpiler), BindingFlags.Static | BindingFlags.NonPublic);
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
                    continue;
                }
                instruction.operand = Path.Combine(configFolderPath, "poc.config.json");
                isInstructionFound = true;
                break;
            }

            if (!isInstructionFound)
            {
                Logger.Error($"{nameof(PocConfigReaderPatch)} failed to find the config file path instruction");
            }
            return instructions;
        }
    }
}


