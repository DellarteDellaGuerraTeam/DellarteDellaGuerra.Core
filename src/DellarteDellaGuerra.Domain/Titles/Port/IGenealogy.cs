using System.Collections.Generic;
using DellarteDellaGuerra.Domain.Titles.Model;

namespace DellarteDellaGuerra.Domain.Titles.Port
{
    /// <summary>
    /// Read-only access to the bloodlines claim derivation walks.
    /// </summary>
    public interface IGenealogy
    {
        HeroNode? GetHero(string heroId);

        /// <summary>The clan's current head.</summary>
        string? GetClanLeaderId(string clanId);

        /// <summary>
        /// The hero's current clan. Derived on every call, never cached: heroes change clans through
        /// marriage and (later) the cadet spinoff, and a stored clan id would go stale silently.
        /// </summary>
        string? GetClanOf(string heroId);

        /// <summary>
        /// The clan's dead members, treated as its former title holders. The campaign data
        /// records no per-hero title history, so every deceased member of the holding clan
        /// anchors that clan's principal dignity.
        /// </summary>
        IReadOnlyList<string> GetDeceasedClanMemberIds(string clanId);
    }
}
