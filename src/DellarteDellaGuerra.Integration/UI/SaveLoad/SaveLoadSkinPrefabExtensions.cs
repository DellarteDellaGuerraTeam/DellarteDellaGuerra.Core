using System.Collections.Generic;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;
using Attribute = Bannerlord.UIExtenderEx.Prefabs2.PrefabExtensionSetAttributePatch.Attribute;

namespace DellarteDellaGuerra.Integration.UI.SaveLoad;

// Keep the shared character-creation panel intact and replace it only here.
[PrefabExtension("SaveLoadScreen", @"descendant::Widget[@Sprite='General\CharacterCreation\character_creation_panel']")]
internal sealed class SaveLoadPanelSkin : PrefabExtensionSetAttributePatch
{
    public override List<Attribute> Attributes =>
        new() { new Attribute("Sprite", @"DADG\SaveLoad\panel_no_frame") };
}

// Move only the dynamically rendered banner, not the row or its hit target.
[PrefabExtension("SavedGameTuple", "descendant::MaskedTextureWidget[@DataSource='{ClanBanner}']")]
internal sealed class SaveLoadBannerInset : PrefabExtensionSetAttributePatch
{
    public override List<Attribute> Attributes => new() { new Attribute("MarginTop", "6") };
}
