using System.Collections.Generic;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;
using Attribute = Bannerlord.UIExtenderEx.Prefabs2.PrefabExtensionSetAttributePatch.Attribute;

namespace DellarteDellaGuerra.Integration.UI.GatherArmy;

// Vanilla pins the banner to the top of the row. Centre it instead; the +4 offsets half of the empty strip at the
// bottom of the banner texture so the visible banner (and its frame) sit in the middle of the row.
[PrefabExtension("GatherArmyTuple", "descendant::ButtonWidget[Children/MaskedTextureWidget[@DataSource='{ClanBanner}']]")]
internal sealed class GatherArmyTupleClanBannerCenterPatch : PrefabExtensionSetAttributePatch
{
    public override List<Attribute> Attributes => new()
    {
        new Attribute("VerticalAlignment", "Center"),
        new Attribute("PositionYOffset", "4")
    };
}
