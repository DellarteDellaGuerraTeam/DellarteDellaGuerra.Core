using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace DellarteDellaGuerra.DadgCampaign.CharacterCreation
{
    public class DadgCharacterCreation : SandboxCharacterCreationContent
    {
        public override void OnCharacterCreationFinalized()
        {
            MobileParty.MainParty.Position2D = new Vec2(750f, 300f);;
            GameState? gameState = GameStateManager.Current?.ActiveState;
            if (gameState is MapState mapState)
            {
                mapState.Handler.ResetCamera(true, true);
                mapState.Handler.TeleportCameraToMainParty();
            }
        }

        public override TextObject ReviewPageDescription => new TextObject("{=W6pKpEoT}You prepare to set off for a grand adventure in England! Here is your character. Continue if you are ready, or go back to make changes.");
        
        
    }
}