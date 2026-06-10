using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;

namespace DellarteDellaGuerra.Integration.Titles.UI.PrefabExtensions
{
    /// <summary>
    /// Inserts the feudal summary section right after the hero info container
    /// (and before the allies divider) on the hero encyclopedia page.
    /// The widgets reuse the page's own visual idiom: an EncyclopediaDivider header
    /// followed by definition/value stat rows.
    /// </summary>
    [PrefabExtension("EncyclopediaHeroPage", "descendant::Widget[@Id='InfoContainer']")]
    internal sealed class EncyclopediaHeroPageFeudalExtension : PrefabExtensionInsertPatch
    {
        public override InsertType Type => InsertType.Append;

        [PrefabExtensionText]
        public string Content => """
            <ListPanel Id="FeudalInfoSection" HeightSizePolicy="CoverChildren" WidthSizePolicy="StretchToParent" StackLayout.LayoutMethod="VerticalBottomToTop" DoNotAcceptEvents="true" IsVisible="@IsFeudalInfoVisible">
              <Children>
                <EncyclopediaDivider Id="FeudalDivider" MarginTop="20" Parameter.Title="@FeudalSectionText" Parameter.ItemList="..\FeudalInfoList"/>
                <ListPanel Id="FeudalInfoList" HeightSizePolicy="CoverChildren" WidthSizePolicy="StretchToParent" StackLayout.LayoutMethod="VerticalBottomToTop" DoNotAcceptEvents="true">
                  <Children>
                    <ListPanel HeightSizePolicy="CoverChildren" WidthSizePolicy="StretchToParent" MarginLeft="30" MarginTop="10">
                      <Children>
                        <AutoHideRichTextWidget HeightSizePolicy="CoverChildren" WidthSizePolicy="CoverChildren" VerticalAlignment="Bottom" Brush="Encyclopedia.Stat.DefinitionText" Text="@FeudalTitlesLabel" MarginRight="5"/>
                        <AutoHideRichTextWidget HeightSizePolicy="CoverChildren" WidthSizePolicy="StretchToParent" VerticalAlignment="Bottom" Brush="Encyclopedia.Stat.ValueText" Text="@FeudalTitlesText"/>
                      </Children>
                    </ListPanel>
                    <ListPanel HeightSizePolicy="CoverChildren" WidthSizePolicy="StretchToParent" MarginLeft="30" MarginTop="3" MarginBottom="5">
                      <Children>
                        <AutoHideRichTextWidget HeightSizePolicy="CoverChildren" WidthSizePolicy="CoverChildren" VerticalAlignment="Bottom" Brush="Encyclopedia.Stat.DefinitionText" Text="@FeudalSuzerainLabel" MarginRight="5"/>
                        <AutoHideRichTextWidget HeightSizePolicy="CoverChildren" WidthSizePolicy="StretchToParent" VerticalAlignment="Bottom" Brush="Encyclopedia.Stat.ValueText" Text="@FeudalSuzerainText"/>
                      </Children>
                    </ListPanel>
                  </Children>
                </ListPanel>
              </Children>
            </ListPanel>
            """;
    }
}