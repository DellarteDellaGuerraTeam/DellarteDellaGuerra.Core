using System.Collections.Generic;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;
using Attribute = Bannerlord.UIExtenderEx.Prefabs2.PrefabExtensionSetAttributePatch.Attribute;

namespace DellarteDellaGuerra.Integration.UI.Recruitment;

// Keep native geometry, constants, bindings and controls. The only geometry change
// is the approved five-pixel alignment of the right-hand panel with the left.
[PrefabExtension("RecruitmentPopup", "descendant::*[@Brush='Recruitment.Frame']")]
internal sealed class RecruitmentFrameSkin : PrefabExtensionSetAttributePatch
{
    public override List<Attribute> Attributes => new() { new Attribute("Brush", "DADG.Recruitment.Frame") };
}
[PrefabExtension("RecruitmentPopup", "descendant::*[@Sprite='SPGeneral\\SPRecruitment\\left_dent']")]
internal sealed class RecruitmentLeftSkin : PrefabExtensionSetAttributePatch
{
    public override List<Attribute> Attributes => new()
    {
        new Attribute("Sprite", "DADG\\Recruitment\\left"),
        new Attribute("SaturationFactor", "0")
    };
}
[PrefabExtension("RecruitmentPopup", "descendant::*[@Sprite='SPGeneral\\SPRecruitment\\right_dent']")]
internal sealed class RecruitmentRightSkin : PrefabExtensionSetAttributePatch
{
    public override List<Attribute> Attributes => new()
    {
        new Attribute("Sprite", "DADG\\Recruitment\\right"),
        new Attribute("SaturationFactor", "0"),
        new Attribute("MarginTop", "70")
    };
}
[PrefabExtension("RecruitmentPopup", "descendant::*[@Sprite='StdAssets\\tabbar_popup']")]
internal sealed class RecruitmentTitleSkin : PrefabExtensionSetAttributePatch
{
    public override List<Attribute> Attributes => new() { new Attribute("Sprite", "DADG\\Recruitment\\title") };
}
[PrefabExtension("RecruitmentPopup", "descendant::*[@Brush='Recruitment.Popup.Title.Text']")]
internal sealed class RecruitmentTitleTextSkin : PrefabExtensionSetAttributePatch
{
    public override List<Attribute> Attributes => new() { new Attribute("Brush.FontColor", "#EEE5CFFF") };
}
[PrefabExtension("RecruitTroopPanel", "descendant::*[@Brush='Recruitment.Troop.Brush']")]
internal sealed class RecruitmentTroopSkin : PrefabExtensionSetAttributePatch
{
    public override List<Attribute> Attributes => new() { new Attribute("Brush", "DADG.Recruitment.Troop") };
}
[PrefabExtension("RecruitTroopPanelCart", "descendant::*[@Brush='Recruitment.Troop.Cart.Brush']")]
internal sealed class RecruitmentCartSkin : PrefabExtensionSetAttributePatch
{
    public override List<Attribute> Attributes => new() { new Attribute("Brush", "DADG.Recruitment.Cart") };
}
[PrefabExtension("RecruitVolunteerTuple", "descendant::*[@Sprite='SPGeneral\\SPRecruitment\\portrait_patron']")]
internal sealed class RecruitmentOwnerSkin : PrefabExtensionSetAttributePatch
{
    public override List<Attribute> Attributes => new() { new Attribute("Sprite", "DADG\\Recruitment\\owner") };
}
[PrefabExtension("RecruitTroopPanel", "descendant::*[@Brush='GameMenu.Tableau.Background.Character']")]
internal sealed class RecruitmentTroopHighlightSkin : PrefabExtensionSetAttributePatch
{
    public override List<Attribute> Attributes => new() { new Attribute("Brush", "DADG.Recruitment.Highlight") };
}
[PrefabExtension("RecruitTroopPanelCart", "descendant::*[@Brush='GameMenu.Tableau.Background.Character']")]
internal sealed class RecruitmentCartHighlightSkin : PrefabExtensionSetAttributePatch
{
    public override List<Attribute> Attributes => new() { new Attribute("Brush", "DADG.Recruitment.Highlight") };
}
[PrefabExtension("RecruitVolunteerTuple", "descendant::*[@Id='PartyItemWidget']")]
internal sealed class RecruitmentOwnerHighlightSkin : PrefabExtensionSetAttributePatch
{
    public override List<Attribute> Attributes => new()
    {
        new Attribute("PartyBackgroundBrush", "DADG.Recruitment.Highlight"),
        new Attribute("CharacterBackgroundBrush", "DADG.Recruitment.Highlight")
    };
}
