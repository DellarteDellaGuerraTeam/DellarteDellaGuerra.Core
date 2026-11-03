using System;
using System.Reflection;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.PoliticalMap.Labels;
using Harmony.DependencyInjection.Patches;
using HarmonyLib;
using SandBox.ViewModelCollection.Nameplate;

namespace DellarteDellaGuerra.Integration.PoliticalMap
{
    /// <summary>
    /// Suppresses party and army nameplates through their per-frame UI visibility calculation.
    /// Vanilla recalculates the value on the next frame after the political map is turned off.
    /// </summary>
    public class HidePartyNameplatesPatch : IPatch
    {
        private readonly ILogger _logger;

        public HidePartyNameplatesPatch(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<HidePartyNameplatesPatch>();
        }

        public MethodInfo? TargetMethod
        {
            get
            {
                MethodInfo? method = AccessTools.DeclaredMethod(typeof(PartyNameplateVM),
                    nameof(PartyNameplateVM.DetermineIsVisibleOnMap), Type.EmptyTypes);
                if (method is null || AccessTools.Field(typeof(PartyNameplateVM), "_isVisibleOnMapBind") is null)
                {
                    _logger.Error("PartyNameplateVM visibility calculation changed: party and army nameplates stay over the political map.");
                    return null;
                }

                return method;
            }
        }

        public MethodInfo? PatchMethod => AccessTools.Method(typeof(HidePartyNameplatesPatch), nameof(Postfix));

        public PatchType PatchType => PatchType.Postfix;

        private static void Postfix(ref bool ____isVisibleOnMapBind)
        {
            // Harmony's three-underscore field injection includes the field's own leading underscore.
            if (PoliticalMapNameplates.HideNativeOverlays) ____isVisibleOnMapBind = false;
        }
    }
}
