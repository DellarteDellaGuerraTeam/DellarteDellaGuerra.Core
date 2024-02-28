using System;
using DellarteDellaGuerra.Configuration.Models;
using DellarteDellaGuerra.Configuration.Providers;
using DellarteDellaGuerra.Utils;
using TaleWorlds.Core;
using TaleWorlds.Engine;

namespace DellarteDellaGuerra.GameManager
{

    /**
     * <summary>
     *    This class is responsible for notifying the user when the game is compiling shaders.
     *    <br/>
     *    <br/>
     *    The notification is displayed only if the configuration has the EnableShaderCompilationNotifications property set to true.
     * </summary>
     * <typeparam name="T">The type of the configuration file.</typeparam>
     * <seealso cref="DadgConfig"/>
     */
    public class ShaderCompilationNotifier<T> : GameHandler where T : IConfigurationProvider<DadgConfig>, new()
    {
        private float _tickCount;
        private readonly T _configWatcher = Activator.CreateInstance<T>();

        protected override void OnTick(float dt)
        {
            base.OnTick(dt);
            _tickCount += dt;
            if (LoadingWindow.IsLoadingWindowActive
                || _tickCount <= 1
                || !(_configWatcher.Config?.EnableShaderCompilationNotifications ?? true))
            {
                return;
            }

            var remainingShaders = Utilities.GetNumberOfShaderCompilationsInProgress();
            if (remainingShaders <= 0)
            {
                return;
            }
            _tickCount = 0f;
            InfoPrinter.Display($"Shader compilation in progress. {remainingShaders} remaining.");
        }

        public override void OnBeforeSave() { }

        public override void OnAfterSave() { }
    }
}