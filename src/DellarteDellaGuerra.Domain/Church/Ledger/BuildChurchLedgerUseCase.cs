using System.Linq;
using DellarteDellaGuerra.Domain.Church.Favour;
using DellarteDellaGuerra.Domain.Church.Hierarchy;
using DellarteDellaGuerra.Domain.Church.Port;

namespace DellarteDellaGuerra.Domain.Church.Ledger
{
    public class BuildChurchLedgerUseCase : IBuildChurchLedgerUseCase
    {
        private readonly IBuildChurchMapUseCase _buildChurchMap;
        private readonly IChurchLedgerWorld _world;

        public BuildChurchLedgerUseCase(
            IBuildChurchMapUseCase buildChurchMap,
            IChurchLedgerWorld world)
        {
            _buildChurchMap = buildChurchMap;
            _world = world;
        }

        public ChurchLedger Execute()
        {
            var roots = _buildChurchMap.Execute().Roots.Select(BuildEntry).ToList();
            var favour = ChurchFavourPolicy.GetProgress(_world.GetLivingClergyRelations());
            return new ChurchLedger(favour, roots);
        }

        private ChurchLedgerEntry BuildEntry(ChurchMapEntry entry)
        {
            var facts = _world.GetFoundation(entry.SettlementId);
            var children = entry.Children.Select(BuildEntry).ToList();
            if (facts is null)
            {
                var fallbackName = entry.Name.Length == 0 ? entry.SettlementId : entry.Name;
                return new ChurchLedgerEntry(
                    entry.Id,
                    fallbackName,
                    entry.SettlementId,
                    fallbackName,
                    entry.Rank,
                    false,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    ChurchFoundationStatus.None,
                    children);
            }

            var status = ChurchFoundationStatus.None;
            if (facts.IsShrine) status |= ChurchFoundationStatus.PilgrimageShrine;
            if (facts.ClergyName is null) status |= ChurchFoundationStatus.Vacant;
            if (facts.IsUnderRaid) status |= ChurchFoundationStatus.UnderRaid;

            return new ChurchLedgerEntry(
                entry.Id,
                entry.Rank == ChurchNodeRank.See ? entry.Name : facts.Name,
                entry.SettlementId,
                facts.Name,
                entry.Rank,
                true,
                facts.ClergyName,
                ResolveClergyOffice(entry.Rank, facts.ClergyName, facts.ClergyIsFemale),
                facts.PlayerRelation,
                facts.ClergyPower,
                facts.OwnerName,
                facts.FactionName,
                status,
                children);
        }

        private static ClergyOffice? ResolveClergyOffice(
            ChurchNodeRank rank,
            string? clergyName,
            bool clergyIsFemale)
        {
            if (clergyName is null) return null;

            switch (rank)
            {
                case ChurchNodeRank.See:
                case ChurchNodeRank.Cathedral:
                    return ClergyOffice.Bishop;
                case ChurchNodeRank.Priory:
                    return clergyIsFemale ? ClergyOffice.Prioress : ClergyOffice.Prior;
                default:
                    return clergyIsFemale ? ClergyOffice.Abbess : ClergyOffice.Abbot;
            }
        }
    }
}
