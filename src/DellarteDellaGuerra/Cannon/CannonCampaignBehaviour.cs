using TaleWorlds.CampaignSystem;

namespace DellarteDellaGuerra.Cannon
{
    public class CannonCampaignBehaviour : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyEvent);
        }

        public override void SyncData(IDataStore dataStore)
        {
        }

        private void OnHourlyEvent()
        {
            var a = "";
        }
    }
}