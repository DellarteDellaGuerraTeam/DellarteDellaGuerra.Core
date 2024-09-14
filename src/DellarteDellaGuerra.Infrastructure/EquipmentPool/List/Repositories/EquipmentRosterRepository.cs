using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using DellarteDellaGuerra.Domain.Common.Exception;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Infrastructure.Caching;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Models.EquipmentRosters;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Utils;

namespace DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Repositories
{
    public class EquipmentRosterRepository(IXmlProcessor xmlProcessor, ICacheProvider cacheProvider,
        ILoggerFactory loggerFactory) : IEquipmentRosterRepository
    {
        internal const string EquipmentRostersRootTag = "EquipmentRosters";
        internal const string DeserialisationErrorMessage = "Error while trying to deserialise equipment rosters";
        internal const string IoErrorMessage = "Error while trying to get equipment rosters";

        private readonly ILogger _logger = loggerFactory.CreateLogger<IEquipmentRosterRepository>();
        private string? _onSessionLaunchedCachedObjectId;

        public EquipmentRosters GetEquipmentRosters()
        {
            if (_onSessionLaunchedCachedObjectId is not null)
            {
                EquipmentRosters? cachedNpcCharacters =
                    cacheProvider.GetCachedObject<EquipmentRosters>(_onSessionLaunchedCachedObjectId);
                if (cachedNpcCharacters is not null) return cachedNpcCharacters;

                _logger.Error("The cached equipment rosters are null.");
                return new EquipmentRosters();
            }
            
            try
            {
                using XmlReader xmlReader = xmlProcessor.GetXmlNodes(EquipmentRostersRootTag).CreateReader();

                var serialiser = new XmlSerializer(typeof(EquipmentRosters));
                EquipmentRosters equipmentRosters = (EquipmentRosters)serialiser.Deserialize(xmlReader);

                CacheNpcCharacters(equipmentRosters);

                return equipmentRosters;
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

        private void CacheNpcCharacters(EquipmentRosters equipmentRosters)
        {
            _onSessionLaunchedCachedObjectId = cacheProvider.CacheObject(equipmentRosters);
            cacheProvider.InvalidateCache(_onSessionLaunchedCachedObjectId, CampaignEvent.OnAfterSessionLaunched);
        }
    }
}