using System.Collections.Generic;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;
using Attribute = Bannerlord.UIExtenderEx.Prefabs2.PrefabExtensionSetAttributePatch.Attribute;

namespace DellarteDellaGuerra.Integration.UI.SaveLoad;

// Bind visual availability on the text only; the native row still owns selection,
// load commands, and the unavailable-save hint. The corruption brush is unchanged.
[PrefabExtension("SavedGameTuple", "descendant::TextWidget[@Id='SaveNameTextWidget']")]
internal sealed class SaveLoadNameAvailability : PrefabExtensionSetAttributePatch
{
    public override List<Attribute> Attributes => new() { new Attribute("IsDisabled", "@IsDisabled") };
}

[PrefabExtension("SavedGameTuple", "descendant::TextWidget[@Text='@RealTimeText1']")]
internal sealed class SaveLoadDateAvailability : PrefabExtensionSetAttributePatch
{
    public override List<Attribute> Attributes => new() { new Attribute("IsDisabled", "@IsDisabled") };
}

[PrefabExtension("SavedGameTuple", "descendant::TextWidget[@Text='@RealTimeText2']")]
internal sealed class SaveLoadTimeAvailability : PrefabExtensionSetAttributePatch
{
    public override List<Attribute> Attributes => new() { new Attribute("IsDisabled", "@IsDisabled") };
}

// Prevent row hover/selection from replacing the dates' own Disabled state.
// The name's native BoolBrushChanger already does not propagate child states.
[PrefabExtension("SavedGameTuple", "descendant::ListPanel[Children/TextWidget[@Text='@RealTimeText1']]")]
internal sealed class SaveLoadDateOwnStates : PrefabExtensionSetAttributePatch
{
    public override List<Attribute> Attributes => new() { new Attribute("UpdateChildrenStates", "false") };
}
