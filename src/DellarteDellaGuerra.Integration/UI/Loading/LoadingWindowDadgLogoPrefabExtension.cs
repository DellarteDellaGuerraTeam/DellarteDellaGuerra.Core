using System.Collections.Generic;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;
using Attribute = Bannerlord.UIExtenderEx.Prefabs2.PrefabExtensionSetAttributePatch.Attribute;

namespace DellarteDellaGuerra.Integration.UI.Loading;

[PrefabExtension("LoadingWindow", "descendant::*[@Brush='InitialMenu.Logo']")]
internal sealed class LoadingWindowDadgLogoPrefabExtension : PrefabExtensionSetAttributePatch
{
    public override List<Attribute> Attributes => new()
    {
        new Attribute("IsHidden", "false")
    };
}

[PrefabExtension("LoadingWindow", "descendant::*[@Brush='InitialMenu.Logo.WarSails']")]
internal sealed class LoadingWindowNavalLogoHiderPrefabExtension : PrefabExtensionSetAttributePatch
{
    public override List<Attribute> Attributes => new()
    {
        new Attribute("IsVisible", "false")
    };
}
