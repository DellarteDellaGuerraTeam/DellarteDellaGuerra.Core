using DellarteDellaGuerra.PrivateWars.Api.GameModels;
using DellarteDellaGuerra.Domain.PrivateWars;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Siege;

namespace DellarteDellaGuerra.Tests.PrivateWars;

public class PrivateWarModelSeamTests
{
    [Fact]
    public void EncounterModel_OverridesNonAttachedNpcReinforcementEnumeration()
    {
        var method = typeof(DadgEncounterModel).GetMethod(
            nameof(DadgEncounterModel.FindNonAttachedNpcPartiesWhoWillJoinPlayerEncounter),
            [typeof(List<MobileParty>), typeof(List<MobileParty>)]);

        Assert.NotNull(method);
        Assert.Equal(typeof(DadgEncounterModel), method!.DeclaringType);
    }

    [Fact]
    public void EncounterModel_ReinforcementOverrideUsesVanillaPlayerSiegeRadiusInputs()
    {
        var method = typeof(DadgEncounterModel).GetMethod(
            nameof(DadgEncounterModel.FindNonAttachedNpcPartiesWhoWillJoinPlayerEncounter),
            [typeof(List<MobileParty>), typeof(List<MobileParty>)]);
        var referencedMembers = ResolveReferencedMembers(method!).ToArray();

        Assert.Contains(referencedMembers, member =>
            member.DeclaringType == typeof(PlayerSiege) && member.Name == "get_PlayerSiegeEvent");
        Assert.Contains(referencedMembers, member =>
            member.DeclaringType == typeof(MobilePartyAIModel) &&
            member.Name == "get_SettlementDefendingWaitingPositionRadius");
        Assert.Contains(referencedMembers, member =>
            member.DeclaringType == typeof(PrivateWarInteractionPolicy) &&
            member.Name == nameof(PrivateWarInteractionPolicy.ResolveEncounterJoiningRadius));
    }

    [Fact]
    public void VolunteerModel_OverridesRecruitableIndex()
    {
        var method = typeof(DadgVolunteerModel).GetMethod(
            nameof(DadgVolunteerModel.MaximumIndexHeroCanRecruitFromHero),
            [typeof(Hero), typeof(Hero), typeof(int)]);

        Assert.NotNull(method);
        Assert.Equal(typeof(DadgVolunteerModel), method!.DeclaringType);
    }

    private static IEnumerable<MemberInfo> ResolveReferencedMembers(MethodInfo method)
    {
        var il = method.GetMethodBody()!.GetILAsByteArray()!;
        for (var index = 0; index <= il.Length - sizeof(int); index++)
        {
            var token = BitConverter.ToInt32(il, index);
            MemberInfo? member;
            try
            {
                member = method.Module.ResolveMember(token);
            }
            catch (Exception)
            {
                continue;
            }

            if (member != null) yield return member;
        }
    }
}
