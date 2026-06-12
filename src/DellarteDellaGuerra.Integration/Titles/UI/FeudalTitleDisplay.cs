using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;

namespace DellarteDellaGuerra.Integration.Titles.UI
{
    /// <summary>
    /// Shared formatting for contested titles: an " (occupied by X)" suffix appended to a
    /// title or holder line when the seat is held de facto by another clan.
    /// </summary>
    internal static class FeudalTitleDisplay
    {
        public static string OccupiedSuffix(string? occupantClanId)
        {
            if (occupantClanId is null) return string.Empty;

            Clan? occupant = Campaign.Current?.CampaignObjectManager.Find<Clan>(occupantClanId);
            TextObject suffix = new TextObject("{=dadg_title_occupied} (occupied by {OCCUPANT})");
            suffix.SetTextVariable("OCCUPANT", occupant?.Name?.ToString() ?? occupantClanId);
            return suffix.ToString();
        }
    }
}
