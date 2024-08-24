using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using DellarteDellaGuerra.Domain.Common.Exception;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Models.EquipmentRosters;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Utils;

namespace DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Repositories
{
    public class EquipmentRosterRepository : IEquipmentRosterRepository
    {
        internal const string EquipmentRostersRootTag = "EquipmentRosters";
        internal const string DeserialisationErrorMessage = "Error while trying to deserialise equipment rosters";
        internal const string IoErrorMessage = "Error while trying to get equipment rosters";

        private readonly IXmlProcessor _xmlProcessor;

        public EquipmentRosterRepository(IXmlProcessor xmlProcessor)
        {
            _xmlProcessor = xmlProcessor;
        }

        public EquipmentRosters GetEquipmentRosters()
        {
            try
            {
                using XmlReader xmlReader = _xmlProcessor.GetXmlNodes(EquipmentRostersRootTag).CreateReader();

                var serialiser = new XmlSerializer(typeof(EquipmentRosters));
                return (EquipmentRosters)serialiser.Deserialize(xmlReader);
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
}