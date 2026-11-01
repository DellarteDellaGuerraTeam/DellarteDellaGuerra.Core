using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;

namespace DellarteDellaGuerra.Integration.PoliticalMap.UI
{
    /// <summary>
    /// Adds the political map button to the top-left corner of the campaign map, with the small
    /// panel it opens under it (one row per view, its name over a one-line caption), a button
    /// beside it switching region labels between titles and clans in either map view and, while a
    /// view is drawn, the legend: a large "Political Map: view" title, one line saying what the
    /// colours mean, the kingdom swatches or the clan colour note, the hover readout and the
    /// hotkey hint. The bottom bar has no free slot (the
    /// button sat squeezed between the time controls and the info bar), and the top-left corner
    /// keeps the button beside the legend it explains.
    /// Inserted as a sibling of the centre panel inside the map bar's full-screen root, so it
    /// binds to the map bar's own view model (see <see cref="MapBarPoliticalMapMixin"/>) and hides
    /// with the bar. Every binding here is a mixin member: if the mixin is not attached to the
    /// live view model, the texts come out empty and every IsVisible binding reads false.
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
                                <ButtonWidget DoNotPassEventsToChildren="true" WidthSizePolicy="StretchToParent" HeightSizePolicy="Fixed" SuggestedHeight="62" MarginBottom="2" Brush="ButtonBrush2" Command.Click="ExecuteSelect" IsSelected="@IsSelected" UpdateChildrenStates="true">
                                  <Children>
                                    <ListPanel WidthSizePolicy="StretchToParent" HeightSizePolicy="CoverChildren" VerticalAlignment="Center" StackLayout.LayoutMethod="VerticalTopToBottom">
                                      <Children>
                                        <TextWidget WidthSizePolicy="StretchToParent" HeightSizePolicy="Fixed" SuggestedHeight="26" Brush="Popup.Button.Text" Brush.FontSize="19" Text="@Name"/>
                                        <TextWidget WidthSizePolicy="StretchToParent" HeightSizePolicy="CoverChildren" MarginLeft="8" MarginRight="8" Brush="MapTextBrushGal" Brush.FontSize="13" Text="@Caption"/>
                                      </Children>
                                    </ListPanel>
                                  </Children>
                                </ButtonWidget>
                              </ItemTemplate>
                            </ListPanel>
                            <TextWidget WidthSizePolicy="StretchToParent" HeightSizePolicy="Fixed" SuggestedHeight="26" MarginTop="4" Brush="MapTextBrushGal" Brush.FontSize="16" Text="@PoliticalMapHotkeyText"/>
                          </Children>
                        </ListPanel>
                      </Children>
                    </Widget>
                    <Widget WidthSizePolicy="StretchToParent" HeightSizePolicy="CoverChildren" MarginTop="6" Sprite="BlankWhiteSquare_9" Color="#0E0B08CC" IsVisible="@IsLegendVisible">
                      <Children>
                        <ListPanel WidthSizePolicy="StretchToParent" HeightSizePolicy="CoverChildren" MarginLeft="12" MarginRight="12" MarginTop="10" MarginBottom="10" StackLayout.LayoutMethod="VerticalTopToBottom">
                          <Children>
                            <TextWidget WidthSizePolicy="StretchToParent" HeightSizePolicy="CoverChildren" Brush="Popup.Button.Text" Brush.FontSize="30" Brush.TextHorizontalAlignment="Left" Text="@LegendTitle"/>
                            <TextWidget WidthSizePolicy="StretchToParent" HeightSizePolicy="CoverChildren" MarginTop="4" Brush="MapTextBrushGal" Brush.FontSize="18" Brush.TextHorizontalAlignment="Left" Text="@LegendCaption"/>
                            <ListPanel DataSource="{LegendEntries}" WidthSizePolicy="StretchToParent" HeightSizePolicy="CoverChildren" MarginTop="8" StackLayout.LayoutMethod="VerticalTopToBottom" IsVisible="@IsKingdomLegendVisible">
                              <ItemTemplate>
                                <ListPanel WidthSizePolicy="StretchToParent" HeightSizePolicy="CoverChildren" MarginBottom="4" StackLayout.LayoutMethod="HorizontalLeftToRight">
                                  <Children>
                                    <Widget WidthSizePolicy="Fixed" HeightSizePolicy="Fixed" SuggestedWidth="22" SuggestedHeight="22" VerticalAlignment="Center" Sprite="BlankWhiteSquare_9" Color="@Color"/>
                                    <TextWidget WidthSizePolicy="StretchToParent" HeightSizePolicy="CoverChildren" MarginLeft="10" VerticalAlignment="Center" Brush="MapTextBrushGal" Brush.FontSize="17" Brush.TextHorizontalAlignment="Left" Text="@Name"/>
                                  </Children>
                                </ListPanel>
                              </ItemTemplate>
                            </ListPanel>
                            <TextWidget WidthSizePolicy="StretchToParent" HeightSizePolicy="CoverChildren" MarginTop="8" Brush="MapTextBrushGal" Brush.FontSize="15" Brush.TextHorizontalAlignment="Left" Text="@ClanNoteText" IsVisible="@IsClanNoteVisible"/>
                            <ListPanel WidthSizePolicy="StretchToParent" HeightSizePolicy="CoverChildren" MarginTop="10" StackLayout.LayoutMethod="VerticalTopToBottom" IsVisible="@IsHoverVisible">
                              <Children>
                                <TextWidget WidthSizePolicy="StretchToParent" HeightSizePolicy="CoverChildren" Brush="MapTextBrushGal" Brush.FontSize="21" Brush.TextHorizontalAlignment="Left" Text="@HoverTitle"/>
                                <TextWidget WidthSizePolicy="StretchToParent" HeightSizePolicy="CoverChildren" MarginTop="2" Brush="MapTextBrushGal" Brush.FontSize="16" Brush.TextHorizontalAlignment="Left" Text="@HoverDetail"/>
                              </Children>
                            </ListPanel>
                            <TextWidget WidthSizePolicy="StretchToParent" HeightSizePolicy="CoverChildren" MarginTop="10" Brush="MapTextBrushGal" Brush.FontSize="14" Brush.TextHorizontalAlignment="Left" Text="@LegendHintText"/>
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
