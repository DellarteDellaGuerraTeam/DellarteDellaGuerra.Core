using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;

namespace DADG_Core
{
    public class SubModule : MBSubModuleBase
    {
        protected override void OnSubModuleLoad()
        {
            CampaignTime startTime = CampaignTime.Years(1471)+CampaignTime.Weeks(2)+CampaignTime.Days(3);
            typeof(CampaignData).GetField("CampaignStartTime",BindingFlags.Static|BindingFlags.Public)?.SetValue(null,startTime);
            
        }

        
    }
}