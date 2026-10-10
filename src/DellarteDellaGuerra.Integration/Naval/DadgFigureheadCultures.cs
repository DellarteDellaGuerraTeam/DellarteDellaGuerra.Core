using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.ObjectSystem;

namespace DellarteDellaGuerra.Integration.Naval;

internal static class DadgFigureheadCultures
{
    private const string LowlandScotsCultureId = "khuzait";

    // DefaultFigureheads is constructed inside InitializeDefaultCampaignObjects, before
    // SPCultures is loaded, so its hardcoded GetObject<CultureObject> lookups can only
    // resolve to presumed objects conjured by the item XML loaded moments earlier. Siren,
    // Turtle and Deer ask for "sturgia", which DADG does not define: they end up holding
    // either null or a presumed culture that UnregisterNonReadyObjects later drops, and
    // the port screen then throws on Culture.Name. Figurehead culture is hardcoded with
    // no XML path, so reattributing them has to happen here. Lowland Scots matches the
    // sturgia mapping in dadg_post_naval_ship_upgrade_pieces.xslt.
    internal static void ReattributeSturgianFigureheads()
    {
        var lowlandScots = MBObjectManager.Instance.GetObject<CultureObject>(LowlandScotsCultureId);
        if (lowlandScots == null) return;

        SetCulture(DefaultFigureheads.Siren, lowlandScots);
        SetCulture(DefaultFigureheads.Turtle, lowlandScots);
        SetCulture(DefaultFigureheads.Deer, lowlandScots);
    }

    // Figurehead.Culture has no setter and Initialize is the only way in. Every other
    // argument is readable, so the name, effect amount and increment type are preserved.
    private static void SetCulture(Figurehead figurehead, CultureObject culture)
    {
        figurehead.Initialize(figurehead.Name, figurehead.Description, figurehead.EffectAmount,
            culture, figurehead.EffectIncrementType);
    }
}
