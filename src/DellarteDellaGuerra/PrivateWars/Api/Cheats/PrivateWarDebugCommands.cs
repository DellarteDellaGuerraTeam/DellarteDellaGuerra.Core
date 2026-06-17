using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.PrivateWars;
using DellarteDellaGuerra.Titles.Api;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace DellarteDellaGuerra.PrivateWars.Api.Cheats
{
    /// <summary>
    /// TEMPORARY debug console command for playtesting the private-war battle path. There is no
    /// in-game declaration path yet (the real one is a later phase), so this registers an active war
    /// directly via <see cref="DeclarePrivateWarUseCase"/> from two same-kingdom clan ids. Remove once
    /// a real declaration trigger exists.
    /// <para>Usage: <c>campaign.declare_private_war &lt;attackerClanId&gt; &lt;defenderClanId&gt; [goalSettlementId]</c></para>
    /// </summary>
    public static class PrivateWarDebugCommands
    {
        [CommandLineFunctionality.CommandLineArgumentFunction("declare_private_war", "campaign")]
        public static string DeclarePrivateWar(List<string> args)
        {
            if (args.Count < 2)
                return "Usage: campaign.declare_private_war <attackerClanId> <defenderClanId> [goalSettlementId]";

            if (!FeudalServices.IsInitialised || FeudalServices.PrivateWars is null)
                return "FeudalServices is not initialised - load a campaign first.";

            var attacker = FindClan(args[0]);
            if (attacker is null) return $"No clan with id '{args[0]}'.";

            var defender = FindClan(args[1]);
            if (defender is null) return $"No clan with id '{args[1]}'.";

            if (attacker == defender) return "Attacker and defender must differ.";

            // The whole point of a private war: same MapFaction (kingdom). Warn but allow.
            string note = attacker.MapFaction == defender.MapFaction
                ? ""
                : " (WARNING: clans are NOT in the same MapFaction - this is an ordinary war, not a private one)";

            Settlement? goal = args.Count >= 3
                ? Settlement.Find(args[2])
                : defender.Settlements.FirstOrDefault(s => s.IsFortification);
            if (goal is null)
                return args.Count >= 3
                    ? $"No settlement with id '{args[2]}'."
                    : $"Defender clan '{defender.StringId}' holds no fortification to use as a goal; pass goalSettlementId explicitly.";

            var fiefSnapshot = defender.Settlements
                .Where(s => s.OwnerClan != null)
                .ToDictionary(s => s.StringId, s => s.OwnerClan.StringId);

            var useCase = new DeclarePrivateWarUseCase(FeudalServices.PrivateWars);
            var war = useCase.Execute(
                attacker.StringId,
                defender.StringId,
                new DebugCasusBelli(),
                goal.StringId,
                fiefSnapshot,
                (float)CampaignTime.Now.ToDays);

            if (war is null)
                return "Declaration rejected (landless defender, or an identical active war already exists).";

            return $"Private war declared: {attacker.StringId} vs {defender.StringId}, goal '{goal.StringId}'{note}. Id={war.Id}";
        }

        private static Clan? FindClan(string stringId)
            => TaleWorlds.CampaignSystem.Campaign.Current?.Clans.FirstOrDefault(c => c.StringId == stringId);

        private sealed class DebugCasusBelli : ICasusBelli
        {
            public string Type => "debug";
            public string TitleId => "debug";
        }
    }
}
