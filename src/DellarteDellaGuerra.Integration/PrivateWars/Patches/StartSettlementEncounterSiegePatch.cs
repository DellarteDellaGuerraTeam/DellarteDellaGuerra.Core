using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Titles.Api;
using Harmony.DependencyInjection.Patches;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace DellarteDellaGuerra.Integration.PrivateWars.Patches
{
    // Make a DADG-driven same-kingdom siege/raid actually resolve.
    //
    // EncounterManager.StartSettlementEncounter gates its two AI hostile branches — the raid branch and
    // the assault (siege -> battle) transition — on
    // FactionManager.IsAtWarAgainstFaction(attackerParty.MapFaction, settlement.MapFaction). For a private
    // war both reduce to the same kingdom, so the check short-circuits to false (design §2) and the
    // assault branch never runs: the besieger camps forever and the siege never becomes a battle
    // (design §4.2, "Siege actually resolves" — load-bearing for real sieges).
    //
    // The faction-grain check has thrown away the clan grain we need, so a postfix/prefix on the gate
    // alone can't recover it. We transpile the two call sites to keep the party and settlement on the
    // stack and route through a clan-aware gate that also passes for a registered private-war pair. Every
    // other branch of the method is left exactly as stock; when no private war is active the gate returns
    // the vanilla answer.
    public class StartSettlementEncounterSiegePatch : IPatch
    {
        private static ILogger _logger;

        public StartSettlementEncounterSiegePatch(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<StartSettlementEncounterSiegePatch>();
        }

        public MethodInfo? TargetMethod =>
            AccessTools.Method(typeof(EncounterManager), nameof(EncounterManager.StartSettlementEncounter));

        public MethodInfo? PatchMethod =>
            AccessTools.Method(typeof(StartSettlementEncounterSiegePatch), nameof(Transpiler));

        public PatchType PatchType => PatchType.Transpiler;

        private static readonly MethodInfo IsAtWarAgainstFaction =
            AccessTools.Method(typeof(FactionManager), nameof(FactionManager.IsAtWarAgainstFaction));

        private static readonly MethodInfo Gate =
            AccessTools.Method(typeof(StartSettlementEncounterSiegePatch), nameof(IsAtWarOrPrivateWar));

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = instructions.ToList();
            int replaced = 0;

            for (int i = 3; i < code.Count; i++)
            {
                if (!code[i].Calls(IsAtWarAgainstFaction)) continue;
                // Only flip the calls whose arguments are `attackerParty.MapFaction` and
                // `settlement.MapFaction` (the two AI hostile-branch gates). Any other shaped call is
                // left untouched.
                if (!IsMapFactionGetter(code[i - 1]) || !IsMapFactionGetter(code[i - 3])) continue;

                // Stack at the call: [..., attackerParty, <get_MapFaction>, settlement, <get_MapFaction>].
                // Nop the two MapFaction getters so the party and settlement objects themselves stay on the
                // stack, then retarget the call to our gate(MobileParty, Settlement).
                code[i - 3].opcode = OpCodes.Nop;
                code[i - 3].operand = null;
                code[i - 1].opcode = OpCodes.Nop;
                code[i - 1].operand = null;
                code[i].operand = Gate;
                replaced++;
            }

            if (replaced == 0)
                _logger.Error(
                    $"{nameof(StartSettlementEncounterSiegePatch)} found no IsAtWarAgainstFaction gate to patch; " +
                    "private-war sieges will not assault on this game version.");

            return code;
        }

        private static bool IsMapFactionGetter(CodeInstruction instruction) =>
            instruction.opcode == OpCodes.Callvirt
            && instruction.operand is MethodInfo method
            && method.Name == "get_MapFaction";

        // The stock answer, OR a registered private war between the besieger's clan and the settlement
        // owner. PrivateWarPatchHelper.AreEnemies null-guards the locator, so this is identical to vanilla
        // whenever private wars are inactive.
        private static bool IsAtWarOrPrivateWar(MobileParty attackerParty, Settlement settlement)
        {
            if (FactionManager.IsAtWarAgainstFaction(attackerParty?.MapFaction, settlement?.MapFaction))
                return true;
            return PrivateWarPatchHelper.AreEnemies(attackerParty?.ActualClan, settlement?.OwnerClan);
        }
    }
}
