using System.Collections.Generic;
using DellarteDellaGuerra.Domain.Church;
using DellarteDellaGuerra.Domain.Church.Port;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;

namespace DellarteDellaGuerra.Church
{
    public class ChurchSettlements
    {
        private readonly Dictionary<string, ChurchSettlementKind> _kindsBySettlementId = new();
        private readonly HashSet<string> _shrineSettlementIds = new();

        public ChurchSettlements(IChurchSettlementsProvider churchSettlementsProvider)
        {
            foreach (var churchSettlement in churchSettlementsProvider.GetChurchSettlements())
            {
                _kindsBySettlementId[churchSettlement.SettlementId] = churchSettlement.Kind;
                if (churchSettlement.IsShrine) _shrineSettlementIds.Add(churchSettlement.SettlementId);
            }
        }

        public bool IsChurchSettlement(Settlement settlement) =>
            _kindsBySettlementId.ContainsKey(settlement.StringId);

        public bool IsShrine(Settlement settlement) =>
            _shrineSettlementIds.Contains(settlement.StringId);

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

        public TextObject GetClergyTitle(Settlement settlement, Hero clergy)
        {
            _kindsBySettlementId.TryGetValue(settlement.StringId, out var kind);
            if (kind == ChurchSettlementKind.Priory)
                return clergy.IsFemale
                    ? new TextObject("{=xM6kQp2R}Prioress")
                    : new TextObject("{=fW2qLp8D}Prior");
            if (kind == ChurchSettlementKind.Cathedral) return new TextObject("{=nQ4wRb8T}Bishop");
            return clergy.IsFemale
                ? new TextObject("{=bN3vHs7K}Abbess")
                : new TextObject("{=uK9dTe5S}Abbot");
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
