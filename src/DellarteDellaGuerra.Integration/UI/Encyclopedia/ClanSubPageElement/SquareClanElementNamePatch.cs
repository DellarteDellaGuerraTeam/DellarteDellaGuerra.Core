using System.Collections.Generic;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;
using Attribute = Bannerlord.UIExtenderEx.Prefabs2.PrefabExtensionSetAttributePatch.Attribute;

namespace DellarteDellaGuerra.Integration.UI.Encyclopedia.ClanSubPageElement;

[PrefabExtension("EncyclopediaClanSubPageElement", "descendant::TextWidget[@Text='@NameText']")]
internal sealed class SquareClanElementNamePatch : PrefabExtensionSetAttributePatch
{
    public override List<Attribute> Attributes => new()
    {
        new Attribute("WidthSizePolicy", "Fixed"),
        new Attribute("SuggestedWidth", "!Encyclopedia.SubPage.Element.Width"),
        new Attribute("HorizontalAlignment", "Center")
    };
}
