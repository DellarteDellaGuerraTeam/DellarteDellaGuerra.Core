using System.Collections.Generic;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;
using Attribute = Bannerlord.UIExtenderEx.Prefabs2.PrefabExtensionSetAttributePatch.Attribute;

namespace DellarteDellaGuerra.Integration.UI.Encyclopedia.ClanPage;

[PrefabExtension("EncyclopediaClanPage", "descendant::Widget[@IsVisible='@IsClanDestroyed']/Children/MaskedTextureWidget[@Id='ClanBannerVisual']")]
[PrefabExtension("EncyclopediaClanPage", "descendant::Widget[@IsHidden='@IsClanDestroyed']/Children/MaskedTextureWidget[@Id='ClanBannerVisual']")]
internal sealed class SquareClanBannerPatch : PrefabExtensionSetAttributePatch
{
    public override List<Attribute> Attributes => new()
    {
        new Attribute("SuggestedWidth", "241"),
        new Attribute("SuggestedHeight", "241"),
        new Attribute("Brush", "Encyclopedia.Clan.SubElement.Banner")
    };
}
