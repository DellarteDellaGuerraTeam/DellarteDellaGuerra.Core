using System.Runtime.CompilerServices;
using DellarteDellaGuerra.PrivateWars.Api.Patches;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;

namespace DellarteDellaGuerra.Tests.PrivateWars;

public class PrivateWarCaptivityPatchTests
{
    [Fact]
    public void TargetResolver_SelectsExactCheckCaptivityChangeOverload()
    {
        var target = PrivateWarHarmonyPatchTargets.PlayerCaptivityCheck();

        Assert.Equal(typeof(PlayerCaptivityCampaignBehavior), target.DeclaringType);
        Assert.Equal(nameof(PlayerCaptivityCampaignBehavior.CheckCaptivityChange), target.Name);
        Assert.Equal(typeof(void), target.ReturnType);
        Assert.Equal([typeof(float)], target.GetParameters().Select(parameter => parameter.ParameterType));
    }

    [Fact]
    public void Wrapper_InterceptsOnlyTheNoMoreEnemiesPredicateWithExactCountGuard()
    {
        var source = ReadPatchSource("PlayerCaptivityRetentionPatch.cs");

        Assert.Contains("PrivateWarHarmonyPatchTargets.PlayerCaptivityCheck()", source, StringComparison.Ordinal);
        Assert.Contains("PatchType.Transpiler", source, StringComparison.Ordinal);
        Assert.Contains("PlayerCaptivity.CaptorParty", source, StringComparison.Ordinal);
        Assert.Contains("ResolveCaptivityWarPredicate", source, StringComparison.Ordinal);
        Assert.Contains("replaced != 1", source, StringComparison.Ordinal);
        Assert.Contains("throw new InvalidOperationException", source, StringComparison.Ordinal);
        Assert.DoesNotContain("SkipIfPrivateWarCaptive", source, StringComparison.Ordinal);
        Assert.DoesNotContain("PatchType.Prefix", source, StringComparison.Ordinal);
    }

    [Fact]
    public void RuntimeIl_ContainsExactlyOneNoMoreEnemiesWarPredicate()
    {
        var vanillaPredicate = typeof(FactionManager).GetMethod(
            nameof(FactionManager.IsAtWarAgainstFaction),
            [typeof(IFaction), typeof(IFaction)])!;

        Assert.Equal(
            1,
            MethodIlCalls.Count(PrivateWarHarmonyPatchTargets.PlayerCaptivityCheck(), vanillaPredicate));
    }

    private static string ReadPatchSource(string fileName, [CallerFilePath] string testPath = "")
    {
        var sourceRoot = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(testPath)!, "..", ".."));
        return File.ReadAllText(Path.Combine(
            sourceRoot,
            "DellarteDellaGuerra.Integration",
            "PrivateWars",
            "Patches",
            fileName));
    }
}
