using TaleWorlds.Engine;
using TaleWorlds.Engine.Screens;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI;
using TaleWorlds.MountAndBlade.View.Screens;
using EngineScene = TaleWorlds.Engine.Scene;

namespace DellarteDellaGuerra.Infrastructure.MainMenu;

/// <summary>
/// The main menu with the static 3D background (scene "main_menu_a", the DADG cathedral) instead of the
/// looping videos vanilla 1.4 plays. GameStateScreenManager picks the last registered screen for a state
/// among the active module assemblies, so this replaces GauntletInitialScreen without any patching.
/// </summary>
[GameStateScreen(typeof(InitialState))]
public class DadgInitialScreen : GauntletInitialScreen
{
    private const string SceneName = "main_menu_a";
    private const string CameraTag = "camera_instance";

    private EngineScene? _scene;
    private SceneLayer? _sceneLayer;
    private Camera? _sceneCamera;

    public DadgInitialScreen(InitialState initialState) : base(initialState)
    {
    }

    protected override void OnInitialize()
    {
        // Before the base call: the Gauntlet layer takes input focus last, and SceneLayer.OnActivate
        // would otherwise steal it.
        CreateSceneLayer();
        base.OnInitialize();
    }

    protected override void OnInitialScreenTick(float dt)
    {
        base.OnInitialScreenTick(dt);

        if (_scene == null || _sceneLayer == null || _sceneCamera == null)
            return;

        _sceneLayer.SetCamera(_sceneCamera);
        SoundManager.SetListenerFrame(_sceneCamera.Frame);
        _scene.Tick(dt);
    }

    // Pre-1.4 behaviour: an options screen pushed on top silences the scene's ambience.
    protected override void OnPause()
    {
        _scene?.FinishSceneSounds();
        base.OnPause();
    }

    protected override void OnFinalize()
    {
        if (_sceneLayer != null)
            RemoveLayer(_sceneLayer);
        _scene?.ManualInvalidate();
        _scene = null;
        _sceneLayer = null;
        _sceneCamera = null;
        base.OnFinalize();
    }

    // Stands in for RefreshScene (see MainMenuSkipVideoPatch): the base class dereferences the video view
    // unguarded in OnFinalize/OnDeactivate/OnPause/RefreshVideoAspect, so one has to exist, but it never
    // plays anything.
    internal void EnsureInertVideoView()
    {
        if (_videoPlayerView != null)
            return;

        _videoPlayerView = VideoPlayerView.CreateVideoPlayerView();
        _videoPlayerView.SetEnable(false);
    }

    private void CreateSceneLayer()
    {
        var scene = EngineScene.CreateNewScene(true, false, DecalAtlasGroup.All, "mono_renderscene");
        scene.SetName("DadgMainMenuScene");
        scene.SetPlaySoundEventsAfterReadyToRender(true);
        var initData = new SceneInitializationData(true);
        scene.Read(SceneName, ref initData, "");
        for (var i = 0; i < 40; i++)
            scene.Tick(0.1f);

        var camera = Camera.CreateCamera();
        var dofParams = default(TaleWorlds.Library.Vec3);
        scene.FindEntityWithTag(CameraTag).GetCameraParamsFromCameraScript(camera, ref dofParams);

        var layer = new SceneLayer();
        AddLayer(layer);
        layer.SceneView.SetResolutionScaling(true);
        layer.SetScene(scene);
        layer.SetCamera(camera);
        layer.SetSceneUsesShadows(true);
        layer.SceneView.SetEnable(true);

        _scene = scene;
        _sceneLayer = layer;
        _sceneCamera = camera;
    }
}
