using System.Collections.Generic;
using System.Xml.Serialization;

namespace DellarteDellaGuerra.Infrastructure.Configuration.Models
{
    [XmlRoot(ElementName = "ChurchSettlements")]
    public class ChurchSettlementsConfig
    {
        [XmlElement(ElementName = "ChurchSettlement")]
        public List<ChurchSettlementConfig> Settlements { get; set; } = new();
    }

    public class ChurchSettlementConfig
    {
        [XmlAttribute(AttributeName = "id")]
        public string Id { get; set; } = "";

        [XmlAttribute(AttributeName = "kind")]
        public string Kind { get; set; } = "";
    }
}
