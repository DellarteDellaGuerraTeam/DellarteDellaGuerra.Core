using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;

namespace DellarteDellaGuerra.Integration.UI.Loading;

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
            MarginBottom="75"
            MarginRight="75"
            Brush="DADG.RoseLoading"
            DoNotUseCustomScale="true" />
        """;
}
