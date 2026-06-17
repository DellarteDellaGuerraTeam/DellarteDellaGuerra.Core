using DellarteDellaGuerra.Titles.Api;
using TaleWorlds.CampaignSystem;

namespace DellarteDellaGuerra.Integration.PrivateWars.Patches
{
    // Shared, null-safe gateway the §4.2 hostility patches use to consult the private-war registry.
    // Two clans are enemies when they sit on opposite sides of an active private war. Every patch
    // falls back to vanilla (returns false here) when the locator is not initialised.
    internal static class PrivateWarPatchHelper
    {
        public static bool AreEnemies(Clan? a, Clan? b)
        {
            if (!FeudalServices.IsInitialised || FeudalServices.PrivateWarHostility is null) return false;
            if (a is null || b is null) return false;
            return FeudalServices.PrivateWarHostility.AreEnemies(a.StringId, b.StringId);
        }
    }
}
