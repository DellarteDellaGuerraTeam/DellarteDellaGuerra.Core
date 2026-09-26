using System.Collections.Generic;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;
using Attribute = Bannerlord.UIExtenderEx.Prefabs2.PrefabExtensionSetAttributePatch.Attribute;

namespace DellarteDellaGuerra.Integration.Loading.UI;

[PrefabExtension("LoadingWindow", "descendant::*[@Id='LoadingAnimWidget']")]
internal sealed class LoadingWindowRosePrefabExtension : PrefabExtensionInsertPatch
{
    public override InsertType Type => InsertType.Prepend;

    [PrefabExtensionText]
    public string Widget =>
        """
        <BrushWidget
            Id="DADGRoseLoadingWidget"
            WidthSizePolicy="Fixed"
            HeightSizePolicy="Fixed"
            SuggestedWidth="128"
            SuggestedHeight="128"
            VerticalAlignment="Bottom"
            HorizontalAlignment="Right"
            MarginBottom="40"
            MarginRight="250"
            Brush="DADG.RoseLoading"
            DoNotUseCustomScale="true" />
        """;
}

[PrefabExtension("LoadingWindow", "descendant::*[@Id='LoadingAnimWidget']")]
internal sealed class LoadingWindowNativeAnimationHiderPrefabExtension : PrefabExtensionSetAttributePatch
{
    public override List<Attribute> Attributes => new()
    {
        new Attribute("IsVisible", "false")
    };
}
