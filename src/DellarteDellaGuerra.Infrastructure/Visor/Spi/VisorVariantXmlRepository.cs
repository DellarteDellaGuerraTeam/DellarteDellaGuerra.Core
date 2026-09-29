using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Domain.Visor;
using DellarteDellaGuerra.Domain.Visor.Port;
using DellarteDellaGuerra.Infrastructure.Utils;

namespace DellarteDellaGuerra.Infrastructure.Visor.Spi
{
    /// <summary>
    /// Loads the closed/open helmet item pairs from the first
    /// ModuleData/CustomXml/dadg_visor_variants.xml found among DADG's own modules.
    /// </summary>
    public class VisorVariantXmlRepository : IVisorVariantRepository
    {
        private readonly ILogger _logger;

        public VisorVariantXmlRepository(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<VisorVariantXmlRepository>();
        }

        public IReadOnlyList<VisorVariantPair> LoadPairs()
        {
            var path = ResourceLocator.GetVisorVariantsXmlFilePath();
            if (path == null)
            {
                _logger.Warn("dadg_visor_variants.xml not found in any module's ModuleData/CustomXml folder");
                return new List<VisorVariantPair>();
            }

            try
            {
                return XDocument.Load(path)
                    .Descendants("Pair")
                    .Select(CreatePair)
                    .ToList();
            }
            catch (Exception e)
            {
                _logger.Error($"Failed to load visor variants from '{path}': {e.Message}");
                return new List<VisorVariantPair>();
            }
        }

        private static VisorVariantPair CreatePair(XElement element)
        {
            var closed = element.Attribute("Closed")?.Value
                         ?? throw new InvalidOperationException("visor pair is missing the 'Closed' attribute");
            var open = element.Attribute("Open")?.Value
                       ?? throw new InvalidOperationException($"visor pair '{closed}' is missing the 'Open' attribute");
            return new VisorVariantPair(closed, open);
        }
    }
}
