using System.Collections.Generic;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;
using Attribute = Bannerlord.UIExtenderEx.Prefabs2.PrefabExtensionSetAttributePatch.Attribute;

namespace DellarteDellaGuerra.Integration.UI.GatherArmy;

// Lets the tuple's Disabled/Hovered/... state reach the banner frame; the banner button itself ignores it.
[PrefabExtension("GatherArmyTuple", "descendant::Widget[Children/ButtonWidget/Children/MaskedTextureWidget[@DataSource='{ClanBanner}']]")]
internal sealed class GatherArmyTupleClanBannerStatePatch : PrefabExtensionSetAttributePatch
{
    public override List<Attribute> Attributes => new()
    {
        new Attribute("UpdateChildrenStates", "true")
    };
}
