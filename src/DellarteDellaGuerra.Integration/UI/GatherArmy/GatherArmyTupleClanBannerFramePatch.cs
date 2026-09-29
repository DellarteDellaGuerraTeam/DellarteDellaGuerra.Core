using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;

namespace DellarteDellaGuerra.Integration.UI.GatherArmy;

// Sibling of the banner button (not its child) so it follows the tuple's state, like the character frame baked
// into the tuple sprite. Height is Banner.Height.Scaled minus the empty strip at the bottom of the banner texture.
[PrefabExtension("GatherArmyTuple", "descendant::ButtonWidget[Children/MaskedTextureWidget[@DataSource='{ClanBanner}']]")]
internal sealed class GatherArmyTupleClanBannerFramePatch : PrefabExtensionInsertPatch
{
    public override InsertType Type => InsertType.Append;

    [PrefabExtensionText]
    public string Frame =>
        """
        <BrushWidget DoNotAcceptEvents="true" WidthSizePolicy="Fixed" HeightSizePolicy="Fixed" SuggestedWidth="!Banner.Width.Scaled" SuggestedHeight="66" HorizontalAlignment="Center" VerticalAlignment="Center" Brush="DADG.ArmyManagement.BannerFrame" />
        """;
}
