using SandBox;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.Core;

namespace DellarteDellaGuerra.CharacterCreation
{
    public  class DadgGameManager : SandBoxGameManager
    {
        public override void OnLoadFinished()
        {
            LaunchCharacterCreation();
            IsLoaded = true;
        }

        private void LaunchCharacterCreation()
        {
            var gameState = Game.Current.GameStateManager?.CreateState<CharacterCreationState>(new DadgCharacterCreation());
            Game.Current.GameStateManager?.CleanAndPushState(gameState);
        }
    }
}