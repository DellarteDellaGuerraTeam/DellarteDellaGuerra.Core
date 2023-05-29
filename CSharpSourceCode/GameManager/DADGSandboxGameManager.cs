using DADG_Core.DADGCampaign.CharacterCreation;
using SandBox;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.Core;

namespace DADG_Core.GameManager
{
    public  class DADGSandboxGameManager : SandBoxGameManager
    {
        public override void OnLoadFinished()
        {
            LaunchCharacterCreation();
            IsLoaded = true;
        }

        private void LaunchCharacterCreation()
        {
            CharacterCreationState gameState = Game.Current.GameStateManager.CreateState<CharacterCreationState>(new object[]
            {
                new DADGCharacterCreation()
            });

            Game.Current.GameStateManager.CleanAndPushState(gameState, 0);
        }
    }
}