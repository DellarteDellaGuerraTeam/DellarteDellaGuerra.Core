using System.Reflection;
using Harmony.DependencyInjection.Patches;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterCreationContent;

namespace DellarteDellaGuerra.Infrastructure.CharacterCreation.Patches;

/// <summary>
/// War Sails adds "nord" to the culture menu unconditionally; DADG repurposes it as a non-main culture,
/// so only main cultures may be offered.
/// </summary>
public class MainCulturesOnlyInCultureMenuPatch : IPatch
{
    public MethodInfo? TargetMethod =>
        AccessTools.Method(typeof(CharacterCreationContent), nameof(CharacterCreationContent.AddCharacterCreationCulture));

    public MethodInfo? PatchMethod =>
        AccessTools.Method(typeof(MainCulturesOnlyInCultureMenuPatch), nameof(Prefix));

    public PatchType PatchType => PatchType.Prefix;

    private static bool Prefix(CultureObject culture) => culture is { IsMainCulture: true };
}
