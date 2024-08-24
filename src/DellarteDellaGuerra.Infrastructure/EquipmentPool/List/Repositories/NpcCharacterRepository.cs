using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using DellarteDellaGuerra.Domain.Common.Exception;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Models.NpcCharacters;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Utils;

namespace DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Repositories;

public class NpcCharacterRepository(IXmlProcessor xmlProcessor) : INpcCharacterRepository
{
    internal const string NpcCharacterRootTag = "NPCCharacters";
    internal const string DeserialisationErrorMessage = "Error while trying to deserialise npc characters";
    internal const string IoErrorMessage = "Error while trying to get npc characters";

    public NpcCharacters GetNpcCharacters()
    {
        try
        {
            using XmlReader xmlReader = xmlProcessor.GetXmlNodes(NpcCharacterRootTag).CreateReader();

            var serialiser = new XmlSerializer(typeof(NpcCharacters));
            return (NpcCharacters)serialiser.Deserialize(xmlReader);
        }
        catch (IOException e)
        {
            throw new TechnicalException(IoErrorMessage, e);
        }
        catch (InvalidOperationException e)
        {
            throw new TechnicalException(DeserialisationErrorMessage, e.GetBaseException());
        }
    }
}