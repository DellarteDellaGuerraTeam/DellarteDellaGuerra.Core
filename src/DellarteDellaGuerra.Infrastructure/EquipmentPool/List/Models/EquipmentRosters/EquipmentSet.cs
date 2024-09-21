using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Models.EquipmentRosters;

[XmlRoot(ElementName = "EquipmentSet")]
public record EquipmentSet : IPoolFlagGetter
{
    [XmlElement(ElementName = "Equipment")]
    public List<Equipment> Equipment { get; init; } = new();

    [XmlAttribute(AttributeName = "battle")]
    public string? IsBattle { get; init; }
    
    [XmlAttribute(AttributeName = "civilian")]
    public string? IsCivilian { get; init; }

    [XmlAttribute(AttributeName = "siege")]
    public string? IsSiege { get; init; }

    [XmlAttribute(AttributeName = "pool")] public string? Pool { get; init; }

    public virtual bool Equals(EquipmentSet? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;

        return IsCivilian == other.IsCivilian &&
               IsSiege == other.IsSiege &&
               Pool == other.Pool &&
               Equipment.SequenceEqual(other.Equipment);
    }

    public override int GetHashCode()
    {
        int hash = 17;
        hash = hash * 31 + (IsCivilian?.GetHashCode() ?? 0);
        hash = hash * 31 + (IsSiege?.GetHashCode() ?? 0);
        hash = hash * 31 + (Pool?.GetHashCode() ?? 0);
        foreach (var equipment in Equipment) hash = hash * 31 + equipment.GetHashCode();
        return hash;
    }
}