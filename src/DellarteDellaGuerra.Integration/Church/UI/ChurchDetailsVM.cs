using DellarteDellaGuerra.Domain.Church.Hierarchy;
using DellarteDellaGuerra.Domain.Church.Ledger;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace DellarteDellaGuerra.Integration.Church.UI
{
    /// <summary>
    /// Read-only, live campaign details for the foundation selected in the church ledger.
    /// </summary>
    public class ChurchDetailsVM : ViewModel
    {
        public ChurchDetailsVM(ChurchLedgerEntry entry)
        {
            NameText = entry.SettlementName;
            KindText = BuildKindText(entry);
            ClergyText = BuildClergyText(entry);
            RelationText = BuildRelationText(entry.PlayerRelation);
            PowerText = BuildPowerText(entry.ClergyPower);
            OwnerText = entry.OwnerName ?? UnavailableText;
            FactionText = entry.FactionName ?? UnavailableText;
            StatusText = BuildStatusText(entry);

            KindLabelText = new TextObject("{=vP4mZk8R}Foundation").ToString();
            ClergyLabelText = new TextObject("{=wN6sHt2D}Clergy").ToString();
            RelationLabelText = new TextObject("{=jX3qFb7L}Your relation").ToString();
            PowerLabelText = new TextObject("{=rC8kMv5S}Influence").ToString();
            OwnerLabelText = new TextObject("{=bT2nQp9H}Owner").ToString();
            FactionLabelText = new TextObject("{=sG7wLd4K}Faction").ToString();
            StatusLabelText = new TextObject("{=fY5hRa3V}Status").ToString();
        }

        [DataSourceProperty] public string NameText { get; }
        [DataSourceProperty] public string KindLabelText { get; }
        [DataSourceProperty] public string KindText { get; }
        [DataSourceProperty] public string ClergyLabelText { get; }
        [DataSourceProperty] public string ClergyText { get; }
        [DataSourceProperty] public string RelationLabelText { get; }
        [DataSourceProperty] public string RelationText { get; }
        [DataSourceProperty] public string PowerLabelText { get; }
        [DataSourceProperty] public string PowerText { get; }
        [DataSourceProperty] public string OwnerLabelText { get; }
        [DataSourceProperty] public string OwnerText { get; }
        [DataSourceProperty] public string FactionLabelText { get; }
        [DataSourceProperty] public string FactionText { get; }
        [DataSourceProperty] public string StatusLabelText { get; }
        [DataSourceProperty] public string StatusText { get; }

        private static string UnavailableText => new TextObject("{=Dqv8jV7K}Unavailable").ToString();

        private static string BuildKindText(ChurchLedgerEntry entry)
        {
            switch (entry.Rank)
            {
                case ChurchNodeRank.See:
                {
                    var text = new TextObject("{=eB7nXq3F}Cathedral seat of {DIOCESE}");
                    text.SetTextVariable("DIOCESE", entry.Name);
                    return text.ToString();
                }
                case ChurchNodeRank.Cathedral:
                    return new TextObject("{=dM2kHs8Q}Cathedral").ToString();
                case ChurchNodeRank.Priory:
                    return new TextObject("{=yT4bWn6J}Priory").ToString();
                default:
                    return new TextObject("{=aQ9fLc5K}Abbey").ToString();
            }
        }

        private static string BuildClergyText(ChurchLedgerEntry entry)
        {
            if (entry.ClergyName is null || !entry.ClergyOffice.HasValue)
                return new TextObject("{=hZ4tKb9N}(vacant)").ToString();

            var text = new TextObject("{=uF3rDs8M}{TITLE} {NAME}");
            text.SetTextVariable("TITLE", ChurchUiText.FormatClergyOffice(entry.ClergyOffice.Value));
            text.SetTextVariable("NAME", entry.ClergyName);
            return text.ToString();
        }

        private static string BuildRelationText(int? relation)
        {
            return relation?.ToString("+0;-0;0") ?? UnavailableText;
        }

        private static string BuildPowerText(float? power)
        {
            if (!power.HasValue) return UnavailableText;

            string category;
            if (power.Value < 100f)
                category = new TextObject("{=hS2pYw6N}Regular").ToString();
            else if (power.Value < 200f)
                category = new TextObject("{=kR5mVx8B}Influential").ToString();
            else
                category = new TextObject("{=nL9qCt4J}Powerful").ToString();

            return $"{category} ({power.Value:0})";
        }

        private static string BuildStatusText(ChurchLedgerEntry entry)
        {
            if (!entry.IsAvailable) return UnavailableText;

            var status = string.Empty;
            AppendStatus(ref status, entry.Status.HasFlag(ChurchFoundationStatus.PilgrimageShrine),
                new TextObject("{=wK4dJp9C}Pilgrimage shrine").ToString());
            AppendStatus(ref status, entry.Status.HasFlag(ChurchFoundationStatus.Vacant),
                new TextObject("{=qV7bMn2X}Clergy office vacant").ToString());
            AppendStatus(ref status, entry.Status.HasFlag(ChurchFoundationStatus.UnderRaid),
                new TextObject("{=tH3sZa6R}Under raid").ToString());
            return status.Length == 0 ? new TextObject("{=mC8fLg5P}In good order").ToString() : status;
        }

        private static void AppendStatus(ref string status, bool condition, string value)
        {
            if (!condition) return;
            if (status.Length > 0) status += "  •  ";
            status += value;
        }
    }
}
