using System;
using System.Reflection;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using Harmony.DependencyInjection.Patches;
using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using SandBox.View.Map;

namespace DellarteDellaGuerra.Integration.Visor.MapConversation
{
    /// <summary>
    /// Ticks <see cref="MapConversationVisorOpener"/> after the map conversation tableau has ticked its agents.
    /// </summary>
    public class MapConversationTableauTickPatch : IPatch
    {
        private static ILogger? _logger;
        private static IServiceProvider? _serviceProvider;
        private static MapConversationVisorOpener? _opener;

        // Patches are built when the patcher is resolved in OnSubModuleLoad, outside any try/catch, so the opener
        // (and the visor XML it loads) is only resolved on the first map conversation.
        public MapConversationTableauTickPatch(ILoggerFactory loggerFactory, IServiceProvider serviceProvider)
        {
            _logger = loggerFactory.CreateLogger<MapConversationTableauTickPatch>();
            _serviceProvider = serviceProvider;
        }

        public MethodInfo? TargetMethod
        {
            get
            {
                if (MapConversationVisorOpener.IsSupported)
                    return AccessTools.DeclaredMethod(typeof(MapConversationTableau),
                        nameof(MapConversationTableau.OnTick), new[] { typeof(float) });

                _logger?.Error("MapConversationTableau changed: lords will not open their visor in map conversations.");
                return null;
            }
        }

        public MethodInfo? PatchMethod => AccessTools.Method(typeof(MapConversationTableauTickPatch), nameof(Postfix));

        public PatchType PatchType => PatchType.Postfix;

        private static void Postfix(MapConversationTableau __instance, float dt)
        {
            try
            {
                _opener ??= _serviceProvider?.GetRequiredService<MapConversationVisorOpener>();
                _opener?.Tick(__instance, dt);
            }
            catch (Exception e)
            {
                _serviceProvider = null;
                _logger?.Error("Map conversation visor tick failed; disabled until restart.", e);
            }
        }
    }
}
