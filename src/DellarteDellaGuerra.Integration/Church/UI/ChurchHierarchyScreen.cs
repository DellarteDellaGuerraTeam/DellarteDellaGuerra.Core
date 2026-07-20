using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.ScreenSystem;

namespace DellarteDellaGuerra.Integration.Church.UI
{
    /// <summary>
    /// Standalone Gauntlet screen showing the church diocese hierarchy.
    /// Pushed on top of whatever screen is active (map) and popped on Exit.
    /// </summary>
    public class ChurchHierarchyScreen : ScreenBase
    {
        private GauntletLayer? _gauntletLayer;
        private ChurchHierarchyScreenVM? _dataSource;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            _dataSource = new ChurchHierarchyScreenVM(Close);
            _gauntletLayer = new GauntletLayer("ChurchHierarchyScreen", 100);
            _gauntletLayer.LoadMovie("ChurchHierarchyScreen", _dataSource);
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
