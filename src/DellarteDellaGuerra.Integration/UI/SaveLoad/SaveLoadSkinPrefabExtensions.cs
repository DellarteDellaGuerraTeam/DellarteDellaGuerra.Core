using System.Collections.Generic;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;
using Attribute = Bannerlord.UIExtenderEx.Prefabs2.PrefabExtensionSetAttributePatch.Attribute;

namespace DellarteDellaGuerra.Integration.UI.SaveLoad;

// Move only the dynamically rendered banner, not the row or its hit target.
[PrefabExtension("SavedGameTuple", "descendant::MaskedTextureWidget[@DataSource='{ClanBanner}']")]
internal sealed class SaveLoadBannerInset : PrefabExtensionSetAttributePatch
{
    public override List<Attribute> Attributes => new() { new Attribute("MarginTop", "6") };
}
