using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Models.NpcCharacters;

[XmlRoot(ElementName = "EquipmentRoster")]
public record EquipmentRoster : IPoolFlagGetter
{
    public EquipmentRoster()
    {
    }

    public EquipmentRoster(EquipmentRoster equipmentRoster)
    {
        IsCivilian = equipmentRoster.IsCivilian;
        IsSiege = equipmentRoster.IsSiege;
        Pool = equipmentRoster.Pool;
        Equipment = equipmentRoster.Equipment.Select(equipment => new Equipment(equipment)).ToList();
    }

    [XmlElement(ElementName = "equipment")]
    public List<Equipment> Equipment { get; init; } = new();

    [XmlAttribute(AttributeName = "civilian")]
    public string? IsCivilian { get; init; }

    [XmlAttribute(AttributeName = "siege")]
    public string? IsSiege { get; init; }

    [XmlAttribute(AttributeName = "pool")] public string? Pool { get; init; }

    public virtual bool Equals(EquipmentRoster? other)
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
        foreach (var equipment in Equipment) hash = hash * 31 + equipment.GetHashCode();
        hash = hash * 31 + IsCivilian?.GetHashCode() ?? 0;
        hash = hash * 31 + IsSiege?.GetHashCode() ?? 0;
        hash = hash * 31 + Pool?.GetHashCode() ?? 0;
        return hash;
    }
}