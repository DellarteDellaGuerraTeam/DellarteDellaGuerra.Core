using System.Collections.Generic;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;
using Attribute = Bannerlord.UIExtenderEx.Prefabs2.PrefabExtensionSetAttributePatch.Attribute;

namespace DellarteDellaGuerra.Integration.UI.Encyclopedia.ClanSubPageElement;

[PrefabExtension("EncyclopediaClanSubPageElement", "descendant::MaskedTextureWidget[@Id='ElementImage']")]
internal sealed class SquareClanElementBannerPatch : PrefabExtensionSetAttributePatch
{
    public override List<Attribute> Attributes => new()
    {
        new Attribute("WidthSizePolicy", "Fixed"),
        new Attribute("HeightSizePolicy", "Fixed"),
        new Attribute("SuggestedWidth", "!BannerSize"),
        new Attribute("SuggestedHeight", "!BannerSize"),
        new Attribute("VerticalAlignment", "Center"),
        new Attribute("MarginLeft", "0"),
        new Attribute("MarginRight", "0"),
        new Attribute("MarginTop", "0"),
        new Attribute("MarginBottom", "0")
    };
}
