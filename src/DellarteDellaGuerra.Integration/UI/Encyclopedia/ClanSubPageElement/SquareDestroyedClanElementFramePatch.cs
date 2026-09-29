using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;

namespace DellarteDellaGuerra.Integration.UI.Encyclopedia.ClanSubPageElement;

// Appended after the banner so the frame draws on top of it.
[PrefabExtension("EncyclopediaClanSubPageElement", "descendant::Widget[@IsVisible='@IsDestroyed']/Children/MaskedTextureWidget[@Id='ElementImage']")]
internal sealed class SquareDestroyedClanElementFramePatch : PrefabExtensionInsertPatch
{
    public override InsertType Type => InsertType.Append;

    [PrefabExtensionText]
    public string Frame =>
        """
        <BrushWidget DoNotAcceptEvents="true" WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent" MarginLeft="1" MarginRight="1" MarginTop="1" MarginBottom="1" Brush="TownManagement.GovernorPopup.GoldFrame" Brush.SaturationFactor="-100" />
        """;
}
