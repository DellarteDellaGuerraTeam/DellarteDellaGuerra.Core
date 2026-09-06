using System.Collections.Generic;

namespace DellarteDellaGuerra.Domain.Titles.Model
{
    /// <summary>
    /// The genealogical facts about a hero that claim derivation and succession need,
    /// decoupled from the engine's hero type. FatherId is what makes siblings reachable —
    /// a hero's brothers are his father's other children — and Age orders a sibling set by
    /// seniority. Age is a float rather than a campaign date to keep the domain free of
    /// CampaignTime.
    /// </summary>
    public record HeroNode(
        string Id,
        bool IsFemale,
        bool IsAlive,
        string? ClanId,
        IReadOnlyList<string> ChildIds,
        string? FatherId = null,
        float Age = 0f);
}
