using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;

namespace DellarteDellaGuerra.Church
{
    public static class ChurchSettlements
    {
        private const string AbbeySuffix = "_Abbey";
        private const string PriorySuffix = "_Priory";
        private const string CathedralSuffix = "_Cathedral";

        public static bool IsChurchSettlement(Settlement settlement) =>
            settlement.IsVillage &&
            (settlement.StringId.EndsWith(AbbeySuffix) ||
             settlement.StringId.EndsWith(PriorySuffix) ||
             settlement.StringId.EndsWith(CathedralSuffix));

        public static TextObject GetClergyTitle(Settlement settlement)
        {
            if (settlement.StringId.EndsWith(PriorySuffix)) return new TextObject("{=fW2qLp8D}Prior");
            if (settlement.StringId.EndsWith(CathedralSuffix)) return new TextObject("{=hN6cRw3B}Dean");
            return new TextObject("{=uK9dTe5S}Abbot");
        }

        public static TextObject GetChurchType(Settlement settlement)
        {
            if (settlement.StringId.EndsWith(PriorySuffix)) return new TextObject("{=cV4bXm7J}priory");
            if (settlement.StringId.EndsWith(CathedralSuffix)) return new TextObject("{=pG8sZn2M}cathedral");
            return new TextObject("{=wQ3jYf6H}abbey");
        }
    }
}
