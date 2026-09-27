using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;

namespace DellarteDellaGuerra.Integration.UI.Nameplate;

[PrefabExtension("PartyNameplateItem", "/Prefab/Constants/Constant[@Name='Party.Banner.Height.Scaled']")]
internal sealed class ArmyBannerSizeConstantsPatch : PrefabExtensionInsertPatch
{
    public override InsertType Type => InsertType.Append;

    [PrefabExtensionText(true)]
    public string Constants =>
        """
        <Constants>
            <Constant Name="Army.Banner.Width" SpriteName="DADG\Nameplates\army_banner_square" SpriteValueType="Width" />
            <Constant Name="Army.Banner.Height" SpriteName="DADG\Nameplates\army_banner_square" SpriteValueType="Height" />
            <Constant Name="Army.Banner.Width.Scaled" MultiplyResult="0.20" Value="!Army.Banner.Width" />
            <Constant Name="Army.Banner.Height.Scaled" MultiplyResult="0.20" Value="!Army.Banner.Height" />
        </Constants>
        """;
}
