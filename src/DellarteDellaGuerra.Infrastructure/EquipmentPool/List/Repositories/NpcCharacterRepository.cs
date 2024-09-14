using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using DellarteDellaGuerra.Domain.Common.Exception;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Infrastructure.Caching;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Models.NpcCharacters;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Utils;

namespace DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Repositories;

public class NpcCharacterRepository(IXmlProcessor xmlProcessor, ICacheProvider cacheProvider,
    ILoggerFactory loggerFactory) : INpcCharacterRepository
{
    internal const string NpcCharacterRootTag = "NPCCharacters";
    internal const string DeserialisationErrorMessage = "Error while trying to deserialise npc characters";
    internal const string IoErrorMessage = "Error while trying to get npc characters";

    private readonly ILogger _logger = loggerFactory.CreateLogger<INpcCharacterRepository>();
    private string? _onSessionLaunchedCachedObjectId;

    public NpcCharacters GetNpcCharacters()
    {
        try
        {
            if (_onSessionLaunchedCachedObjectId is not null)
            {
                NpcCharacters? cachedNpcCharacters =
                    cacheProvider.GetCachedObject<NpcCharacters>(_onSessionLaunchedCachedObjectId);
                if (cachedNpcCharacters is not null) return cachedNpcCharacters;

                _logger.Error("The cached npc characters are null.");
                return new NpcCharacters();
            }
                
            using XmlReader xmlReader = xmlProcessor.GetXmlNodes(NpcCharacterRootTag).CreateReader();

            var serialiser = new XmlSerializer(typeof(NpcCharacters));
            NpcCharacters npcCharacters = (NpcCharacters)serialiser.Deserialize(xmlReader);

            CacheNpcCharacters(npcCharacters);

            return npcCharacters;
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

    private void CacheNpcCharacters(NpcCharacters npcCharacters)
    {
        _onSessionLaunchedCachedObjectId = cacheProvider.CacheObject(npcCharacters);
        cacheProvider.InvalidateCache(_onSessionLaunchedCachedObjectId, CampaignEvent.OnAfterSessionLaunched);
    }
}