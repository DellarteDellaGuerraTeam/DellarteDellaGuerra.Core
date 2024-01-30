using DellarteDellaGuerra.DADGCampaign.CharacterCreation;
using SandBox;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.Core;

namespace DellarteDellaGuerra.GameManager
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
            var gameState = Game.Current.GameStateManager?.CreateState<CharacterCreationState>(new DADGCharacterCreation());
            Game.Current.GameStateManager?.CleanAndPushState(gameState);
        }
    }
}