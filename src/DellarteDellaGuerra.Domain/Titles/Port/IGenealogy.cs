using DellarteDellaGuerra.Domain.Titles.Model;

namespace DellarteDellaGuerra.Domain.Titles.Port
{
    /// <summary>
    /// Read-only access to the bloodlines claim derivation and succession walk.
    /// </summary>
    public interface IGenealogy
    {
        HeroNode? GetHero(string heroId);

        /// <summary>
        /// The clan's current head. A head is not a title holder — since titles became hero-held
        /// this is only the last-resort backstop for a succession that finds no heir by blood.
        /// </summary>
        string? GetClanLeaderId(string clanId);

        /// <summary>
        /// The hero's current clan. Derived on every call, never cached: heroes change clans through
        /// marriage and (later) the cadet spinoff, and a stored clan id would go stale silently.
        /// </summary>
        string? GetClanOf(string heroId);
    }
}
