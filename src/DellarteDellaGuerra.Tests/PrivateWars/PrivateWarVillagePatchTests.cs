using System.Runtime.CompilerServices;
using DellarteDellaGuerra.PrivateWars.Api.Patches;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.GameMenus;

namespace DellarteDellaGuerra.Tests.PrivateWars;

public class PrivateWarVillagePatchTests
{
    [Fact]
    public void Targets_ResolveExactPrivateConditionMethods()
    {
        var hostileAction = PrivateWarHarmonyPatchTargets.VillageHostileActionCondition();
        var raid = PrivateWarHarmonyPatchTargets.VillageRaidCondition();

        AssertTarget(hostileAction, "game_menu_village_hostile_action_on_condition", isStatic: true);
        AssertTarget(raid, "game_menu_village_hostile_action_raid_village_on_condition", isStatic: false);
    }

    [Theory]
    [InlineData("VillageHostileActionConditionPatch.cs")]
    [InlineData("VillageRaidConditionPatch.cs")]
    public void Postfixes_OnlyPromoteVanillaFalseForTheOwnerPairAndPreserveNavalState(string fileName)
    {
        var source = ReadPatchSource(fileName);

        Assert.Contains("if (__result) return;", source, StringComparison.Ordinal);
        Assert.Contains("PrivateWarPatchHelper.AreEnemies", source, StringComparison.Ordinal);
        Assert.Contains("OwnerClan", source, StringComparison.Ordinal);
        Assert.DoesNotContain("args.IsEnabled = true", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IsCurrentlyAtSea", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ShipHelper", source, StringComparison.Ordinal);
        Assert.DoesNotContain("port", source, StringComparison.OrdinalIgnoreCase);
    }

    private static void AssertTarget(System.Reflection.MethodInfo target, string name, bool isStatic)
    {
        Assert.Equal(typeof(VillageHostileActionCampaignBehavior), target.DeclaringType);
        Assert.Equal(name, target.Name);
        Assert.True(target.IsPrivate);
        Assert.Equal(isStatic, target.IsStatic);
        Assert.Equal(typeof(bool), target.ReturnType);
        Assert.Equal([typeof(MenuCallbackArgs)], target.GetParameters().Select(parameter => parameter.ParameterType));
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
