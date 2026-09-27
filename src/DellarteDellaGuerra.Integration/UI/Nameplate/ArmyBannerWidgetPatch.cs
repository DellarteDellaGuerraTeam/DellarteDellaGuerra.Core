using System.Collections.Generic;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;
using Attribute = Bannerlord.UIExtenderEx.Prefabs2.PrefabExtensionSetAttributePatch.Attribute;

namespace DellarteDellaGuerra.Integration.UI.Nameplate;

[PrefabExtension("PartyNameplateItem", "descendant::MaskedTextureWidget[@Id='PartyBannerWidget']")]
internal sealed class ArmyBannerWidgetPatch : PrefabExtensionSetAttributePatch
{
    public override List<Attribute> Attributes => new()
    {
        new Attribute("Brush", "DADG.Nameplate.FlatBanner.Party"),
        new Attribute("VisualDefinition", "DADG.PartyBanner")
    };
}
