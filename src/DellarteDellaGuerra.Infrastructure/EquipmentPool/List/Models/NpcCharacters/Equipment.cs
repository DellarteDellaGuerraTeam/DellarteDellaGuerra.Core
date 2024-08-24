using System.Xml.Serialization;

namespace DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Models.NpcCharacters;

[XmlRoot(ElementName = "equipment")]
public record struct Equipment
{
    public Equipment()
    {
    }

    public Equipment(Equipment equipment)
    {
        Slot = equipment.Slot;
        Id = equipment.Id;
    }

    [XmlAttribute(AttributeName = "slot")] public string? Slot { get; init; }
    [XmlAttribute(AttributeName = "id")] public string? Id { get; init; }
}