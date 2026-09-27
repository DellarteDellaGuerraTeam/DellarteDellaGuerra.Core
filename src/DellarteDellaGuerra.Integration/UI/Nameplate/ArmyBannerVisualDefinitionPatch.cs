using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;

namespace DellarteDellaGuerra.Integration.UI.Nameplate;

[PrefabExtension("PartyNameplateItem", "/Prefab/Window")]
internal sealed class ArmyBannerVisualDefinitionPatch : PrefabExtensionInsertPatch
{
    public override InsertType Type => InsertType.Prepend;

    [PrefabExtensionText]
    public string VisualDefinition =>
        """
        <VisualDefinitions>
            <VisualDefinition Name="DADG.PartyBanner" TransitionDuration="0">
                <VisualState State="Default" SuggestedWidth="!Party.Banner.Width.Scaled" SuggestedHeight="!Party.Banner.Height.Scaled" />
                <VisualState State="Army" SuggestedWidth="!Army.Banner.Width.Scaled" SuggestedHeight="!Army.Banner.Height.Scaled" />
            </VisualDefinition>
        </VisualDefinitions>
        """;
}
