using TaleWorlds.CampaignSystem.GameComponents;

namespace DellarteDellaGuerra.Integration.CampaignTime
{
    public class DadgCampaignTimeModel : DefaultCampaignTimeModel
    {
        // 14 March 1471, Edward IV lands at Ravenspur: 13 of spring's 92 days (1 Mar - 31 May) into the
        // in-game spring, which is Spring 4 in a normal-speed campaign.
        public override TaleWorlds.CampaignSystem.CampaignTime CampaignStartTime =>
            TaleWorlds.CampaignSystem.CampaignTime.Years(1471) +
            TaleWorlds.CampaignSystem.CampaignTime.Days(TaleWorlds.CampaignSystem.CampaignTime.DaysInSeason * 13f / 92f) +
            TaleWorlds.CampaignSystem.CampaignTime.Hours(9f);
    }
}
