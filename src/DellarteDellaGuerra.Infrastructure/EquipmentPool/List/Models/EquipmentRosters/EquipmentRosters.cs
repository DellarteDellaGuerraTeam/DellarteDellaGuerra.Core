using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Models.EquipmentRosters;

[XmlRoot(ElementName = "EquipmentRosters")]
public record EquipmentRosters
{
    [XmlElement(ElementName = "EquipmentRoster")]
    public List<EquipmentRoster> EquipmentRoster { get; init; } = new();

    public virtual bool Equals(EquipmentRosters? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;

        return EquipmentRoster.SequenceEqual(other.EquipmentRoster);
    }

    public override int GetHashCode()
    {
        int hash = 17;
        foreach (var roster in EquipmentRoster) hash = hash * 31 + roster.GetHashCode();
        return hash;
    }
}