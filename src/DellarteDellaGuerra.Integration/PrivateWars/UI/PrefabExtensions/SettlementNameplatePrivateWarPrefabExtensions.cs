using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;

namespace DellarteDellaGuerra.Integration.PrivateWars.UI.PrefabExtensions
{
    internal abstract class SettlementNameplatePrivateWarCapsuleOverlayPatch : PrefabExtensionInsertPatch
    {
        public override InsertType Type => InsertType.Child;

        public override int Index => 0;

        protected abstract string Sprite { get; }

        [PrefabExtensionText]
        public string Content => $"""
            <Widget Id="SettlementCapsuleTintOverlay" WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent" Sprite="{Sprite}" Color="@SettlementCapsuleColor" IsEnabled="false" DoNotAcceptEvents="true" DoNotPassEventsToChildren="true" />
            """;
    }

    [PrefabExtension("SettlementNameplateItemSmall", "descendant::ButtonWidget[@Id='SettlementNameplateCapsuleWidget']/Children")]
    internal sealed class SmallSettlementNameplatePrivateWarCapsuleOverlayPatch : SettlementNameplatePrivateWarCapsuleOverlayPatch
    {
        protected override string Sprite => "enemy_village_9";
    }

    [PrefabExtension("SettlementNameplateItemMedium", "descendant::ButtonWidget[@Id='SettlementNameplateCapsuleWidget']/Children")]
    internal sealed class MediumSettlementNameplatePrivateWarCapsuleOverlayPatch : SettlementNameplatePrivateWarCapsuleOverlayPatch
    {
        protected override string Sprite => "enemy_castle_9";
    }

    [PrefabExtension("SettlementNameplateItemLarge", "descendant::ButtonWidget[@Id='SettlementNameplateCapsuleWidget']/Children")]
    internal sealed class LargeSettlementNameplatePrivateWarCapsuleOverlayPatch : SettlementNameplatePrivateWarCapsuleOverlayPatch
    {
        protected override string Sprite => "enemy_town_9";
    }
}
