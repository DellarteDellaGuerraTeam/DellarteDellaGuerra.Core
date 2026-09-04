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

        /// <summary>The clan's current head, treated as the living holder of its titles.</summary>
        string? GetClanLeaderId(string clanId);

        /// <summary>
        /// The clan's dead members, treated as its former title holders. The campaign data
        /// records no per-hero title history, so every deceased member of the holding clan
        /// anchors that clan's principal dignity.
        /// </summary>
        IReadOnlyList<string> GetDeceasedClanMemberIds(string clanId);
    }
}
