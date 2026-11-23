using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.ObjectSystem;

namespace DellarteDellaGuerra.Integration.Naval;

internal static class OrphanedFigureheads
{
    // DefaultFigureheads hardcodes one culture per figurehead and has no XML path, so the three that
    // ask for "sturgia" cannot be fixed in data. That lookup also runs before SPCultures is loaded,
    // meaning it resolves to whatever presumed culture the item XML conjured, which
    // UnregisterNonReadyObjects later drops. Siren, Turtle and Deer are left pointing at a culture
    // that no longer exists, and the port screen dereferences it for their tooltip. They belong to no
    // DADG faction, so unregister them instead of adopting them into one.
    internal static void Remove()
    {
        foreach (var figurehead in MBObjectManager.Instance.GetObjectTypeList<Figurehead>()
                     .Where(IsOrphaned).ToList())
        {
            // A save made before this ran can still have one of them equipped, and the port screen
            // re-adds the equipped figurehead even once it is unregistered. The tooltip skips the
            // culture line when it is null, so clear it as well as unregistering the object.
            figurehead.Initialize(figurehead.Name, figurehead.Description, figurehead.EffectAmount,
                null, figurehead.EffectIncrementType);
            MBObjectManager.Instance.UnregisterObject(figurehead);
        }
    }

    // A culture DADG defines resolves back to the very same instance. A dropped one resolves to null.
    private static bool IsOrphaned(Figurehead figurehead)
    {
        return figurehead.Culture == null
               || MBObjectManager.Instance.GetObject<CultureObject>(figurehead.Culture.StringId) == null;
    }
}
