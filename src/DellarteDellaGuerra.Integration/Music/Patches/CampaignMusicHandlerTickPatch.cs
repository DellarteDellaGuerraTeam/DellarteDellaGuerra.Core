using System.Reflection;
using Harmony.DependencyInjection.Patches;
using HarmonyLib;
using psai.net;
using SandBox.View;

namespace DellarteDellaGuerra.Integration.Music.Patches;

/// <summary>
///     Patches <see cref="CampaignMusicHandler" /> so that the gap between campaign
///     music tracks is controlled by <c>RestSecondsMin/Max</c> in <c>soundtrack.xml</c>
///     rather than the vanilla hardcoded 30–120 second rest timer.
///     <para>
///         Vanilla <c>TickCampaignMusic</c> resets its own rest timer whenever PSAI is
///         not in <see cref="PsaiState.playing" />, including during PSAI's own brief
///         between-segment rest (<see cref="PsaiState.rest" />). This prefix skips the
///         vanilla method entirely while PSAI is managing a normal segment transition,
///         allowing PSAI's configured rest duration to take effect. The vanilla logic
///         is preserved for <see cref="PsaiState.silence" /> (e.g. music externally
///         stopped when entering a battle).
///     </para>
/// </summary>
public class CampaignMusicHandlerTickPatch : IPatch
{
    public MethodInfo? TargetMethod =>
        AccessTools.Method(typeof(CampaignMusicHandler), "TickCampaignMusic", new[] { typeof(float) });

    public MethodInfo? PatchMethod =>
        AccessTools.Method(typeof(CampaignMusicHandlerTickPatch), nameof(Prefix));

    public PatchType PatchType => PatchType.Prefix;

    /// <returns>
    ///     <c>false</c> (skip vanilla) while PSAI is in its own rest state so that
    ///     <c>RestSecondsMin/Max</c> from <c>soundtrack.xml</c> drives the gap.
    ///     <c>true</c> (run vanilla) otherwise, preserving normal stop/restart behaviour.
    /// </returns>
    private static bool Prefix()
    {
        return PsaiCore.Instance.GetPsaiInfo().psaiState != PsaiState.rest;
    }
}
