using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using DellarteDellaGuerra.Domain.PrivateWars;
using DellarteDellaGuerra.PrivateWars.Api.Patches;
using Harmony.DependencyInjection.Patches;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace DellarteDellaGuerra.Integration.PrivateWars.Patches
{
    // Amend only the 1.4.6 strength-loop enemy predicate. All land/blockade selection, sea parity,
    // strength contexts, ratios and sally consequences remain in the vanilla method body.
    public class SallyOutStrengthPatch : IPatch
    {
        private static readonly PrivateWarInteractionPolicy InteractionPolicy = new();

        private static readonly MethodInfo MobilePartyMapFactionGetter =
            AccessTools.PropertyGetter(typeof(MobileParty), nameof(MobileParty.MapFaction));

        private static readonly MethodInfo SettlementPartyGetter =
            AccessTools.PropertyGetter(typeof(Settlement), nameof(Settlement.Party));

        private static readonly MethodInfo PartyMapFactionGetter =
            AccessTools.PropertyGetter(typeof(PartyBase), nameof(PartyBase.MapFaction));

        private static readonly MethodInfo VanillaEnemyPredicate =
            AccessTools.Method(typeof(IFaction), nameof(IFaction.IsAtWarWith), new[] { typeof(IFaction) });

        private static readonly MethodInfo SallyOutEnemyPredicate =
            AccessTools.Method(typeof(SallyOutStrengthPatch), nameof(IsEnemyForSallyOutStrength));

        public MethodInfo TargetMethod => PrivateWarHarmonyPatchTargets.SallyOutCheck();

        public MethodInfo? PatchMethod =>
            AccessTools.Method(typeof(SallyOutStrengthPatch), nameof(Transpiler));

        public PatchType PatchType => PatchType.Transpiler;

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = instructions.ToList();
            var replaced = 0;

            for (var i = 4; i < code.Count; i++)
            {
                if (!code[i].Calls(VanillaEnemyPredicate) ||
                    !code[i - 4].Calls(MobilePartyMapFactionGetter) ||
                    !code[i - 2].Calls(SettlementPartyGetter) ||
                    !code[i - 1].Calls(PartyMapFactionGetter))
                    continue;

                // Preserve the two object loads but remove the three getters, changing the stack from
                // (IFaction, IFaction) to the evaluated (MobileParty, Settlement) pair.
                code[i - 4].opcode = OpCodes.Nop;
                code[i - 4].operand = null;
                code[i - 2].opcode = OpCodes.Nop;
                code[i - 2].operand = null;
                code[i - 1].opcode = OpCodes.Nop;
                code[i - 1].operand = null;
                code[i].operand = SallyOutEnemyPredicate;
                replaced++;
            }

            if (replaced != 1)
                throw new InvalidOperationException(
                    $"{nameof(SallyOutStrengthPatch)} expected exactly one sally-out strength predicate, found {replaced}.");

            return code;
        }

        private static bool IsEnemyForSallyOutStrength(MobileParty mobileParty, Settlement settlement)
        {
            var vanillaEnemies = mobileParty.MapFaction.IsAtWarWith(settlement.Party.MapFaction);
            var arePrivateEnemies = PrivateWarPatchHelper.AreEnemies(
                mobileParty.ActualClan,
                settlement.OwnerClan);

            return InteractionPolicy.ResolveSallyOutStrengthEnemy(vanillaEnemies, arePrivateEnemies);
        }
    }
}
