using System.Xml.Serialization;

namespace DellarteDellaGuerra.Infrastructure.Configuration.Models
{
    public class ChurchConfig
    {
        [XmlElement(ElementName = "DonationCost")]
        public int DonationCost { get; set; } = 500;

        [XmlElement(ElementName = "DonationRelation")]
        public int DonationRelation { get; set; } = 2;

        [XmlElement(ElementName = "MassRelation")]
        public int MassRelation { get; set; } = 1;

        [XmlElement(ElementName = "MassMorale")]
        public int MassMorale { get; set; } = 4;

        [XmlElement(ElementName = "SacrilegeRelationLocal")]
        public int SacrilegeRelationLocal { get; set; } = -15;

        [XmlElement(ElementName = "SacrilegeRelationOthers")]
        public int SacrilegeRelationOthers { get; set; } = -5;

        [XmlElement(ElementName = "WeeklyTithePower")]
        public int WeeklyTithePower { get; set; } = 2;

        [XmlElement(ElementName = "DonationPower")]
        public int DonationPower { get; set; } = 5;

        [XmlElement(ElementName = "BlessingMorale")]
        public int BlessingMorale { get; set; } = 5;

        [XmlElement(ElementName = "MaxPilgrimParties")]
        public int MaxPilgrimParties { get; set; } = 3;

        [XmlElement(ElementName = "PilgrimProtectionRelation")]
        public int PilgrimProtectionRelation { get; set; } = 2;
    }
}
