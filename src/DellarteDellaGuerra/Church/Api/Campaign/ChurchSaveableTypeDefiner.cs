using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.SaveSystem;

namespace DellarteDellaGuerra.Church.Api.Campaign
{
    public class ChurchSaveableTypeDefiner : SaveableTypeDefiner
    {
        public ChurchSaveableTypeDefiner()
            : base(674592360)
        {
        }

        protected override void DefineContainerDefinitions()
        {
            ConstructContainerDefinition(typeof(Dictionary<Hero, Settlement>));
        }
    }
}
