using System.Collections.Generic;
using DellarteDellaGuerra.PrivateWars.Spi;
using DellarteDellaGuerra.Titles.Api.Campaign;
using TaleWorlds.CampaignSystem;

namespace DellarteDellaGuerra.PrivateWars.Api.Campaign
{
    /**
     * <summary>
     *  Owns the lifecycle of the private-war registry: persists the active and concluded wars
     *  through Bannerlord saves as flat string lists and restores them on load. War declaration,
     *  scoring, and resolution are wired in later phases; this behaviour is persistence only.
     * </summary>
     */
    public class PrivateWarCampaignBehavior : CampaignBehaviorBase
    {
        private readonly IFeudalStateStore _stateStore;

        private List<string> _serialisedWars = new();

        public PrivateWarCampaignBehavior(IFeudalStateStore stateStore)
        {
            _stateStore = stateStore;
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
        }

        public override void SyncData(IDataStore dataStore)
        {
            if (dataStore.IsSaving)
            {
                _serialisedWars = PrivateWarStateSerialiser.SerialiseWars(_stateStore.SnapshotPrivateWars());
            }

            dataStore.SyncData("DadgPrivateWars", ref _serialisedWars);

            _serialisedWars ??= new List<string>();
        }

        private void OnGameLoaded(CampaignGameStarter campaignGameStarter)
        {
            if (_serialisedWars.Count == 0) return;

            _stateStore.InitialisePrivateWars(PrivateWarStateSerialiser.DeserialiseWars(_serialisedWars));
        }
    }
}