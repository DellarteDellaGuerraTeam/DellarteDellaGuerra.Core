using System.Collections.Generic;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;
using Attribute = Bannerlord.UIExtenderEx.Prefabs2.PrefabExtensionSetAttributePatch.Attribute;

namespace DellarteDellaGuerra.Integration.UI.Encyclopedia.ClanSubPageElement;

// Hides the vanilla banner-shaped backgrounds; the square gold frame replaces them.
[PrefabExtension("EncyclopediaClanSubPageElement", "descendant::BrushWidget[@Brush='Encyclopedia.SubPage.Element']")]
[PrefabExtension("EncyclopediaClanSubPageElement", "descendant::BrushWidget[@Brush='Encyclopedia.SubPage.Element.Dead']")]
internal sealed class SquareClanElementBackgroundPatch : PrefabExtensionSetAttributePatch
{
    public override List<Attribute> Attributes => new()
    {
        new Attribute("IsVisible", "false")
    };
}
