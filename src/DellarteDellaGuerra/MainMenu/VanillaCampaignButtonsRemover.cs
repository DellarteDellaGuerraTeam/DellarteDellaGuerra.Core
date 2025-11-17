using System.Linq;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.MainMenu
{
    public class VanillaCampaignButtonsRemover
    {
        public void RemoveVanillaCampaignOptions(Module module)
        {
            var options = module.GetInitialStateOptions().Where(option => !option.Id.Equals("StoryModeNewGame") && !option.Id.Equals("SandBoxNewGame")).ToList();
            module.ClearStateOptions();
            options.ForEach(option => module.AddInitialStateOption(option));
        }
    }
}