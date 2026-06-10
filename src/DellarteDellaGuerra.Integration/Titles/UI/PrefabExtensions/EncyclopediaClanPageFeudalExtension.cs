using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;

namespace DellarteDellaGuerra.Integration.Titles.UI.PrefabExtensions
{
    /// <summary>
    /// Inserts the feudal section at the bottom of the clan encyclopedia page, right
    /// after the wars grid (the page's last section). The widgets reuse the page's own
    /// visual idiom: an EncyclopediaDivider header followed by definition/value stat
    /// rows, plus a popup-style button opening the full hierarchy screen.
    /// </summary>
    [PrefabExtension("EncyclopediaClanPage", "descendant::NavigatableGridWidget[@Id='WarsGrid']")]
    internal sealed class EncyclopediaClanPageFeudalExtension : PrefabExtensionInsertPatch
    {
        public override InsertType Type => InsertType.Append;

        [PrefabExtensionText]
        public string Content => """
            <ListPanel Id="FeudalInfoSection" HeightSizePolicy="CoverChildren" WidthSizePolicy="StretchToParent" StackLayout.LayoutMethod="VerticalBottomToTop" IsVisible="@IsFeudalInfoVisible">
              <Children>
                <EncyclopediaDivider Id="FeudalDivider" MarginTop="20" Parameter.Title="@FeudalSectionText" Parameter.ItemList="..\FeudalInfoList"/>
                <ListPanel Id="FeudalInfoList" HeightSizePolicy="CoverChildren" WidthSizePolicy="StretchToParent" StackLayout.LayoutMethod="VerticalBottomToTop">
                  <Children>
                    <ListPanel HeightSizePolicy="CoverChildren" WidthSizePolicy="StretchToParent" MarginLeft="30" MarginTop="10">
                      <Children>
                        <AutoHideRichTextWidget HeightSizePolicy="CoverChildren" WidthSizePolicy="CoverChildren" VerticalAlignment="Bottom" Brush="Encyclopedia.Stat.DefinitionText" Text="@FeudalTitlesLabel" MarginRight="5"/>
                        <AutoHideRichTextWidget HeightSizePolicy="CoverChildren" WidthSizePolicy="StretchToParent" VerticalAlignment="Bottom" Brush="Encyclopedia.Stat.ValueText" Text="@FeudalTitlesText"/>
                      </Children>
                    </ListPanel>
                    <ListPanel HeightSizePolicy="CoverChildren" WidthSizePolicy="StretchToParent" MarginLeft="30" MarginTop="3">
                      <Children>
                        <AutoHideRichTextWidget HeightSizePolicy="CoverChildren" WidthSizePolicy="CoverChildren" VerticalAlignment="Bottom" Brush="Encyclopedia.Stat.DefinitionText" Text="@FeudalSuzerainLabel" MarginRight="5"/>
                        <AutoHideRichTextWidget HeightSizePolicy="CoverChildren" WidthSizePolicy="StretchToParent" VerticalAlignment="Bottom" Brush="Encyclopedia.Stat.ValueText" Text="@FeudalSuzerainText"/>
                      </Children>
                    </ListPanel>
                    <ListPanel HeightSizePolicy="CoverChildren" WidthSizePolicy="StretchToParent" MarginLeft="30" MarginTop="3">
                      <Children>
                        <AutoHideRichTextWidget HeightSizePolicy="CoverChildren" WidthSizePolicy="CoverChildren" VerticalAlignment="Bottom" Brush="Encyclopedia.Stat.DefinitionText" Text="@FeudalLiegeChainLabel" MarginRight="5"/>
                        <AutoHideRichTextWidget HeightSizePolicy="CoverChildren" WidthSizePolicy="StretchToParent" VerticalAlignment="Bottom" Brush="Encyclopedia.Stat.ValueText" Text="@FeudalLiegeChainText"/>
                      </Children>
                    </ListPanel>
                    <ListPanel HeightSizePolicy="CoverChildren" WidthSizePolicy="StretchToParent" MarginLeft="30" MarginTop="3">
                      <Children>
                        <AutoHideRichTextWidget HeightSizePolicy="CoverChildren" WidthSizePolicy="CoverChildren" VerticalAlignment="Bottom" Brush="Encyclopedia.Stat.DefinitionText" Text="@FeudalVassalsLabel" MarginRight="5"/>
                        <AutoHideRichTextWidget HeightSizePolicy="CoverChildren" WidthSizePolicy="StretchToParent" VerticalAlignment="Bottom" Brush="Encyclopedia.Stat.ValueText" Text="@FeudalVassalsText"/>
                      </Children>
                    </ListPanel>
                    <ListPanel HeightSizePolicy="CoverChildren" WidthSizePolicy="StretchToParent" MarginLeft="30" MarginTop="3">
                      <Children>
                        <AutoHideRichTextWidget HeightSizePolicy="CoverChildren" WidthSizePolicy="CoverChildren" VerticalAlignment="Bottom" Brush="Encyclopedia.Stat.DefinitionText" Text="@FeudalLeviesLabel" MarginRight="5"/>
                        <AutoHideRichTextWidget HeightSizePolicy="CoverChildren" WidthSizePolicy="StretchToParent" VerticalAlignment="Bottom" Brush="Encyclopedia.Stat.ValueText" Text="@FeudalLeviesText"/>
                      </Children>
                    </ListPanel>
                    <ButtonWidget DoNotPassEventsToChildren="true" WidthSizePolicy="Fixed" HeightSizePolicy="Fixed" SuggestedWidth="340" SuggestedHeight="58" HorizontalAlignment="Left" MarginLeft="30" MarginTop="15" MarginBottom="10" Brush="Popup.Done.Button.NineGrid" Command.Click="ExecuteViewFeudalHierarchy" UpdateChildrenStates="true">
                      <Children>
                        <TextWidget WidthSizePolicy="CoverChildren" HeightSizePolicy="StretchToParent" HorizontalAlignment="Center" VerticalAlignment="Center" Brush="Popup.Button.Text" Text="@ViewHierarchyText"/>
                      </Children>
                    </ButtonWidget>
                  </Children>
                </ListPanel>
              </Children>
            </ListPanel>
            """;
    }
}