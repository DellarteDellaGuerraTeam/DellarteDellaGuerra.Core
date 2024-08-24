using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Models.NpcCharacters;

[XmlRoot(ElementName = "Equipments")]
public record Equipments
{
    public Equipments()
    {
    }

    public Equipments(Equipments equipments)
    {
        EquipmentRoster = new List<EquipmentRoster>(equipments.EquipmentRoster);
        EquipmentSet = new List<EquipmentSet>(equipments.EquipmentSet);
        Equipment = new List<Equipment>(equipments.Equipment);
    }

    [XmlElement(ElementName = "EquipmentRoster")]
    public List<EquipmentRoster> EquipmentRoster { get; init; } = new();

    [XmlElement(ElementName = "EquipmentSet")]
    public List<EquipmentSet> EquipmentSet { get; init; } = new();

    [XmlElement(ElementName = "equipment")]
    public List<Equipment> Equipment { get; init; } = new();

    public virtual bool Equals(Equipments? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;

        return EquipmentRoster.SequenceEqual(other.EquipmentRoster) &&
               EquipmentSet.SequenceEqual(other.EquipmentSet) &&
               Equipment.SequenceEqual(other.Equipment);
    }

    public override int GetHashCode()
    {
        int hash = 17;
        foreach (var roster in EquipmentRoster) hash = hash * 31 + roster.GetHashCode();
        foreach (var set in EquipmentSet) hash = hash * 31 + set.GetHashCode();
        foreach (var equipment in Equipment) hash = hash * 31 + equipment.GetHashCode();
        return hash;
    }
}