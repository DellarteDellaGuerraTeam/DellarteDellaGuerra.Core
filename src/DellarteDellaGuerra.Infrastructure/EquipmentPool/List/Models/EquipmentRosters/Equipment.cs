using System.Xml.Serialization;

namespace DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Models.EquipmentRosters;

[XmlRoot(ElementName = "Equipment")]
public record Equipment
{
    [XmlAttribute(AttributeName = "slot")] public string? Slot { get; init; }
    [XmlAttribute(AttributeName = "id")] public string? Id { get; init; }
}