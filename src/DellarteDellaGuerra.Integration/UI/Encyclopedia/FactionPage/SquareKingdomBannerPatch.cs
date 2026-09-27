using System.Collections.Generic;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;
using Attribute = Bannerlord.UIExtenderEx.Prefabs2.PrefabExtensionSetAttributePatch.Attribute;

namespace DellarteDellaGuerra.Integration.UI.Encyclopedia.FactionPage;

[PrefabExtension("EncyclopediaFactionPage", "descendant::MaskedTextureWidget[@Brush='Encyclopedia.Faction.Banner']")]
internal sealed class SquareKingdomBannerPatch : PrefabExtensionSetAttributePatch
{
    public override List<Attribute> Attributes => new()
    {
        new Attribute("SuggestedWidth", "243"),
        new Attribute("SuggestedHeight", "243"),
        new Attribute("MarginTop", "12"),
        new Attribute("Brush", "Encyclopedia.Clan.SubElement.Banner")
    };
}
