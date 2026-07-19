using System.Collections.Generic;
using DellarteDellaGuerra.Domain.Church;
using DellarteDellaGuerra.Domain.Church.Port;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;

namespace DellarteDellaGuerra.Church
{
    public class ChurchSettlements
    {
        private readonly Dictionary<string, ChurchSettlementKind> _kindsBySettlementId = new();

        public ChurchSettlements(IChurchSettlementsProvider churchSettlementsProvider)
        {
            foreach (var churchSettlement in churchSettlementsProvider.GetChurchSettlements())
                _kindsBySettlementId[churchSettlement.SettlementId] = churchSettlement.Kind;
        }

        public bool IsChurchSettlement(Settlement settlement) =>
            _kindsBySettlementId.ContainsKey(settlement.StringId);

        public bool IsCathedral(Settlement settlement) =>
            _kindsBySettlementId.TryGetValue(settlement.StringId, out var kind) &&
            kind == ChurchSettlementKind.Cathedral;

        public TextObject GetClergyTitle(Settlement settlement)
        {
            _kindsBySettlementId.TryGetValue(settlement.StringId, out var kind);
            if (kind == ChurchSettlementKind.Priory) return new TextObject("{=fW2qLp8D}Prior");
            if (kind == ChurchSettlementKind.Cathedral) return new TextObject("{=nQ4wRb8T}Bishop");
            return new TextObject("{=uK9dTe5S}Abbot");
        }

        public TextObject GetChurchType(Settlement settlement)
        {
            _kindsBySettlementId.TryGetValue(settlement.StringId, out var kind);
            if (kind == ChurchSettlementKind.Priory) return new TextObject("{=cV4bXm7J}priory");
            if (kind == ChurchSettlementKind.Cathedral) return new TextObject("{=pG8sZn2M}cathedral");
            return new TextObject("{=wQ3jYf6H}abbey");
        }
    }
}
