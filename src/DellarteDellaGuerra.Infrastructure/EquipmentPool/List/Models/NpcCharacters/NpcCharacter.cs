using System.Xml.Serialization;

namespace DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Models.NpcCharacters;

[XmlRoot(ElementName = "NPCCharacter")]
public record NpcCharacter
{
    public NpcCharacter()
    {
    }

    public NpcCharacter(NpcCharacter npcCharacter)
    {
        Id = npcCharacter.Id;
        Equipments = new Equipments(npcCharacter.Equipments);
    }

    [XmlElement(ElementName = "Equipments")]
    public Equipments Equipments { get; init; } = new();

    [XmlAttribute(AttributeName = "id")] public string? Id { get; init; }

    public virtual bool Equals(NpcCharacter? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;

        return Id == other.Id &&
               Equipments.Equals(other.Equipments);
    }

    public override int GetHashCode()
    {
        int hash = 17;
        hash = hash * 31 + (Id?.GetHashCode() ?? 0);
        hash = hash * 31 + Equipments.GetHashCode();
        return hash;
    }
}