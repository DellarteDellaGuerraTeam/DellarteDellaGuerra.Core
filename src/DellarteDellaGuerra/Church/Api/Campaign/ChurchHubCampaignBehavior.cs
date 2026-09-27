using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace DellarteDellaGuerra.Church.Api.Campaign
{
    public class ChurchHubCampaignBehavior : CampaignBehaviorBase
    {
        private readonly ChurchSettlements _churchSettlements;

        public ChurchHubCampaignBehavior(ChurchSettlements churchSettlements)
        {
            _churchSettlements = churchSettlements;
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        public override void SyncData(IDataStore dataStore)
        {
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            starter.AddGameMenu(
                ChurchMenuIds.Hub,
                "{=hN4qVz8C}You enter the {CHURCH_TYPE} at {SETTLEMENT_NAME}.",
                InitializeHub,
                GameMenu.MenuOverlayType.SettlementWithBoth);
            starter.AddGameMenuOption(
                ChurchMenuIds.Village,
                "dadg_church_visit",
                "{=mT7kWp3R}Visit the {CHURCH_TYPE}",
                CanVisitChurch,
                VisitChurch);
            starter.AddGameMenuOption(
                ChurchMenuIds.Hub,
                "dadg_church_speak_to_clergy",
                "{=bF6rQm9S}Speak with {CLERGY_TITLE} {CLERGY_NAME}",
                CanSpeakWithClergy,
                SpeakWithClergy);
            starter.AddGameMenuOption(
                ChurchMenuIds.Hub,
                "dadg_church_hub_return",
                "{=xP5dJs2L}Return to the village",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                    return true;
                },
                _ => GameMenu.SwitchToMenu(ChurchMenuIds.Village),
                isLeave: true);
        }

        private bool CanVisitChurch(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Submenu;

            if (!TryGetCurrentChurchSettlement(out var settlement)) return false;
            SetChurchTextVariables(settlement);
            return true;
        }

        private void VisitChurch(MenuCallbackArgs args)
        {
            if (!TryGetCurrentChurchSettlement(out var settlement)) return;

            SetChurchTextVariables(settlement);
            GameMenu.SwitchToMenu(ChurchMenuIds.Hub);
        }

        private void InitializeHub(MenuCallbackArgs args)
        {
            if (!TryGetCurrentChurchSettlement(out var settlement))
            {
                GameMenu.SwitchToMenu(ChurchMenuIds.Village);
                return;
            }

            SetChurchTextVariables(settlement);
        }

        private bool CanSpeakWithClergy(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Conversation;
            if (!TryGetCurrentChurchSettlement(out var settlement)) return false;

            var clergy = FindClergy(settlement);
            if (clergy == null) return false;

            MBTextManager.SetTextVariable("CLERGY_TITLE", _churchSettlements.GetClergyTitle(settlement, clergy));
            MBTextManager.SetTextVariable("CLERGY_NAME", clergy.Name);
            return true;
        }

        private void SpeakWithClergy(MenuCallbackArgs args)
        {
            if (!TryGetCurrentChurchSettlement(out var settlement)) return;

            var clergy = FindClergy(settlement);
            if (clergy == null) return;

            CampaignMapConversation.OpenConversation(
                new ConversationCharacterData(
                    CharacterObject.PlayerCharacter,
                    null,
                    isCivilianEquipmentRequiredForLeader: true),
                new ConversationCharacterData(
                    clergy.CharacterObject,
                    null,
                    isCivilianEquipmentRequiredForLeader: true));
        }

        private bool TryGetCurrentChurchSettlement(out Settlement settlement)
        {
            settlement = Settlement.CurrentSettlement;
            return settlement != null && _churchSettlements.IsChurchSettlement(settlement);
        }

        private void SetChurchTextVariables(Settlement settlement)
        {
            MBTextManager.SetTextVariable("CHURCH_TYPE", _churchSettlements.GetChurchType(settlement));
            MBTextManager.SetTextVariable("SETTLEMENT_NAME", settlement.Name);
        }

        private static Hero? FindClergy(Settlement settlement) =>
            settlement.Notables.FirstOrDefault(notable => notable.IsPreacher && notable.IsAlive);
    }
}
