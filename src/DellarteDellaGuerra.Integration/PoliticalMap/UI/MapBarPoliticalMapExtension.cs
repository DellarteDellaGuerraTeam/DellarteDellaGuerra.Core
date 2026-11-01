using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;

namespace DellarteDellaGuerra.Integration.PoliticalMap.UI
{
    /// <summary>
    /// Adds the map-view and label-name buttons, with a compact view selector beneath them.
    /// </summary>
    [PrefabExtension("MapBar", "descendant::MapCurrentTimeVisualWidget[@Id='CenterPanel']")]
    internal sealed class MapBarPoliticalMapExtension : PrefabExtensionInsertPatch
    {
        public override InsertType Type => InsertType.Append;

        [PrefabExtensionText]
        public string Content => """
            <Widget Id="PoliticalMapControl" WidthSizePolicy="Fixed" HeightSizePolicy="CoverChildren" SuggestedWidth="440" HorizontalAlignment="Left" VerticalAlignment="Top" MarginLeft="16" MarginTop="60">
              <Children>
                <ListPanel WidthSizePolicy="StretchToParent" HeightSizePolicy="CoverChildren" StackLayout.LayoutMethod="VerticalTopToBottom">
                  <Children>
                    <ListPanel WidthSizePolicy="StretchToParent" HeightSizePolicy="Fixed" SuggestedHeight="50" StackLayout.LayoutMethod="HorizontalLeftToRight">
                      <Children>
                        <ButtonWidget DoNotPassEventsToChildren="true" WidthSizePolicy="Fixed" HeightSizePolicy="Fixed" SuggestedWidth="220" SuggestedHeight="50" Brush="ButtonBrush2" Command.Click="ExecuteTogglePoliticalMapPanel" UpdateChildrenStates="true">
                          <Children>
                            <TextWidget WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent" Brush="Popup.Button.Text" Brush.FontSize="22" Text="@PoliticalMapButtonText"/>
                          </Children>
                        </ButtonWidget>
                        <ButtonWidget DoNotPassEventsToChildren="true" WidthSizePolicy="Fixed" HeightSizePolicy="Fixed" SuggestedWidth="212" SuggestedHeight="50" MarginLeft="8" Brush="ButtonBrush2" Command.Click="ExecuteTogglePoliticalMapLabelMode" IsVisible="@IsLabelModeVisible" UpdateChildrenStates="true">
                          <Children>
                            <TextWidget WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent" Brush="Popup.Button.Text" Brush.FontSize="19" Text="@LabelModeText"/>
                          </Children>
                        </ButtonWidget>
                      </Children>
                    </ListPanel>
                    <Widget WidthSizePolicy="StretchToParent" HeightSizePolicy="CoverChildren" MarginTop="4" Sprite="BlankWhiteSquare_9" Color="#0E0B08DD" IsVisible="@IsPoliticalMapPanelOpen">
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
                          </Children>
                        </ListPanel>
                      </Children>
                    </Widget>
                  </Children>
                </ListPanel>
              </Children>
            </Widget>
            """;
    }
}
