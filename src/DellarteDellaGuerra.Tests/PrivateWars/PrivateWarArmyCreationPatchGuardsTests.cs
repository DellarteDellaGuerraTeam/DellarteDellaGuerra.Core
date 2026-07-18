using System.Runtime.CompilerServices;
using DellarteDellaGuerra.PrivateWars.Api.Armies;

namespace DellarteDellaGuerra.Tests.PrivateWars;

public class PrivateWarArmyCreationPatchGuardsTests
{
    [Fact]
    public void TestProject_ResolvesBannerlordRootBeforeUsingRuntimeReferences()
    {
        var testFilePath = GetTestFilePath();
        var projectPath = Path.GetFullPath(Path.Combine(
            Path.GetDirectoryName(testFilePath)!, "..", "DellarteDellaGuerra.Tests.csproj"));
        var projectSource = File.ReadAllText(projectPath);

        Assert.Contains("<BannerlordGameRoot Condition=", projectSource, StringComparison.Ordinal);
        Assert.Contains("Exists('$(GameFolder)\\bin\\Win64_Shipping_Client\\TaleWorlds.CampaignSystem.dll')", projectSource, StringComparison.Ordinal);
        Assert.Contains("Exists('$(GameFolder)\\..\\bin\\Win64_Shipping_Client\\TaleWorlds.CampaignSystem.dll')", projectSource, StringComparison.Ordinal);
        Assert.Contains("Exists('$(GameFolder)\\..\\..\\..\\bin\\Win64_Shipping_Client\\TaleWorlds.CampaignSystem.dll')", projectSource, StringComparison.Ordinal);
        Assert.DoesNotContain("<HintPath>$(GameFolder)", projectSource, StringComparison.Ordinal);
        Assert.Equal(4, projectSource.Split("<HintPath>$(BannerlordGameRoot)").Length - 1);
        Assert.Contains("Could not resolve Bannerlord game root", projectSource, StringComparison.Ordinal);
    }

    [Fact]
    public void TargetResolver_SelectsExactKingdomCreateArmyOverload()
    {
        var target = KingdomCreateArmyPatchTarget.Resolve();

        Assert.Equal("CreateArmy", target.Name);
        Assert.Equal(
            new[]
            {
                "TaleWorlds.CampaignSystem.Hero",
                "TaleWorlds.CampaignSystem.Settlements.Settlement",
                "TaleWorlds.CampaignSystem.Army+ArmyTypes",
                "TaleWorlds.Library.MBReadOnlyList`1[[TaleWorlds.CampaignSystem.Party.MobileParty"
            },
            target.GetParameters().Select(parameter =>
                parameter.ParameterType.FullName!.StartsWith("TaleWorlds.Library.MBReadOnlyList`1")
                    ? "TaleWorlds.Library.MBReadOnlyList`1[[TaleWorlds.CampaignSystem.Party.MobileParty"
                    : parameter.ParameterType.FullName));
    }

    [Fact]
    public void HarmonyWrapper_IsRegisteredAndUsesTheExactSharedTarget()
    {
        var testFilePath = GetTestFilePath();
        var sourceRoot = Path.GetFullPath(Path.Combine(
            Path.GetDirectoryName(testFilePath)!, "..", ".."));
        var patchPath = Path.Combine(
            sourceRoot, "DellarteDellaGuerra.Integration", "PrivateWars", "Patches",
            "KingdomCreateArmyPatch.cs");

        Assert.True(File.Exists(patchPath), $"Missing patch: {patchPath}");

        var patchSource = File.ReadAllText(patchPath);
        var registrationSource = File.ReadAllText(Path.Combine(
            sourceRoot, "DellarteDellaGuerra.Integration", "DI", "DadgServiceContainer.cs"));

        Assert.Contains("KingdomCreateArmyPatchTarget.Resolve()", patchSource, StringComparison.Ordinal);
        Assert.Contains("PatchType.Prefix", patchSource, StringComparison.Ordinal);
        Assert.Contains("ref MBReadOnlyList<MobileParty> partiesToCallToArmy", patchSource, StringComparison.Ordinal);
        Assert.DoesNotContain("CreatePlan(", patchSource, StringComparison.Ordinal);
        Assert.Contains(
            "services.AddSingleton<IPatch, KingdomCreateArmyPatch>();",
            registrationSource,
            StringComparison.Ordinal);
    }

    [Fact]
    public void CampaignBehavior_DoesNotWriteSharedArmyMemberCacheOrInspectCompletedScores()
    {
        var testFilePath = GetTestFilePath();
        var sourcePath = Path.GetFullPath(Path.Combine(
            Path.GetDirectoryName(testFilePath)!,
            "..", "..", "DellarteDellaGuerra", "PrivateWars", "Api", "Campaign",
            "PrivateWarCampaignBehavior.cs"));
        var source = File.ReadAllText(sourcePath);

        Assert.DoesNotContain("SetArmyMembers", source, StringComparison.Ordinal);
        Assert.DoesNotContain("AIBehaviorScores", source, StringComparison.Ordinal);
        Assert.DoesNotContain("SelectPrivateMembers", source, StringComparison.Ordinal);
    }

    private static string GetTestFilePath([CallerFilePath] string path = "") => path;
}
