using DellarteDellaGuerra.Church;
using DellarteDellaGuerra.Domain.Church.Hierarchy;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.ScreenSystem;

namespace DellarteDellaGuerra.Integration.Church.UI
{
    /// <summary>
    /// Adds the "Survey the Church in England" option to the village menu of church settlements,
    /// opening the standalone church hierarchy screen. Lives in the Integration layer because it
    /// pushes the Gauntlet screen type.
    /// </summary>
    public class ChurchHierarchyMenuBehavior : CampaignBehaviorBase
    {
        private readonly ChurchSettlements _churchSettlements;
        private readonly IBuildChurchMapUseCase _buildChurchMap;

        private bool? _hasDioceses;

        public ChurchHierarchyMenuBehavior(
            ChurchSettlements churchSettlements,
            IBuildChurchMapUseCase buildChurchMap)
        {
            _churchSettlements = churchSettlements;
            _buildChurchMap = buildChurchMap;
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
            starter.AddGameMenuOption(
                "village",
                "dadg_church_survey_hierarchy",
                "{=dW6pFn4Y}Survey the Church in England",
                CanSurveyChurch,
                SurveyChurch);
        }

        private bool CanSurveyChurch(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Submenu;

            var settlement = Settlement.CurrentSettlement;
            if (settlement == null || !_churchSettlements.IsChurchSettlement(settlement)) return false;

            _hasDioceses ??= _buildChurchMap.Execute().Roots.Count > 0;
            return _hasDioceses.Value;
        }

        private void SurveyChurch(MenuCallbackArgs args)
        {
            if (ScreenManager.TopScreen is not ChurchHierarchyScreen)
                ScreenManager.PushScreen(new ChurchHierarchyScreen());
        }
    }
}
