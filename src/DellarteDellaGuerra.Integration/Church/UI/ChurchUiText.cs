using DellarteDellaGuerra.Domain.Church.Ledger;
using TaleWorlds.Localization;

namespace DellarteDellaGuerra.Integration.Church.UI
{
    internal static class ChurchUiText
    {
        public static TextObject FormatClergyOffice(ClergyOffice office)
        {
            switch (office)
            {
                case ClergyOffice.Bishop:
                    return new TextObject("{=nQ4wRb8T}Bishop");
                case ClergyOffice.Abbess:
                    return new TextObject("{=bN3vHs7K}Abbess");
                case ClergyOffice.Prior:
                    return new TextObject("{=fW2qLp8D}Prior");
                case ClergyOffice.Prioress:
                    return new TextObject("{=xM6kQp2R}Prioress");
                default:
                    return new TextObject("{=uK9dTe5S}Abbot");
            }
        }
    }
}
