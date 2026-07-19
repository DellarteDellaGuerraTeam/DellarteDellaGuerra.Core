using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using DellarteDellaGuerra.Domain.Church;
using DellarteDellaGuerra.Domain.Church.Port;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Infrastructure.Configuration.Models;
using DellarteDellaGuerra.Infrastructure.Utils;

namespace DellarteDellaGuerra.Infrastructure.Church
{
    /**
     * <summary>
     * Loads the church settlement list from the mod's config folder.
     * <br/>
     * <br/>
     * The configuration file is expected to be named "dadg.church_settlements.xml" and is loaded
     * once as the list is static data. On a missing or invalid file, an empty collection is
     * returned and the church features stay inactive.
     * </summary>
     */
    public class ChurchSettlementsXmlProvider : IChurchSettlementsProvider
    {
        private const string ConfigFileName = "dadg.church_settlements.xml";
        private readonly ILogger _logger;
        private readonly IReadOnlyCollection<ChurchSettlementData> _churchSettlements;

        public ChurchSettlementsXmlProvider(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ChurchSettlementsXmlProvider>();
            _churchSettlements = LoadChurchSettlements();
        }

        public IReadOnlyCollection<ChurchSettlementData> GetChurchSettlements() => _churchSettlements;

        private IReadOnlyCollection<ChurchSettlementData> LoadChurchSettlements()
        {
            string? configFilePath = ResourceLocator.GetConfigurationFilePath(ConfigFileName);
            if (configFilePath is null)
            {
                _logger.Warn($"Could not find config file {ConfigFileName}");
                return Array.Empty<ChurchSettlementData>();
            }

            var serialiser = new XmlSerializer(typeof(ChurchSettlementsConfig));
            ChurchSettlementsConfig config;
            try
            {
                using var stream = new FileStream(configFilePath, FileMode.Open);
                config = (ChurchSettlementsConfig)serialiser.Deserialize(stream);
            }
            catch (InvalidOperationException e)
            {
                _logger.Error($"Failed to parse config file: {e.InnerException}");
                return Array.Empty<ChurchSettlementData>();
            }
            catch (IOException e)
            {
                _logger.Error($"Failed to read config file: {e}");
                return Array.Empty<ChurchSettlementData>();
            }

            var churchSettlements = new List<ChurchSettlementData>();
            foreach (var settlement in config.Settlements)
            {
                if (!Enum.TryParse(settlement.Kind, out ChurchSettlementKind kind))
                {
                    _logger.Warn(
                        $"Unknown church settlement kind '{settlement.Kind}' for '{settlement.Id}': entry skipped");
                    continue;
                }

                churchSettlements.Add(new ChurchSettlementData(settlement.Id, kind, settlement.Shrine));
            }

            return churchSettlements;
        }
    }
}
