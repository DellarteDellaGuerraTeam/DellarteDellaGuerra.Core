using System.Reflection;
using Harmony.DependencyInjection.Patches;
using HarmonyLib;
using TaleWorlds.CampaignSystem.ViewModelCollection.CharacterCreation;

namespace DellarteDellaGuerra.Infrastructure.CharacterCreation.Patches;

public class DisableSortingBehaviourInCultureMenuPatch : IPatch
{
    public MethodInfo? TargetMethod =>
        AccessTools.Method(typeof(CharacterCreationCultureStageVM), "SortCultureList");

    public MethodInfo? PatchMethod =>
        AccessTools.Method(typeof(DisableSortingBehaviourInCultureMenuPatch), nameof(Prefix));

    public PatchType PatchType => PatchType.Prefix;

    private static bool Prefix() => false;
}
