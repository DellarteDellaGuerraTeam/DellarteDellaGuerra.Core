using System.Reflection;
using System.Runtime.CompilerServices;
using DellarteDellaGuerra.PrivateWars.Api.Patches;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Conversation.Tags;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;

namespace DellarteDellaGuerra.Tests.PrivateWars;

public class PrivateWarHarmonyTargetInventoryTests
{
    [Fact]
    public void RemainingPrivateWarTargets_ResolveExact146Signatures()
    {
        AssertTarget(PrivateWarHarmonyPatchTargets.MobilePartyAiIsEnemy(), typeof(DefaultMobilePartyAIModel), "IsEnemy", typeof(bool), false, false, typeof(PartyBase), typeof(MobileParty));
        AssertTarget(PrivateWarHarmonyPatchTargets.MobilePartyAiStanceScore(), typeof(DefaultMobilePartyAIModel), "CalculateStanceScore", typeof(float), false, false, typeof(MobileParty), typeof(MobileParty));
        AssertTarget(PrivateWarHarmonyPatchTargets.StartPartyEncounter(), typeof(EncounterManager), nameof(EncounterManager.StartPartyEncounter), typeof(void), true, true, typeof(PartyBase), typeof(PartyBase));
        AssertTarget(PrivateWarHarmonyPatchTargets.StartSettlementEncounter(), typeof(EncounterManager), nameof(EncounterManager.StartSettlementEncounter), typeof(void), true, true, typeof(MobileParty), typeof(Settlement));
        AssertTarget(PrivateWarHarmonyPatchTargets.CanPartyJoinBattle(), typeof(MapEvent), nameof(MapEvent.CanPartyJoinBattle), typeof(bool), true, false, typeof(PartyBase), typeof(BattleSideEnum));
        AssertTarget(PrivateWarHarmonyPatchTargets.PrisonerRelease(), typeof(EndCaptivityAction), "ApplyInternal", typeof(void), false, true, typeof(Hero), typeof(EndCaptivityDetail), typeof(Hero), typeof(bool));
        AssertTarget(PrivateWarHarmonyPatchTargets.PlayerEncounterSetupFields(), typeof(PlayerEncounter), nameof(PlayerEncounter.SetupFields), typeof(void), true, false, typeof(PartyBase), typeof(PartyBase));
        AssertTarget(PrivateWarHarmonyPatchTargets.SiegeDefenderJoin(), typeof(SiegeEvent), nameof(SiegeEvent.CanPartyJoinSide), typeof(bool), true, false, typeof(PartyBase), typeof(BattleSideEnum));
        AssertTarget(PrivateWarHarmonyPatchTargets.PlayerIsEnemyTag(), typeof(PlayerIsEnemyTag), nameof(PlayerIsEnemyTag.IsApplicableTo), typeof(bool), true, false, typeof(CharacterObject));
        AssertTarget(PrivateWarHarmonyPatchTargets.WillLordAttack(), typeof(HeroHelper), nameof(HeroHelper.WillLordAttack), typeof(bool), true, true);
        AssertTarget(PrivateWarHarmonyPatchTargets.PlayerCanAttackRival(), typeof(LordConversationsCampaignBehavior), "conversation_player_can_attack_hero_on_condition", typeof(bool), true, false);
    }

    [Fact]
    public void RetainedWrappers_UseExactSharedTargets()
    {
        var expected = new Dictionary<string, string>
        {
            ["MobilePartyAiIsEnemyPatch.cs"] = "PrivateWarHarmonyPatchTargets.MobilePartyAiIsEnemy()",
            ["MobilePartyAiStanceScorePatch.cs"] = "PrivateWarHarmonyPatchTargets.MobilePartyAiStanceScore()",
            ["StartPartyEncounterBattlePatch.cs"] = "PrivateWarHarmonyPatchTargets.StartPartyEncounter()",
            ["StartSettlementEncounterSiegePatch.cs"] = "PrivateWarHarmonyPatchTargets.StartSettlementEncounter()",
            ["CanPartyJoinBattlePatch.cs"] = "PrivateWarHarmonyPatchTargets.CanPartyJoinBattle()",
            ["PrivateWarPrisonerRetentionPatch.cs"] = "PrivateWarHarmonyPatchTargets.PrisonerRelease()",
            ["PlayerEncounterSetupFieldsPatch.cs"] = "PrivateWarHarmonyPatchTargets.PlayerEncounterSetupFields()",
            ["SiegeDefenderJoinPatch.cs"] = "PrivateWarHarmonyPatchTargets.SiegeDefenderJoin()",
            ["PlayerIsEnemyTagPatch.cs"] = "PrivateWarHarmonyPatchTargets.PlayerIsEnemyTag()",
            ["WillLordAttackPrivateWarPatch.cs"] = "PrivateWarHarmonyPatchTargets.WillLordAttack()",
            ["PlayerCanAttackPrivateWarRivalPatch.cs"] = "PrivateWarHarmonyPatchTargets.PlayerCanAttackRival()",
        };

        foreach (var (fileName, resolverCall) in expected)
            Assert.Contains(resolverCall, ReadPatchSource(fileName), StringComparison.Ordinal);
    }

    [Fact]
    public void PrisonerRetention_BlocksOnlyPairFilteredInvoluntaryReleaseDetails()
    {
        var source = ReadPatchSource("PrivateWarPrisonerRetentionPatch.cs");

        Assert.Contains("PrivateWarPatchHelper.AreEnemies(prisoner.Clan, captorClan)", source, StringComparison.Ordinal);
        Assert.Contains("EndCaptivityDetail.ReleasedAfterPeace", source, StringComparison.Ordinal);
        Assert.Contains("EndCaptivityDetail.ReleasedAfterBattle", source, StringComparison.Ordinal);
        Assert.DoesNotContain("EndCaptivityDetail.Ransom", source, StringComparison.Ordinal);
        Assert.DoesNotContain("EndCaptivityDetail.ReleasedAfterEscape", source, StringComparison.Ordinal);
        Assert.DoesNotContain("EndCaptivityDetail.Death", source, StringComparison.Ordinal);
    }

    [Fact]
    public void SettlementEncounterTranspiler_RequiresExactlyTwoClanAwareGates()
    {
        var vanillaPredicate = typeof(FactionManager).GetMethod(
            nameof(FactionManager.IsAtWarAgainstFaction),
            [typeof(IFaction), typeof(IFaction)])!;
        var source = ReadPatchSource("StartSettlementEncounterSiegePatch.cs");

        Assert.Equal(
            2,
            MethodIlCalls.Count(PrivateWarHarmonyPatchTargets.StartSettlementEncounter(), vanillaPredicate));
        Assert.Contains("replaced != 2", source, StringComparison.Ordinal);
        Assert.Contains("throw new InvalidOperationException", source, StringComparison.Ordinal);
    }

    private static void AssertTarget(
        MethodInfo target,
        Type declaringType,
        string name,
        Type returnType,
        bool isPublic,
        bool isStatic,
        params Type[] parameterTypes)
    {
        Assert.Equal(declaringType, target.DeclaringType);
        Assert.Equal(name, target.Name);
        Assert.Equal(returnType, target.ReturnType);
        Assert.Equal(isPublic, target.IsPublic);
        Assert.Equal(isStatic, target.IsStatic);
        Assert.Equal(parameterTypes, target.GetParameters().Select(parameter => parameter.ParameterType));
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
