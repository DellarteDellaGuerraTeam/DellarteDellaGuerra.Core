using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.Levy;
using DellarteDellaGuerra.Domain.Levy.Model;
using DellarteDellaGuerra.Domain.Levy.Port;
using DellarteDellaGuerra.Levy.Spi;
using DellarteDellaGuerra.Titles.Api;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;

namespace DellarteDellaGuerra.Levy.Api
{
    // Feudal levy system: when a lord creates an army the de jure vassals within the same
    // kingdom are expected to muster. Joining the army marks the levy as Answered; failing
    // to join within 3 days marks it as Refused and applies relation penalties.
    //
    // Relation effects:
    //   Answered: +2 relation between lord and vassal leader
    //   Refused:  -5 relation both ways between lord and vassal leader
    public class LevyCampaignBehavior : CampaignBehaviorBase
    {
        private const float LevyExpiryDays = 3f;
        private const int RelationBonusOnAnswer = 2;
        private const int RelationPenaltyOnRefuse = 5;

        private readonly IIssueLevyUseCase _issueLevyUseCase;
        private readonly IExpireLeviesUseCase _expireLeviesUseCase;
        private readonly ILevyRepository _levyRepository;

        private List<string> _serialisedLevies = new();

        public LevyCampaignBehavior(
            IIssueLevyUseCase issueLevyUseCase,
            IExpireLeviesUseCase expireLeviesUseCase,
            ILevyRepository levyRepository)
        {
            _issueLevyUseCase = issueLevyUseCase;
            _expireLeviesUseCase = expireLeviesUseCase;
            _levyRepository = levyRepository;
        }

        public override void RegisterEvents()
        {
            CampaignEvents.ArmyCreated.AddNonSerializedListener(this, OnArmyCreated);
            CampaignEvents.OnPartyJoinedArmyEvent.AddNonSerializedListener(this, OnPartyJoinedArmy);
            CampaignEvents.ArmyDispersed.AddNonSerializedListener(this, OnArmyDispersed);
            CampaignEvents.DailyTickClanEvent.AddNonSerializedListener(this, OnDailyTickClan);
        }

        public override void SyncData(IDataStore dataStore)
        {
            if (dataStore.IsSaving)
                _serialisedLevies = LevyStateSerialiser.Serialise(_levyRepository.Snapshot());

            dataStore.SyncData("DadgLevyCalls", ref _serialisedLevies);
            _serialisedLevies ??= new List<string>();

            if (!dataStore.IsSaving)
                _levyRepository.Initialise(LevyStateSerialiser.Deserialise(_serialisedLevies));
        }

        private void OnArmyCreated(Army army)
        {
            var leaderClan = army.LeaderParty?.ActualClan;
            if (leaderClan is null || leaderClan.Kingdom is null) return;
            if (!FeudalServices.IsInitialised) return;

            var vassalClanIds = CollectDeJureVassalClanIds(leaderClan);
            if (vassalClanIds.Count == 0) return;

            float currentDay = (float)CampaignTime.Now.ToDays;
            _issueLevyUseCase.Execute(leaderClan.StringId, vassalClanIds, currentDay);
        }

        private void OnPartyJoinedArmy(MobileParty party)
        {
            var vassalClan = party.ActualClan;
            if (vassalClan is null || party.Army is null) return;

            var lordClan = party.Army.LeaderParty?.ActualClan;
            if (lordClan is null) return;

            var pending = _levyRepository.GetPendingLeviesFor(vassalClan.StringId)
                .Where(c => c.IssuingClanId == lordClan.StringId)
                .ToList();

            foreach (var call in pending)
            {
                _levyRepository.SaveLevyCall(call with { Status = LevyStatus.Answered });
                ApplyAnswerRelations(lordClan, vassalClan);
            }
        }

        private void OnArmyDispersed(Army army, Army.ArmyDispersionReason reason, bool isPlayersArmy)
        {
            var leaderClan = army.LeaderParty?.ActualClan;
            if (leaderClan is null) return;

            foreach (var call in _levyRepository.GetLeviesIssuedBy(leaderClan.StringId))
                _levyRepository.RemoveLevyCall(call.Id);
        }

        private void OnDailyTickClan(Clan clan)
        {
            // Only lord clans run the expiry check (one check is enough per day rather than per vassal)
            if (clan.Kingdom is null || clan.IsEliminated) return;

            float currentDay = (float)CampaignTime.Now.ToDays;
            var refused = _expireLeviesUseCase.Execute(currentDay, LevyExpiryDays);

            foreach (var call in refused)
            {
                if (call.IssuingClanId != clan.StringId) continue;

                var vassalClan = Clan.All.FirstOrDefault(c =>
                    c.StringId == call.VassalClanId && !c.IsEliminated);
                if (vassalClan is null) continue;

                ApplyRefusalRelations(clan, vassalClan);
            }
        }

        private static void ApplyAnswerRelations(Clan lordClan, Clan vassalClan)
        {
            if (lordClan.Leader is not null && vassalClan.Leader is not null)
            {
                ChangeRelationAction.ApplyRelationChangeBetweenHeroes(
                    lordClan.Leader, vassalClan.Leader, RelationBonusOnAnswer, false);
            }
        }

        private static void ApplyRefusalRelations(Clan lordClan, Clan vassalClan)
        {
            if (lordClan.Leader is not null && vassalClan.Leader is not null)
            {
                ChangeRelationAction.ApplyRelationChangeBetweenHeroes(
                    lordClan.Leader, vassalClan.Leader, -RelationPenaltyOnRefuse, false);
                ChangeRelationAction.ApplyRelationChangeBetweenHeroes(
                    vassalClan.Leader, lordClan.Leader, -RelationPenaltyOnRefuse, false);
            }
        }

        private static IReadOnlyList<string> CollectDeJureVassalClanIds(Clan lordClan)
        {
            if (!FeudalServices.IsInitialised) return System.Array.Empty<string>();

            var structure = FeudalServices.Structure!;
            var titles = FeudalServices.Titles!;

            var lordTitles = titles.GetTitlesByClan(lordClan.StringId);
            if (lordTitles.Count == 0) return System.Array.Empty<string>();

            var vassalClanIds = new System.Collections.Generic.HashSet<string>();
            foreach (var lordTitle in lordTitles)
            {
                foreach (string vassalTitleId in structure.GetDeJureVassalTitleIds(lordTitle.Id))
                {
                    var vassalTitle = titles.GetTitle(vassalTitleId);
                    if (vassalTitle?.HolderClanId is not null
                        && vassalTitle.HolderClanId != lordClan.StringId)
                    {
                        var vassalClan = Clan.All.FirstOrDefault(c =>
                            c.StringId == vassalTitle.HolderClanId
                            && !c.IsEliminated
                            && c.Kingdom == lordClan.Kingdom);
                        if (vassalClan is not null)
                            vassalClanIds.Add(vassalClan.StringId);
                    }
                }
            }

            return vassalClanIds.ToList();
        }
    }
}
