using System.Reflection;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.PoliticalMap.Api.Campaign;
using Harmony.DependencyInjection.Patches;
using HarmonyLib;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.InputSystem;
using DellarteDellaGuerra.Domain.PoliticalMap.Overlay;
using DellarteDellaGuerra.Domain.PoliticalMap.Labels;
using DellarteDellaGuerra.Integration.Titles.UI;

namespace DellarteDellaGuerra.Integration.PoliticalMap
{
    /// <summary>Native dispatch already excludes UI clicks and camera drags; select instead of travelling.</summary>
    public class SelectPoliticalMapRegionPatch : IPatch
    {
        private readonly ILogger _logger;

        public SelectPoliticalMapRegionPatch(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<SelectPoliticalMapRegionPatch>();
        }

        public MethodInfo? TargetMethod
        {
            get
            {
                MethodInfo? method = AccessTools.DeclaredMethod(typeof(MapScreen), "HandleLeftMouseButtonClick");
                if (method is null) _logger.Error("MapScreen.HandleLeftMouseButtonClick changed: political map region selection is unavailable.");
                return method;
            }
        }

        public MethodInfo? PatchMethod => AccessTools.Method(typeof(SelectPoliticalMapRegionPatch), nameof(Prefix));
        public PatchType PatchType => PatchType.Prefix;

        private static bool Prefix(MapScreen __instance, CampaignVec2 intersectionPoint)
        {
            var behavior = Campaign.Current?.GetCampaignBehavior<PoliticalMapOverlayCampaignBehavior>();
            if (behavior?.TrySelectAt(__instance, intersectionPoint.X, intersectionPoint.Y) != true) return true;
            if ((Input.IsKeyDown(InputKey.LeftControl) || Input.IsKeyDown(InputKey.RightControl))
                && behavior.Selection is { } selection)
            {
                if (PoliticalMapPainter.SettlementIdFromKey(selection.Key) is { } seat)
                {
                    if (Settlement.Find(seat) is { } settlement)
                        Campaign.Current.EncyclopediaManager.GoToLink(settlement.EncyclopediaLink);
                }
                else if (selection.View == PoliticalMapView.DeJure
                    || selection.View == PoliticalMapView.Owners && selection.Mode == PoliticalMapLabelMode.Titles)
                    TitleEncyclopediaEntry.Open(selection.Key, selection.View == PoliticalMapView.Owners
                        && selection.Level == PoliticalMapOwnerLevel.SubVassals);
                else
                {
                    IFaction? faction = selection.View == PoliticalMapView.Kingdom
                        ? Campaign.Current?.CampaignObjectManager.Find<Kingdom>(selection.Key) : null;
                    faction ??= Campaign.Current?.CampaignObjectManager.Find<Clan>(selection.Key);
                    if (faction is not null) Campaign.Current?.EncyclopediaManager.GoToLink(faction.EncyclopediaLink);
                }
            }
            return false;
        }
    }
}
