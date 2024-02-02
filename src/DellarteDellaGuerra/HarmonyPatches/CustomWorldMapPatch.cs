﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Xml;
using HarmonyLib;
using SandBox;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace DellarteDellaGuerra.HarmonyPatches
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
        public static void CustomBorders(MapScene __instance, ref Vec2 minimumPosition, ref Vec2 maximumPosition, ref float maximumHeight)
        {
            minimumPosition = new Vec2(000, 000);
            maximumPosition = new Vec2(1100, 1100);
            maximumHeight = 350;
        }
        
        [HarmonyPrefix]
        [HarmonyPatch(typeof(MobileParty), "RecoverPositionsForNavMeshUpdate")]
        public static bool WorldMapNavMeshDebugPatch(ref MobileParty __instance)
        {
            if (!__instance.Position2D.IsNonZero() || !PartyBase.IsPositionOkForTraveling(__instance.Position2D))
            {
                //teleport party to a valid navmesh position.
                __instance.Position2D = Settlement.All.First().GatePosition;
            }
            return true;
        }
    }
}