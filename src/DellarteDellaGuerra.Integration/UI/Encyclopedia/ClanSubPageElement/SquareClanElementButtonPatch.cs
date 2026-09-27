using System.Collections.Generic;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;
using Attribute = Bannerlord.UIExtenderEx.Prefabs2.PrefabExtensionSetAttributePatch.Attribute;

namespace DellarteDellaGuerra.Integration.UI.Encyclopedia.ClanSubPageElement;

[PrefabExtension("EncyclopediaClanSubPageElement", "/Prefab/Window/ButtonWidget")]
internal sealed class SquareClanElementButtonPatch : PrefabExtensionSetAttributePatch
{
    public override List<Attribute> Attributes => new()
    {
        new Attribute("SuggestedWidth", "!FrameSize"),
        new Attribute("SuggestedHeight", "!FrameSize"),
        new Attribute("Brush", "Encyclopedia.SubPage.Element.Settlement.ButtonBrush")
    };
}
