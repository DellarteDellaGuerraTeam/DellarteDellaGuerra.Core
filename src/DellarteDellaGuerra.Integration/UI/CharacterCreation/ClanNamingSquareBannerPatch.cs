using System.Collections.Generic;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;
using Attribute = Bannerlord.UIExtenderEx.Prefabs2.PrefabExtensionSetAttributePatch.Attribute;

namespace DellarteDellaGuerra.Integration.UI.CharacterCreation;

// Keep the native dynamic banner provider; replace only the narrow preview's mask and dimensions.
[PrefabExtension("CharacterCreationClanNamingStage", "descendant::MaskedTextureWidget[@DataSource='{ClanBanner}'][@Brush='ClanNaming.Stage.Banner']")]
internal sealed class ClanNamingSquareBannerPatch : PrefabExtensionSetAttributePatch
{
    public override List<Attribute> Attributes => new()
    {
        new Attribute("SuggestedWidth", "440"),
        new Attribute("SuggestedHeight", "440"),
        new Attribute("Brush", "DADG.ClanNaming.SquareBanner")
    };
}
