using System.Collections.Generic;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;
using Attribute = Bannerlord.UIExtenderEx.Prefabs2.PrefabExtensionSetAttributePatch.Attribute;

namespace DellarteDellaGuerra.Integration.UI.Encyclopedia.ClanSubPageElement;

[PrefabExtension("EncyclopediaClanSubPageElement", "descendant::Widget[@IsVisible='@IsDestroyed']/Children/MaskedTextureWidget[@Id='ElementImage']")]
internal sealed class SquareDestroyedClanElementBannerPatch : PrefabExtensionSetAttributePatch
{
    public override List<Attribute> Attributes => new()
    {
        new Attribute("Brush.SaturationFactor", "-100")
    };
}
