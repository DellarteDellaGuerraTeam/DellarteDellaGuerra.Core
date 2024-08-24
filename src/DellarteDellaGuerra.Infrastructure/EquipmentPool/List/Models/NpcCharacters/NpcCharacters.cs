using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Models.NpcCharacters;

[XmlRoot(ElementName = "NPCCharacters")]
public record NpcCharacters
{
    [XmlElement(ElementName = "NPCCharacter")]
    public List<NpcCharacter> NpcCharacter { get; init; } = new();

    public virtual bool Equals(NpcCharacters? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;

        return NpcCharacter.SequenceEqual(other.NpcCharacter);
    }

    public override int GetHashCode()
    {
        int hash = 17;
        foreach (var character in NpcCharacter) hash = hash * 31 + character.GetHashCode();
        return hash;
    }
}