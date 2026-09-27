using System.Collections.Generic;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;
using Attribute = Bannerlord.UIExtenderEx.Prefabs2.PrefabExtensionSetAttributePatch.Attribute;

namespace DellarteDellaGuerra.Integration.UI.Loading;

[PrefabExtension("LoadingWindow", "descendant::*[@Id='LoadingAnimWidget']")]
internal sealed class LoadingWindowNativeAnimationHiderPrefabExtension : PrefabExtensionSetAttributePatch
{
    public override List<Attribute> Attributes => new()
    {
        new Attribute("IsVisible", "false")
    };
}
