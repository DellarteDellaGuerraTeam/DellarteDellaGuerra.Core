using HarmonyLib;
using TaleWorlds.CampaignSystem.ViewModelCollection.CharacterCreation;

namespace DellarteDellaGuerra.CharacterCreation.Patches
{
    [HarmonyPatch(typeof(CharacterCreationCultureStageVM), "SortCultureList")]
    public class DisableSortingBehaviourInCultureMenuPatch
    {
        private static bool Prefix()
        {
            return false;
        }
    }
}