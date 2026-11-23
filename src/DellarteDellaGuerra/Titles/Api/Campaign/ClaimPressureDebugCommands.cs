using System.Collections.Generic;
using TaleWorlds.Library;
using BannerlordCampaign = TaleWorlds.CampaignSystem.Campaign;

namespace DellarteDellaGuerra.Titles.Api.Campaign
{
    /// <summary>
    ///  <para>Usage: <c>campaign.press_claims &lt;claimantClanId&gt;</c></para>
    ///  Runs the real claim-pressure gates for one house today instead of waiting out its 30-day cooldown.
    /// </summary>
    public static class ClaimPressureDebugCommands
    {
        [CommandLineFunctionality.CommandLineArgumentFunction("press_claims", "campaign")]
        public static string PressClaims(List<string> args)
        {
            if (args.Count < 1) return "Usage: campaign.press_claims <claimantClanId>";

            var behavior = BannerlordCampaign.Current?.GetCampaignBehavior<ClaimPressureCampaignBehavior>();
            if (behavior is null) return "Load a campaign first.";

            var trace = behavior.PressClaimsNow(args[0]);
            return trace.Count == 0 ? $"{args[0]} holds no claim on a held title." : string.Join("\n", trace);
        }
    }
}
