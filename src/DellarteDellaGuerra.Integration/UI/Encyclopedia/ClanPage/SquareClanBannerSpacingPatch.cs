using System.Collections.Generic;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;
using Attribute = Bannerlord.UIExtenderEx.Prefabs2.PrefabExtensionSetAttributePatch.Attribute;

namespace DellarteDellaGuerra.Integration.UI.Encyclopedia.ClanPage;

[PrefabExtension("EncyclopediaClanPage", "descendant::Widget[@IsVisible='@IsClanDestroyed' and Children/MaskedTextureWidget[@Id='ClanBannerVisual']]")]
[PrefabExtension("EncyclopediaClanPage", "descendant::Widget[@IsHidden='@IsClanDestroyed' and Children/MaskedTextureWidget[@Id='ClanBannerVisual']]")]
internal sealed class SquareClanBannerSpacingPatch : PrefabExtensionSetAttributePatch
{
    public override List<Attribute> Attributes => new()
    {
        new Attribute("MarginTop", "45")
    };
}
