using System;
using System.Linq;
using SandBox;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace DellarteDellaGuerra.MainMenu
{
    public class DadgCampaignStartButtonAdder
    {
        public void AddDadgCampaignStartButton(Module module)
        {
            InitialStateOption initialStateOption = new InitialStateOption("DADGNewGame",
                new TextObject("Start Dell'arte della Guerra"), 3,
                OnClick, IsDisabledAndReason);

            if (!module.GetInitialStateOptions().Any(option => option.Id.Equals("DADGNewGame")))
                module.AddInitialStateOption(initialStateOption);
        }

        private static void OnClick()
        {
            MBGameManager gameManager = new SandBoxGameManager(() => new Campaign(CampaignGameMode.Campaign));
            MBGameManager.StartNewGame(gameManager);
        }

        private static (bool, TextObject) IsDisabledAndReason()
        {
            TextObject coreContentDisabledReason = new TextObject("{=V8BXjyYq}Disabled during installation.");
            return new ValueTuple<bool, TextObject>(Module.CurrentModule.IsOnlyCoreContentEnabled,
                coreContentDisabledReason);
        }
    }
}