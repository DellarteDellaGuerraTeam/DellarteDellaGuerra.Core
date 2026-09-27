namespace DellarteDellaGuerra.Domain.Church.Ledger
{
    public class ChurchFoundationFacts
    {
        public ChurchFoundationFacts(
            string name,
            string? clergyName,
            bool clergyIsFemale,
            int? playerRelation,
            float? clergyPower,
            string? ownerName,
            string? factionName,
            bool isShrine,
            bool isUnderRaid)
        {
            Name = name;
            ClergyName = clergyName;
            ClergyIsFemale = clergyIsFemale;
            PlayerRelation = playerRelation;
            ClergyPower = clergyPower;
            OwnerName = ownerName;
            FactionName = factionName;
            IsShrine = isShrine;
            IsUnderRaid = isUnderRaid;
        }

        public string Name { get; }
        public string? ClergyName { get; }
        public bool ClergyIsFemale { get; }
        public int? PlayerRelation { get; }
        public float? ClergyPower { get; }
        public string? OwnerName { get; }
        public string? FactionName { get; }
        public bool IsShrine { get; }
        public bool IsUnderRaid { get; }
    }
}
