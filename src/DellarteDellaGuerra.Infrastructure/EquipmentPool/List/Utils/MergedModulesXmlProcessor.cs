using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml.Linq;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Infrastructure.Caching;
using TaleWorlds.ObjectSystem;

namespace DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Utils
{
    public class MergedModulesXmlProcessor : IXmlProcessor
    {
        private readonly ILogger _logger;
        private readonly ICacheProvider _cacheProvider;
        private readonly Dictionary<string, string> _cachedXmlDocumentKeys = new();

        public MergedModulesXmlProcessor(ILoggerFactory loggerFactory, ICacheProvider cacheProvider)
        {
            _logger = loggerFactory.CreateLogger<MergedModulesXmlProcessor>();
            _cacheProvider = cacheProvider;
        }

        public XNode GetXmlNodes(string rootElementName)
        {
            if (_cachedXmlDocumentKeys.TryGetValue(rootElementName, out var xmlDocumentKey))
            {
                var cachedXmlDocument = _cacheProvider.GetCachedObject<XDocument>(xmlDocumentKey);
                if (cachedXmlDocument is not null) return cachedXmlDocument;
            }

            var xmlDocument = GetMergedXmlCharacterNodes(rootElementName);

            _cachedXmlDocumentKeys[rootElementName] = _cacheProvider.CacheObject(xmlDocument);
            _cacheProvider.InvalidateCache(_cachedXmlDocumentKeys[rootElementName],
                CampaignEvent.OnAfterSessionLaunched);

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