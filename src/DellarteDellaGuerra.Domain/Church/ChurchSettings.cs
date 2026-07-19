namespace DellarteDellaGuerra.Domain.Church
{
    public class ChurchSettings
    {
        public int DonationCost { get; }
        public int DonationRelation { get; }
        public int MassRelation { get; }
        public int MassMorale { get; }
        public int SacrilegeRelationLocal { get; }
        public int SacrilegeRelationOthers { get; }
        public int WeeklyTithePower { get; }
        public int DonationPower { get; }
        public int BlessingMorale { get; }

        public ChurchSettings(
            int donationCost,
            int donationRelation,
            int massRelation,
            int massMorale,
            int sacrilegeRelationLocal,
            int sacrilegeRelationOthers,
            int weeklyTithePower,
            int donationPower,
            int blessingMorale)
        {
            DonationCost = donationCost;
            DonationRelation = donationRelation;
            MassRelation = massRelation;
            MassMorale = massMorale;
            SacrilegeRelationLocal = sacrilegeRelationLocal;
            SacrilegeRelationOthers = sacrilegeRelationOthers;
            WeeklyTithePower = weeklyTithePower;
            DonationPower = donationPower;
            BlessingMorale = blessingMorale;
        }
    }
}
