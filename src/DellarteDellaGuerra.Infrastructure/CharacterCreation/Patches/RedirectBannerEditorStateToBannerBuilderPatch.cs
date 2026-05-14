using System;
using System.Reflection;
using DellarteDellaGuerra.Domain.CharacterCreation.Ports;
using Harmony.DependencyInjection.Patches;
using HarmonyLib;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.Infrastructure.CharacterCreation.Patches;

public class RedirectBannerEditorStateToBannerBuilderPatch : IPatch
{
    private static IAdvancedBannerBuilderConfig _config = null!;

    private static readonly FieldInfo? OnEndActionField =
        AccessTools.Field(typeof(BannerEditorState), "_onEndAction");

    public RedirectBannerEditorStateToBannerBuilderPatch(IAdvancedBannerBuilderConfig config)
    {
        _config = config;
    }

    public MethodInfo? TargetMethod =>
        AccessTools.Method(typeof(GameStateManager), nameof(GameStateManager.PushState),
            new[] { typeof(GameState), typeof(int) });

    public MethodInfo? PatchMethod =>
        AccessTools.Method(typeof(RedirectBannerEditorStateToBannerBuilderPatch), nameof(Prefix));

    public PatchType PatchType => PatchType.Prefix;

    private static void Prefix(GameStateManager __instance, ref GameState gameState)
    {
        bool isBannerEditorState = gameState is BannerEditorState;
        bool hasEndActionCallback = false;

        if (isBannerEditorState)
        {
            hasEndActionCallback = OnEndActionField?.GetValue(gameState) is Action;
        }

        if (!ShouldRedirectState(_config?.IsEnabled() ?? false, isBannerEditorState, hasEndActionCallback))
        {
            return;
        }

        gameState = __instance.CreateState<BannerBuilderState>();
    }

    public static bool ShouldRedirectState(bool featureEnabled, bool isBannerEditorState, bool hasEndActionCallback)
    {
        return featureEnabled && isBannerEditorState && !hasEndActionCallback;
    }
}
