using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Harmony.DependencyInjection.Patches;
using HarmonyLib;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade.View;

namespace DellarteDellaGuerra.Infrastructure.Heraldry.Patches;

/// <summary>
/// Vanilla AgentVisuals releases a banner_replacement_mesh right after requesting its banner texture.
/// When that banner is still rendering the texture callback is deferred, then runs against a freed
/// mesh and throws (or faults natively). DADG tabards carry that tag, so keep the reference until applied.
/// </summary>
public class KeepBannerTabardMeshUntilTextureAppliedPatch : IPatch
{
    public MethodInfo? TargetMethod =>
        AccessTools.DeclaredMethod(typeof(AgentVisuals), "AddArmorMultiMeshesToAgentEntity");

    public MethodInfo? PatchMethod =>
        AccessTools.Method(typeof(KeepBannerTabardMeshUntilTextureAppliedPatch), nameof(Transpiler));

    public PatchType PatchType => PatchType.Transpiler;

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchEndForward(
                new CodeMatch(i => i.operand is MethodInfo { Name: nameof(BannerVisual.GetTableauTextureLarge) } m
                                   && m.DeclaringType == typeof(BannerVisual)),
                new CodeMatch(OpCodes.Pop),
                new CodeMatch(OpCodes.Ldloc_S),
                new CodeMatch(OpCodes.Ldfld),
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(NativeObject), nameof(NativeObject.ManualInvalidate))))
            .ThrowIfInvalid("AgentVisuals banner mesh release not found")
            .Set(OpCodes.Pop, null)
            .InstructionEnumeration();
    }
}

public class ReleaseBannerTabardMeshAfterTextureAppliedPatch : IPatch
{
    public MethodInfo? TargetMethod =>
        AccessTools.DeclaredMethod(typeof(AgentVisuals), "ApplyBannerTextureToMesh");

    public MethodInfo? PatchMethod =>
        AccessTools.Method(typeof(ReleaseBannerTabardMeshAfterTextureAppliedPatch), nameof(Postfix));

    public PatchType PatchType => PatchType.Postfix;

    // The deferred path never reaches this when the mesh is collected first; its finalizer releases it then.
    private static void Postfix(Mesh armorMesh) => armorMesh?.ManualInvalidate();
}
