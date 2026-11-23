using System.Collections.Generic;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;
using Attribute = Bannerlord.UIExtenderEx.Prefabs2.PrefabExtensionSetAttributePatch.Attribute;

namespace DellarteDellaGuerra.Integration.UI.CharacterCreation;

[PrefabExtension("CharacterCreationOptionsStage", "descendant::Standard.DropdownWithHorizontalControl[@Id='SelectionOption']")]
internal sealed class DifficultySelectorWidthPatch : PrefabExtensionSetAttributePatch
{
    public override List<Attribute> Attributes => new()
    {
        new Attribute("Parameter.IsCustomWidth", "true"),
        new Attribute("Parameter.CustomWidth", "440")
    };
}
