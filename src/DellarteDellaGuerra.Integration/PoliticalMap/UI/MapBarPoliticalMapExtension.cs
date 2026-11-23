using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;

namespace DellarteDellaGuerra.Integration.PoliticalMap.UI
{
    /// <summary>
    /// Adds the compact selector above the native toolbar's political-map icon.
    /// </summary>
    [PrefabExtension("MapBar", "descendant::MapCurrentTimeVisualWidget[@Id='CenterPanel']")]
    internal sealed class MapBarPoliticalMapExtension : PrefabExtensionInsertPatch
    {
        public override InsertType Type => InsertType.Append;

        [PrefabExtensionText]
        public string Content => """
            <Widget Id="PoliticalMapControl" WidthSizePolicy="Fixed" HeightSizePolicy="CoverChildren" SuggestedWidth="240" HorizontalAlignment="Left" VerticalAlignment="Bottom" MarginLeft="380" MarginBottom="72" Sprite="BlankWhiteSquare_9" Color="#0E0B08DD" IsVisible="@IsPoliticalMapPanelOpen">
              <Children>
                <ListPanel WidthSizePolicy="StretchToParent" HeightSizePolicy="CoverChildren" MarginLeft="8" MarginRight="8" MarginTop="8" MarginBottom="8" StackLayout.LayoutMethod="VerticalTopToBottom">
                  <Children>
                    <ListPanel DataSource="{PoliticalMapViews}" WidthSizePolicy="StretchToParent" HeightSizePolicy="CoverChildren" StackLayout.LayoutMethod="VerticalTopToBottom">
                      <ItemTemplate>
                        <ButtonWidget DoNotPassEventsToChildren="true" WidthSizePolicy="StretchToParent" HeightSizePolicy="Fixed" SuggestedHeight="44" MarginBottom="2" Brush="ButtonBrush2" Command.Click="ExecuteSelect" IsSelected="@IsSelected" UpdateChildrenStates="true">
                          <Children>
                            <TextWidget WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent" Brush="Popup.Button.Text" Brush.FontSize="19" Text="@Name"/>
                          </Children>
                        </ButtonWidget>
                      </ItemTemplate>
                    </ListPanel>
                    <ButtonWidget DoNotPassEventsToChildren="true" WidthSizePolicy="StretchToParent" HeightSizePolicy="Fixed" SuggestedHeight="44" MarginTop="4" Brush="ButtonBrush2" Command.Click="ExecuteTogglePoliticalMapLabelMode" IsVisible="@IsLabelModeVisible" UpdateChildrenStates="true">
                      <Children>
                        <TextWidget WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent" Brush="Popup.Button.Text" Brush.FontSize="19" Text="@LabelModeText"/>
                      </Children>
                    </ButtonWidget>
                  </Children>
                </ListPanel>
              </Children>
            </Widget>
            """;
    }
}
