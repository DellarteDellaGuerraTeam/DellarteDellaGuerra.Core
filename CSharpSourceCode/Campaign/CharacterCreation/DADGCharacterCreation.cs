using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace DADG_Core.Campaign.CharacterCreation
{
    public class DADGCharacterCreation : SandboxCharacterCreationContent
    {
        
        


        public override void OnCharacterCreationFinalized()
        {
            CultureObject culture = CharacterObject.PlayerCharacter.Culture;
            Vec2 position2D = default(Vec2);

            switch (culture.StringId)
            {
                case "vlandia":
                    position2D = new Vec2(750f, 300f);
                    break;
                default:
                    position2D = new Vec2(750f, 300f);
                    break;
            }
            MobileParty.MainParty.Position2D = position2D;
            MapState mapState;
            if ((mapState = (GameStateManager.Current.ActiveState as MapState)) != null)
            {
                mapState.Handler.ResetCamera(true, true);
                mapState.Handler.TeleportCameraToMainParty();
            }
        }

        public override TextObject ReviewPageDescription => new TextObject("{=W6pKpEoT}You prepare to set off for a grand adventure in England! Here is your character. Continue if you are ready, or go back to make changes.");
        
        
    }
}