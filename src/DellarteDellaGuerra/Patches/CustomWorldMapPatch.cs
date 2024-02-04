using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using SandBox;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace DellarteDellaGuerra.Patches
{
    [HarmonyPatch]
    public static class CustomWorldMapPatch
    {
        [HarmonyPatch(typeof(MapScene), "Load")]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            int truthOccurance = -1;
            bool truthFlag = false;
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldstr && instruction.OperandIs("Main_map"))
                {
                    instruction.operand = "modded_main_map";
                }
                else if (instruction.opcode == OpCodes.Ldloca_S)
                {
                    truthOccurance++;
                    truthFlag = true;
                }
                else if (instruction.opcode == OpCodes.Stfld)
                {
                    truthFlag = false;
                }
                else if (instruction.opcode == OpCodes.Ldc_I4_0 && truthFlag && (truthOccurance == 1 || truthOccurance == 3))
                {
                    instruction.opcode = OpCodes.Ldc_I4_1;
                }
                yield return instruction;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MapScene), "GetMapBorders")]
        public static void CustomBorders(ref Vec2 minimumPosition, ref Vec2 maximumPosition, ref float maximumHeight)
        {
            minimumPosition = new Vec2(0f, 0f);
            maximumPosition = new Vec2(1100f, 1100f);
            maximumHeight = 350f;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(MobileParty), "RecoverPositionsForNavMeshUpdate")]
        public static bool WorldMapNavMeshDebugPatch(ref MobileParty __instance)
        {
            if (Settlement.All.Count > 0 && (!__instance.Position2D.IsNonZero() || !PartyBase.IsPositionOkForTraveling(__instance.Position2D)))
            {
                //teleport party to a valid navmesh position.
                __instance.Position2D = Settlement.All[0].GatePosition;
            }
            return true;
        }
    }
}