using System;
using System.Collections.Generic;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.ViewModels;
using DellarteDellaGuerra.Domain.PoliticalMap.Labels;
using DellarteDellaGuerra.Domain.PoliticalMap.Overlay;
using DellarteDellaGuerra.PoliticalMap.Api.Campaign;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapBar;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.InputSystem;
using TaleWorlds.ScreenSystem;

namespace DellarteDellaGuerra.Integration.PoliticalMap.UI
{
    /// <summary>
    /// Backs the political map button on the campaign map bar: opens and closes its panel, lists
    /// the views with the current one marked, and switches between title and clan labels.
    /// Follows the overlay's <see cref="PoliticalMapOverlayCampaignBehavior.Changed"/> event, so
    /// console-command changes show here too. The map bar view model lives as long as
    /// the map screen, which only exists inside a running campaign.
    /// <para>
    /// <c>handleDerived</c> is required: with War Sails loaded the map bar is a
    /// <c>NavalDLC.ViewModelCollection.Map.MapBar.NavalMapBarVM</c>, and UIExtenderEx only attaches a
    /// mixin to instances of exactly the registered type unless told to reach derived types. Without
    /// it the prefab's mixin bindings resolve to nothing: empty buttons.
    /// </para>
    /// </summary>
    [ViewModelMixin(nameof(MapBarVM.Initialize), handleDerived: true)]
    public sealed class MapBarPoliticalMapMixin : BaseViewModelMixin<MapBarVM>
    {
        private static MapBarPoliticalMapMixin? _current;
        private static readonly PoliticalMapView[] Views =
        {
            PoliticalMapView.Kingdom,
            PoliticalMapView.Owners,
            PoliticalMapView.DeJure,
            PoliticalMapView.Off
        };

        private readonly PoliticalMapOverlayCampaignBehavior? _overlay;
        private readonly PoliticalMapLabelsCampaignBehavior? _labels;
        private bool _isLabelModeVisible;
        private string _labelModeText = string.Empty;
        private bool _isPanelOpen;
        private readonly MapNavigationItemVM _toolbarItem;
        private MBBindingList<MapNavigationItemVM>? _navigationItems;
        private InputKey _toggleKey = InputKey.Invalid;

        public MapBarPoliticalMapMixin(MapBarVM vm) : base(vm)
        {
            _current = this;
            _overlay = Campaign.Current?.GetCampaignBehavior<PoliticalMapOverlayCampaignBehavior>();
            _toolbarItem = new MapNavigationItemVM(new PoliticalMapNavigationElement(
                ExecuteTogglePoliticalMapPanel,
                () => IsPoliticalMapPanelOpen));
            foreach (PoliticalMapView view in Views)
            {
                PoliticalMapViews.Add(new PoliticalMapViewItemVM(view, NameOf(view), Select));
            }

            if (_overlay is not null) _overlay.Changed += OnOverlayChanged;
            _labels = Campaign.Current?.GetCampaignBehavior<PoliticalMapLabelsCampaignBehavior>();
            if (_labels is not null) _labels.Modes.Changed += OnOverlayChanged;
            Refresh();
        }

        public override void OnRefresh()
        {
            base.OnRefresh();
            // UIExtender constructs this mixin before MapBarVM.Initialize creates navigation.
            var navigationItems = ViewModel?.MapNavigation?.NavigationItems;
            if (!ReferenceEquals(_navigationItems, navigationItems))
            {
                _navigationItems?.Remove(_toolbarItem);
                _navigationItems = navigationItems;
                if (_navigationItems is not null)
                {
                    int index = 0;
                    while (index < _navigationItems.Count && _navigationItems[index].ItemId != "clan") index++;
                    _navigationItems.Insert(index, _toolbarItem);
                }
            }
            Refresh();
        }

        [DataSourceProperty]
        public bool IsLabelModeVisible { get => _isLabelModeVisible; private set => Set(ref _isLabelModeVisible, value, nameof(IsLabelModeVisible)); }

        [DataSourceProperty]
        public string LabelModeText { get => _labelModeText; private set => Set(ref _labelModeText, value, nameof(LabelModeText)); }

        /// <summary>Switches the current view's region labels between titles and clans; only the text changes.</summary>
        [DataSourceMethod]
        public void ExecuteTogglePoliticalMapLabelMode()
        {
            if (_overlay is not null) _labels?.Modes.Toggle(_overlay.View);
        }

        // The label button in the map selector names what the active view's labels show now.
        private void RefreshLabelMode()
        {
            PoliticalMapView view = _overlay?.View ?? PoliticalMapView.Off;
            IsLabelModeVisible = _labels is not null && PoliticalMapLabelModes.HasChoice(view);
            if (!IsLabelModeVisible) return;

            bool titles = _labels!.Modes.Of(view) == PoliticalMapLabelMode.Titles;
            LabelModeText = titles
                ? new TextObject("{=dadg_polmap_labels_titles}Labels: Titles").ToString()
                : new TextObject("{=dadg_polmap_labels_clans}Labels: Clans").ToString();
        }

        [DataSourceProperty] public MBBindingList<PoliticalMapViewItemVM> PoliticalMapViews { get; } = new();

        [DataSourceProperty]
        public bool IsPoliticalMapPanelOpen
        {
            get => _isPanelOpen;
            private set
            {
                if (_isPanelOpen == value) return;
                _isPanelOpen = value;
                OnPropertyChangedWithValue(value, nameof(IsPoliticalMapPanelOpen));
                _toolbarItem.RefreshStates();
            }
        }

        [DataSourceMethod]
        public void ExecuteTogglePoliticalMapPanel() => IsPoliticalMapPanelOpen = !IsPoliticalMapPanelOpen;

        internal static void TickInput()
        {
            MapBarPoliticalMapMixin? current = _current;
            if (current?.ViewModel?.IsEnabled != true || current.ViewModel.MapTimeControl?.IsCenterPanelEnabled != true
                || current.ViewModel.MapTimeControl.IsEscapeMenuOpened
                || ScreenManager.TopScreen is not MapScreen mapScreen
                || mapScreen.SceneLayer?.Input is not { IsKeysAllowed: true } input
                || ScreenManager.FocusedLayer?.IsFocusedOnInput() == true
                || Campaign.Current?.ConversationManager?.IsConversationInProgress == true) return;

            if (current._toggleKey == InputKey.Invalid) current._toggleKey = ResolveToggleKey();
            if (input.IsKeyPressed(current._toggleKey)
                && !input.IsKeyDown(InputKey.LeftControl) && !input.IsKeyDown(InputKey.RightControl)
                && !input.IsKeyDown(InputKey.LeftAlt) && !input.IsKeyDown(InputKey.RightAlt)
                && !input.IsKeyDown(InputKey.LeftShift) && !input.IsKeyDown(InputKey.RightShift))
                current.ExecuteTogglePoliticalMapPanel();
        }

        private static InputKey ResolveToggleKey()
        {
            // InputKey names describe physical positions; use the key labelled M on AZERTY too.
            foreach (InputKey key in Enum.GetValues(typeof(InputKey)))
                if ((int)key > 0 && (int)key < 128 && Input.GetVirtualKeyCode(key) == 'M') return key;
            return InputKey.M;
        }

        public override void OnFinalize()
        {
            if (ReferenceEquals(_current, this)) _current = null;
            if (_overlay is not null) _overlay.Changed -= OnOverlayChanged;
            if (_labels is not null) _labels.Modes.Changed -= OnOverlayChanged;
            _navigationItems?.Remove(_toolbarItem);
            base.OnFinalize();
        }

        private void Select(PoliticalMapView view) => _overlay?.SetView(view);

        private void OnOverlayChanged()
        {
            if (_overlay?.View == PoliticalMapView.Off) IsPoliticalMapPanelOpen = false;
            Refresh();
        }

        private void Refresh()
        {
            foreach (PoliticalMapViewItemVM item in PoliticalMapViews) item.IsSelected = _overlay?.View == item.View;
            RefreshLabelMode();
            _toolbarItem.RefreshStates();
        }

        private void Set<T>(ref T field, T value, string propertyName)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return;
            field = value;
            OnPropertyChangedWithValue(value, propertyName);
        }

        private static string NameOf(PoliticalMapView view) => view switch
        {
            PoliticalMapView.Kingdom => new TextObject("{=dadg_polmap_view_kingdom}Kingdoms").ToString(),
            PoliticalMapView.Owners => new TextObject("{=dadg_polmap_view_owners}Owners").ToString(),
            PoliticalMapView.DeJure => new TextObject("{=dadg_polmap_view_de_jure}De jure").ToString(),
            _ => new TextObject("{=dadg_polmap_view_off}Off").ToString()
        };
    }

    /// <summary>One view button, marked when it is the one shown.</summary>
    public sealed class PoliticalMapViewItemVM : ViewModel
    {
        private readonly string _name;
        private readonly Action<PoliticalMapView> _select;
        private bool _isSelected;

        public PoliticalMapViewItemVM(PoliticalMapView view, string name, Action<PoliticalMapView> select)
        {
            View = view;
            _name = name;
            _select = select;
        }

        public PoliticalMapView View { get; }

        [DataSourceProperty] public string Name => (_isSelected ? "> " : string.Empty) + _name;

        [DataSourceProperty]
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value) return;
                _isSelected = value;
                OnPropertyChangedWithValue(value, nameof(IsSelected));
                OnPropertyChangedWithValue(Name, nameof(Name));
            }
        }

        public void ExecuteSelect() => _select(View);
    }
}
