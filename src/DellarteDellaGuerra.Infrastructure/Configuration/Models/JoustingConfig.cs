using System.Xml.Serialization;

namespace DellarteDellaGuerra.Infrastructure.Configuration.Models
{
    public class JoustingConfig
    {
        [XmlElement(ElementName = "EnableJousting")]
        public bool EnableJousting { get; set; } = true;

        [XmlElement(ElementName = "MinimumRidingSkill")]
        public int MinimumRidingSkill { get; set; } = 100;

        [XmlElement(ElementName = "MinimumPolearmSkill")]
        public int MinimumPolearmSkill { get; set; } = 100;
    }
}
