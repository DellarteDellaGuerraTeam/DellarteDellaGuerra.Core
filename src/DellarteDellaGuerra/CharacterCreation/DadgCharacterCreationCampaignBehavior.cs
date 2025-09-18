using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace DellarteDellaGuerra.CharacterCreation
{
    public class DadgCharacterCreationCampaignBehavior : CharacterCreationCampaignBehavior
    {
        public override void RegisterEvents()
        {
        }

        public override void SyncData(IDataStore dataStore)
        {
        }

        public void InitializeContent(CharacterCreationManager characterCreationManager)
        {
            characterCreationManager.CharacterCreationContent.ChangeReviewPageDescription(
                new TextObject(
                    "{=W6pKpEoT}You prepare to set off for a grand adventure in England! Here is your character. Continue if you are ready, or go back to make changes."));
        }

        public void AfterInitializeContent(CharacterCreationManager characterCreationManager)
        {
        }

        public void OnStageCompleted(CharacterCreationStageBase stage)
        {
        }

        public void OnCharacterCreationFinalize(CharacterCreationManager characterCreationManager)
        {
            MobileParty.MainParty.Position = new CampaignVec2(new Vec2(750f, 300f), true);
            GameState? gameState = GameStateManager.Current?.ActiveState;
            if (gameState is MapState mapState)
            {
                mapState.Handler.ResetCamera(true, true);
                mapState.Handler.TeleportCameraToMainParty();
            }
        }
    }
}