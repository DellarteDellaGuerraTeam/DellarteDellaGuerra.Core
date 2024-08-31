using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Models.EquipmentRosters;

[XmlRoot(ElementName = "EquipmentRoster")]
public record EquipmentRoster
{
    [XmlAttribute(AttributeName = "id")] public string? Id { get; init; }

    [XmlElement(ElementName = "EquipmentSet")]
    public List<EquipmentSet> EquipmentSet { get; init; } = new();

    public virtual bool Equals(EquipmentRoster? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;

        return Id == other.Id &&
               EquipmentSet.SequenceEqual(other.EquipmentSet);
    }

    public override int GetHashCode()
    {
        int hash = 17;
        hash = hash * 31 + (Id?.GetHashCode() ?? 0);
        foreach (var set in EquipmentSet) hash = hash * 31 + set.GetHashCode();
        return hash;
    }
}