using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.ScreenSystem;

namespace DellarteDellaGuerra.Integration.Titles.UI
{
    /// <summary>
    /// Standalone Gauntlet screen showing the whole de jure feudal hierarchy.
    /// Pushed on top of whatever screen is active (map, encyclopedia) and popped on Exit.
    /// </summary>
    public class FeudalHierarchyScreen : ScreenBase
    {
        private GauntletLayer? _gauntletLayer;
        private FeudalHierarchyScreenVM? _dataSource;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            _dataSource = new FeudalHierarchyScreenVM(Close);
            _gauntletLayer = new GauntletLayer("FeudalHierarchyScreen", 100);
            _gauntletLayer.LoadMovie("FeudalHierarchyScreen", _dataSource);
            _gauntletLayer.InputRestrictions.SetInputRestrictions();
            _gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
            _gauntletLayer.IsFocusLayer = true;
            AddLayer(_gauntletLayer);
            ScreenManager.TrySetFocus(_gauntletLayer);
        }

        protected override void OnFrameTick(float dt)
        {
            base.OnFrameTick(dt);
            if (_gauntletLayer?.Input.IsHotKeyReleased("Exit") == true) Close();
        }

        protected override void OnFinalize()
        {
            if (_gauntletLayer is not null) RemoveLayer(_gauntletLayer);
            _dataSource?.OnFinalize();
            _gauntletLayer = null;
            _dataSource = null;
            base.OnFinalize();
        }

        private static void Close() => ScreenManager.PopScreen();
    }
}