using System.Linq;
using TaleWorlds.ModuleManager;
using TaleWorlds.ObjectSystem;

namespace DellarteDellaGuerra.Integration.Naval;

internal static class NavalXmlSelector
{
    private const string MapModuleId = "DellarteDellaGuerraMap";

    // Naval content that references NavalDLC objects. Loaded only with War Sails.
    private static readonly string[] NavalOnlyXmls =
    {
        "dadg_custom_mission_ships",
        "dadg_custom_ship_hulls",
        "characters/dadg_naval_characters",
        "dadg_naval_party_templates",
        "dadg_naval_items",
        "dadg_naval_siegeengines",
        "dadg_ship_upgrade_pieces",
        "dadg_ship_slots",
        "dadg_post_naval_ship_upgrade_pieces",
    };

    // Strips of ports, shipyards and naval culture references. Loaded only without War Sails.
    private static readonly string[] NoNavalXmls =
    {
        "culture/dadg_no_naval_cultures",
        "clans/dadg_no_naval_clans",
        "dadg_no_naval_settlements",
    };

    // Module.LoadSubModules reads every module's <Xmls> into XmlResource before loading any
    // submodule DLL, and nothing is merged until a game starts, so the list can still be edited here.
    internal static void Apply()
    {
        var dropped = ModuleHelper.IsModuleActive("NavalDLC") ? NoNavalXmls : NavalOnlyXmls;
        XmlResource.XmlInformationList.RemoveAll(xml =>
            xml.ModuleName == MapModuleId && dropped.Contains(xml.Name));
    }
}
