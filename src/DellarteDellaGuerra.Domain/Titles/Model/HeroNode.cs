using System.Collections.Generic;

namespace DellarteDellaGuerra.Domain.Titles.Model
{
    /// <summary>
    /// The genealogical facts about a hero that claim derivation needs, decoupled from the
    /// engine's hero type.
    /// </summary>
    public record HeroNode(
        string Id,
        bool IsFemale,
        bool IsAlive,
        string? ClanId,
        IReadOnlyList<string> ChildIds);
}
