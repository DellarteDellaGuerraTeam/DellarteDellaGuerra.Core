using DellarteDellaGuerra.Domain.DisplayCompilingShaders;
using TaleWorlds.Core;
using TaleWorlds.Engine;

namespace DellarteDellaGuerra.DisplayCompilingShaders
{
    /**
     * <summary>
     *    This class is responsible for notifying the user when the game is compiling shaders.
     *    <br/>
     *    <br/>
     *    The notification is displayed only if the configuration has the EnableShaderCompilationNotifications property set to true.
     * </summary>
     */
    public class CompilingShaderNotifier : GameHandler
    {
        private float _tickCount;
        private static DisplayShaderNumber _displayShaderNumber;

        public CompilingShaderNotifier()
        {
            _tickCount = 0f;
        }        

        /// <summary>
        /// Initialises the dependency for the CompilingShaderNotifier.
        /// </summary>
        /// <param name="displayShaderNumber"></param>
        public static void Init(DisplayShaderNumber displayShaderNumber)
        {
            _displayShaderNumber = displayShaderNumber;
        }

        protected override void OnTick(float dt)
        {
            base.OnTick(dt);
            _tickCount += dt;
            if (LoadingWindow.IsLoadingWindowActive || _tickCount <= 1)
            {
                return;
            }

            if (_displayShaderNumber.DisplayNumberOfRemainingCompilingShaders())
            {
                _tickCount = 0f;
            }
        }

        public override void OnBeforeSave() { }

        public override void OnAfterSave() { }
    }    
}
