using TaleWorlds.CampaignSystem.GameComponents;

namespace DellarteDellaGuerra.Integration.CampaignTime
{
    public class DadgCampaignTimeModel : DefaultCampaignTimeModel
    {
        public override TaleWorlds.CampaignSystem.CampaignTime CampaignStartTime =>
            TaleWorlds.CampaignSystem.CampaignTime.Years(1471) +
            TaleWorlds.CampaignSystem.CampaignTime.Weeks(TaleWorlds.CampaignSystem.CampaignTime.WeeksInSeason) +
            TaleWorlds.CampaignSystem.CampaignTime.Hours(9f);
    }
}
