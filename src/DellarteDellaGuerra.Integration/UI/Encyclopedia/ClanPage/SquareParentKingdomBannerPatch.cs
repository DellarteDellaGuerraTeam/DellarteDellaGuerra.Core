using System.Collections.Generic;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;
using Attribute = Bannerlord.UIExtenderEx.Prefabs2.PrefabExtensionSetAttributePatch.Attribute;

namespace DellarteDellaGuerra.Integration.UI.Encyclopedia.ClanPage;

[PrefabExtension("EncyclopediaClanPage", "descendant::Widget[@Id='ParentKingdomVisual']")]
internal sealed class SquareParentKingdomBannerPatch : PrefabExtensionSetAttributePatch
{
    public override List<Attribute> Attributes => new()
    {
        new Attribute("SuggestedWidth", "89")
    };
}
