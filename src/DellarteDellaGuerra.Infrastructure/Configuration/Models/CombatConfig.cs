using System.Xml.Serialization;

namespace DellarteDellaGuerra.Infrastructure.Configuration.Models
{
    public class CombatConfig
    {
        [XmlElement(ElementName = "EnableCombatOverhaul")]
        public bool EnableCombatOverhaul { get; set; } = true;
    }
}
