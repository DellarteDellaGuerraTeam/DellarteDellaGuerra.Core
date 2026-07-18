using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DellarteDellaGuerra.Domain.PrivateWars;
using DellarteDellaGuerra.PrivateWars.Api.Patches;
using Harmony.DependencyInjection.Patches;
using HarmonyLib;
using TaleWorlds.CampaignSystem;

namespace DellarteDellaGuerra.Integration.PrivateWars.Patches
{
    // Change only CheckCaptivityChange's no-more-enemies war predicate. The rest of the 1.4.6 method,
    // including ransom offers, timed escape and naval captor state, remains vanilla.
    public class PlayerCaptivityRetentionPatch : IPatch
    {
        private static readonly PrivateWarInteractionPolicy InteractionPolicy = new();

        private static readonly MethodInfo VanillaWarPredicate =
            AccessTools.Method(typeof(FactionManager), nameof(FactionManager.IsAtWarAgainstFaction),
                new[] { typeof(IFaction), typeof(IFaction) });

        private static readonly MethodInfo CaptivityWarPredicate =
            AccessTools.Method(typeof(PlayerCaptivityRetentionPatch), nameof(IsAtWarOrPrivateWar));

        public MethodInfo TargetMethod => PrivateWarHarmonyPatchTargets.PlayerCaptivityCheck();

        public MethodInfo? PatchMethod =>
            AccessTools.Method(typeof(PlayerCaptivityRetentionPatch), nameof(Transpiler));

        public PatchType PatchType => PatchType.Transpiler;

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = instructions.ToList();
            var replaced = 0;

            foreach (var instruction in code)
            {
                if (!instruction.Calls(VanillaWarPredicate)) continue;
                instruction.operand = CaptivityWarPredicate;
                replaced++;
            }

            if (replaced != 1)
                throw new InvalidOperationException(
                    $"{nameof(PlayerCaptivityRetentionPatch)} expected exactly one no-more-enemies war predicate, found {replaced}.");

            return code;
        }

        private static bool IsAtWarOrPrivateWar(IFaction captorFaction, IFaction playerFaction)
        {
            var vanillaAtWar = FactionManager.IsAtWarAgainstFaction(captorFaction, playerFaction);

            var captorParty = PlayerCaptivity.CaptorParty;
            var captorClan = captorParty?.MobileParty?.ActualClan ?? captorParty?.Settlement?.OwnerClan;
            var arePrivateEnemies = PrivateWarPatchHelper.AreEnemies(Hero.MainHero.Clan, captorClan);

            return InteractionPolicy.ResolveCaptivityWarPredicate(vanillaAtWar, arePrivateEnemies);
        }
    }
}
