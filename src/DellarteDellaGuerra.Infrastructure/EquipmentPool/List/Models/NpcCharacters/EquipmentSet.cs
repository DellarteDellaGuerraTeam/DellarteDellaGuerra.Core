using System.Xml.Serialization;

namespace DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Models.NpcCharacters;

[XmlRoot(ElementName = "EquipmentSet")]
public record EquipmentSet : IPoolFlagGetter
{
    public EquipmentSet()
    {
    }

    public EquipmentSet(EquipmentSet equipmentSet)
    {
        Id = equipmentSet.Id;
        IsCivilian = equipmentSet.IsCivilian;
        IsSiege = equipmentSet.IsSiege;
        Pool = equipmentSet.Pool;
    }

    [XmlAttribute(AttributeName = "id")] public string? Id { get; init; }

    [XmlAttribute(AttributeName = "battle")]
    public string? IsBattle { get; init; }
    
    [XmlAttribute(AttributeName = "civilian")]
    public string? IsCivilian { get; init; }

    [XmlAttribute(AttributeName = "siege")]
    public string? IsSiege { get; init; }

    [XmlAttribute(AttributeName = "pool")] public string? Pool { get; init; }
}