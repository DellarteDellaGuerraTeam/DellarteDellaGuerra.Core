using System.Diagnostics;
using System.IO;
using System.Xml.Linq;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Infrastructure.Cache;
using TaleWorlds.ObjectSystem;

namespace DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Utils
{
    public class NpcCharacterXmlProcessor : INpcCharacterXmlProcessor
    {
        private const string NpcCharacterRootTag = "NPCCharacters";

        private readonly ILogger _logger;
        private readonly ICacheProvider _cacheProvider;
        private string? _cachedXmlDocumentKey;

        public NpcCharacterXmlProcessor(ILoggerFactory loggerFactory, ICacheProvider cacheProvider)
        {
            _logger = loggerFactory.CreateLogger<NpcCharacterXmlProcessor>();
            _cacheProvider = cacheProvider;
        }

        public XNode GetMergedXmlCharacterNodes()
        {
            if (_cachedXmlDocumentKey != null)
            {
                var cachedXmlDocument = _cacheProvider.GetCachedObject<XDocument>(_cachedXmlDocumentKey);
                if (cachedXmlDocument is not null) return cachedXmlDocument;
            }

            var xmlDocument = GetMergedXmlCharacterNodes(NpcCharacterRootTag);

            _cachedXmlDocumentKey = _cacheProvider.CacheObject(xmlDocument);
            _cacheProvider.InvalidateCache(_cachedXmlDocumentKey, CachedEvent.OnAfterSessionLaunched);

            return xmlDocument;
        }

        private XDocument GetMergedXmlCharacterNodes(string rootTag)
        {
            try
            {
                var sw = new Stopwatch();
                sw.Start();
                var characterDocument = MBObjectManager.GetMergedXmlForManaged(rootTag, true);
                sw.Stop();

                _logger.Debug($"GetMergedXmlForManaged took: {sw.ElapsedMilliseconds}ms");

                return XDocument.Parse(characterDocument.OuterXml);
            }
            catch (IOException e)
            {
                _logger.Error($"Failed to get merged XML character nodes: {e}");
                return new XDocument();
            }
        }
    }
}