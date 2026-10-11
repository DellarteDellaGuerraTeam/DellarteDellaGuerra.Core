using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade.View;

namespace DellarteDellaGuerra.Integration.UI.Visuals;

/// <summary>
/// Vanilla AgentVisuals releases a banner_replacement_mesh right after requesting its banner texture.
/// When that banner is still rendering the texture callback is deferred, then runs against a freed
/// mesh and throws (or faults natively). DADG tabards carry that tag, so keep the reference until applied.
/// </summary>
internal static class BannerTabardMeshLifetimePatch
{
    internal static void Apply()
    {
        var harmony = new HarmonyLib.Harmony("com.dadg.banner-tabard-mesh-lifetime");
        harmony.Patch(AccessTools.DeclaredMethod(typeof(AgentVisuals), "AddArmorMultiMeshesToAgentEntity"),
            transpiler: new HarmonyMethod(typeof(BannerTabardMeshLifetimePatch), nameof(KeepMeshUntilTextureApplied)));
        harmony.Patch(AccessTools.DeclaredMethod(typeof(AgentVisuals), "ApplyBannerTextureToMesh"),
            postfix: new HarmonyMethod(typeof(BannerTabardMeshLifetimePatch), nameof(ReleaseMesh)));
    }

    private static IEnumerable<CodeInstruction> KeepMeshUntilTextureApplied(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchEndForward(
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(BannerVisual), nameof(BannerVisual.GetTableauTextureLarge))),
                new CodeMatch(OpCodes.Pop),
                new CodeMatch(OpCodes.Ldloc_S),
                new CodeMatch(OpCodes.Ldfld),
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(NativeObject), nameof(NativeObject.ManualInvalidate))))
            .ThrowIfInvalid("AgentVisuals banner mesh release not found")
            .Set(OpCodes.Pop, null)
            .InstructionEnumeration();
    }

    // The deferred path never reaches this when the mesh is collected first; its finalizer releases it then.
    private static void ReleaseMesh(Mesh armorMesh) => armorMesh?.ManualInvalidate();
}
