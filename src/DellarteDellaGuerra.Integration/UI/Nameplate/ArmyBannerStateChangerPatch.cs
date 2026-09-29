using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;

namespace DellarteDellaGuerra.Integration.UI.Nameplate;

[PrefabExtension("PartyNameplateItem", "descendant::MaskedTextureWidget[@Id='PartyBannerWidget']/Children")]
internal sealed class ArmyBannerStateChangerPatch : PrefabExtensionInsertPatch
{
    public override InsertType Type => InsertType.Child;

    [PrefabExtensionText]
    public string StateChanger =>
        """
        <BoolStateChangerWidget DataSource="{..}" TargetWidget="..\." BooleanCheck="@IsArmy" TrueState="Army" FalseState="Default" />
        """;
}
