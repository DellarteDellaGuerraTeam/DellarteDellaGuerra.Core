using System.Runtime.CompilerServices;

namespace DellarteDellaGuerra.Tests.PrivateWars;

public class PrivateWarEncounterMenuRegistrationTests
{
    [Fact]
    public void ThreeSimpleMenus_UsePrivateWarOnlyRegistrationsWithVanillaPublicConsequences()
    {
        var sourceRoot = GetSourceRoot();
        var registrarPath = Path.Combine(
            sourceRoot,
            "DellarteDellaGuerra.Integration",
            "PrivateWars",
            "PrivateWarEncounterMenuOptions.cs");

        Assert.True(File.Exists(registrarPath), $"Missing registered menu options: {registrarPath}");
        var source = File.ReadAllText(registrarPath);

        Assert.Contains("AddGameMenuOption(\"town_outside\", \"dadg_private_war_besiege\"", source, StringComparison.Ordinal);
        Assert.Contains("AddGameMenuOption(\"castle_outside\", \"dadg_private_war_besiege\"", source, StringComparison.Ordinal);
        Assert.Contains("AddGameMenuOption(\"encounter\", \"dadg_private_war_continue_preparations\"", source, StringComparison.Ordinal);
        Assert.Contains("AddGameMenuOption(\"army_encounter\", \"dadg_private_war_attack_army\"", source, StringComparison.Ordinal);

        Assert.Contains("Campaign.Current.SiegeEventManager.StartSiegeEvent(currentSettlement, MobileParty.MainParty);", source, StringComparison.Ordinal);
        Assert.Contains("PlayerSiege.StartPlayerSiege(BattleSideEnum.Attacker);", source, StringComparison.Ordinal);
        Assert.Contains("PlayerSiege.StartSiegePreparation();", source, StringComparison.Ordinal);
        Assert.Contains("GameMenu.SwitchToMenu(\"encounter\");", source, StringComparison.Ordinal);
        Assert.Contains("MenuHelper.CheckEnemyAttackableHonorably(args);", source, StringComparison.Ordinal);
        Assert.Contains("str_enemy_not_attackable_tooltip", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Startup_RemovesThreeMenuPostfixesAndRetainsDeferredVillagePatches()
    {
        var sourceRoot = GetSourceRoot();
        var subModuleSource = File.ReadAllText(Path.Combine(
            sourceRoot,
            "DellarteDellaGuerra.Integration",
            "SubModule.cs"));

        Assert.Contains("PrivateWarEncounterMenuOptions.Register(campaignGameStarter);", subModuleSource, StringComparison.Ordinal);
        Assert.DoesNotContain("new BesiegeMenuConditionPatch()", subModuleSource, StringComparison.Ordinal);
        Assert.DoesNotContain("new ContinueSiegeMenuConditionPatch()", subModuleSource, StringComparison.Ordinal);
        Assert.DoesNotContain("new ArmyAttackMenuConditionPatch()", subModuleSource, StringComparison.Ordinal);
        Assert.Contains("new VillageHostileActionConditionPatch()", subModuleSource, StringComparison.Ordinal);
        Assert.Contains("new VillageRaidConditionPatch()", subModuleSource, StringComparison.Ordinal);
    }

    private static string GetSourceRoot([CallerFilePath] string testPath = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(testPath)!, "..", ".."));
}
