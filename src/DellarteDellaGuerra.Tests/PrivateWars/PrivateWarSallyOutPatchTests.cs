using System.Runtime.CompilerServices;
using DellarteDellaGuerra.PrivateWars.Api.Patches;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Settlements;

namespace DellarteDellaGuerra.Tests.PrivateWars;

public class PrivateWarSallyOutPatchTests
{
    [Fact]
    public void TargetResolver_SelectsExactPrivateCheckSallyOutOverload()
    {
        var target = PrivateWarHarmonyPatchTargets.SallyOutCheck();

        Assert.Equal(typeof(SallyOutsCampaignBehavior), target.DeclaringType);
        Assert.Equal("CheckSallyOut", target.Name);
        Assert.True(target.IsPrivate);
        Assert.False(target.IsStatic);
        Assert.Equal(typeof(void), target.ReturnType);
        Assert.Equal(
            [typeof(Settlement), typeof(bool), typeof(bool).MakeByRefType()],
            target.GetParameters().Select(parameter => parameter.ParameterType));
        Assert.True(target.GetParameters()[2].IsOut);
    }

    [Fact]
    public void RuntimeIl_ContainsExactlyOneBesiegerStrengthEnemyPredicate()
    {
        var vanillaPredicate = typeof(IFaction).GetMethod(
            nameof(IFaction.IsAtWarWith),
            [typeof(IFaction)])!;

        Assert.Equal(1, MethodIlCalls.Count(PrivateWarHarmonyPatchTargets.SallyOutCheck(), vanillaPredicate));
    }

    [Fact]
    public void Wrapper_ReplacesOnlyTheEvaluatedPartyOwnerPredicateAndGuardsExactCount()
    {
        var source = ReadPatchSource("SallyOutStrengthPatch.cs");

        Assert.Contains("PrivateWarHarmonyPatchTargets.SallyOutCheck()", source, StringComparison.Ordinal);
        Assert.Contains("PatchType.Transpiler", source, StringComparison.Ordinal);
        Assert.Contains("IsEnemyForSallyOutStrength(MobileParty mobileParty, Settlement settlement)", source, StringComparison.Ordinal);
        Assert.Contains("mobileParty.ActualClan", source, StringComparison.Ordinal);
        Assert.Contains("settlement.OwnerClan", source, StringComparison.Ordinal);
        Assert.Contains("replaced != 1", source, StringComparison.Ordinal);
        Assert.Contains("throw new InvalidOperationException", source, StringComparison.Ordinal);

        Assert.DoesNotContain("checkForNavalSallyOut", source, StringComparison.Ordinal);
        Assert.DoesNotContain("PowerCalculationContext", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IsCurrentlyAtSea", source, StringComparison.Ordinal);
        Assert.DoesNotContain("GetCustomStrength", source, StringComparison.Ordinal);
        Assert.DoesNotContain("StartPartyEncounter", source, StringComparison.Ordinal);
        Assert.DoesNotContain("PatchType.Prefix", source, StringComparison.Ordinal);
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
