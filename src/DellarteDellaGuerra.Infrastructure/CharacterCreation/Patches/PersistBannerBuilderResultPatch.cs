using System.Reflection;
using DellarteDellaGuerra.Domain.CharacterCreation.Ports;
using Harmony.DependencyInjection.Patches;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade.GauntletUI;
using TaleWorlds.MountAndBlade.ViewModelCollection.BannerBuilder;

namespace DellarteDellaGuerra.Infrastructure.CharacterCreation.Patches;

public class PersistBannerBuilderResultPatch : IPatch
{
    private static IAdvancedBannerBuilderConfig _config = null!;

    private static readonly FieldInfo? DataSourceField =
        AccessTools.Field(typeof(GauntletBannerBuilderScreen), "_dataSource");

    public PersistBannerBuilderResultPatch(IAdvancedBannerBuilderConfig config)
    {
        _config = config;
    }

    public MethodInfo? TargetMethod =>
        AccessTools.Method(typeof(GauntletBannerBuilderScreen), "Exit", new[] { typeof(bool) });

    public MethodInfo? PatchMethod =>
        AccessTools.Method(typeof(PersistBannerBuilderResultPatch), nameof(Postfix));

    public PatchType PatchType => PatchType.Postfix;

    private static void Postfix(object __instance, bool isCancel)
    {
        var dataSource = DataSourceField?.GetValue(__instance) as BannerBuilderVM;
        var banner = dataSource?.CurrentBanner;
        bool shouldPersist = ShouldPersistResult(
            _config?.IsEnabled() ?? false,
            isCancel,
            Clan.PlayerClan != null,
            banner != null
        );
        if (!shouldPersist)
        {
            return;
        }

        uint primaryColor = banner.GetPrimaryColor();
        uint iconColor = banner.GetFirstIconColor();

        Clan.PlayerClan.Color = primaryColor;
        Clan.PlayerClan.Color2 = iconColor;
        Clan.PlayerClan.UpdateBannerColor(primaryColor, iconColor);
    }

    public static bool ShouldPersistResult(bool featureEnabled, bool isCancel, bool hasPlayerClan, bool hasBanner)
    {
        return featureEnabled && !isCancel && hasPlayerClan && hasBanner;
    }
}
