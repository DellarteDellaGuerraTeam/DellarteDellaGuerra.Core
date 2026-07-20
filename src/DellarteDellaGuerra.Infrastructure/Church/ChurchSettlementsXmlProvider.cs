using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
     * Loads the church settlement dioceses from the mod's config folder.
     * <br/>
     * <br/>
     * The configuration file is expected to be named "dadg.church_settlements.xml" and is loaded
     * once as the list is static data. On a missing or invalid file (including an invalid diocese
     * structure), empty collections are returned and the church features stay inactive.
     * </summary>
     */
    public class ChurchSettlementsXmlProvider : IChurchSettlementsProvider
    {
        private const string ConfigFileName = "dadg.church_settlements.xml";
        private readonly ILogger _logger;
        private readonly IReadOnlyCollection<ChurchDioceseData> _dioceses;
        private readonly IReadOnlyCollection<ChurchSettlementData> _churchSettlements;

        public ChurchSettlementsXmlProvider(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ChurchSettlementsXmlProvider>();
            _dioceses = LoadDioceses();
            _churchSettlements = _dioceses.SelectMany(diocese => diocese.Members).ToList();
        }

        public IReadOnlyCollection<ChurchSettlementData> GetChurchSettlements() => _churchSettlements;

        public IReadOnlyCollection<ChurchDioceseData> GetDioceses() => _dioceses;

        private IReadOnlyCollection<ChurchDioceseData> LoadDioceses()
        {
            string? configFilePath = ResourceLocator.GetConfigurationFilePath(ConfigFileName);
            if (configFilePath is null)
            {
                _logger.Warn($"Could not find config file {ConfigFileName}");
                return Array.Empty<ChurchDioceseData>();
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
                return Array.Empty<ChurchDioceseData>();
            }
            catch (IOException e)
            {
                _logger.Error($"Failed to read config file: {e}");
                return Array.Empty<ChurchDioceseData>();
            }

            var dioceses = new List<ChurchDioceseData>();
            foreach (var diocese in config.Dioceses)
            {
                var members = new List<ChurchSettlementData>();
                foreach (var settlement in diocese.Settlements)
                {
                    if (!Enum.TryParse(settlement.Kind, out ChurchSettlementKind kind))
                    {
                        _logger.Warn(
                            $"Unknown church settlement kind '{settlement.Kind}' for '{settlement.Id}': entry skipped");
                        continue;
                    }

                    members.Add(new ChurchSettlementData(settlement.Id, kind, settlement.Shrine));
                }

                dioceses.Add(new ChurchDioceseData(diocese.Id, diocese.Name, members));
            }

            return Validate(dioceses) ? dioceses : Array.Empty<ChurchDioceseData>();
        }

        private bool Validate(List<ChurchDioceseData> dioceses)
        {
            var duplicateDioceseId = dioceses
                .GroupBy(diocese => diocese.Id)
                .FirstOrDefault(group => group.Count() > 1);
            if (duplicateDioceseId is not null)
            {
                _logger.Warn(
                    $"Duplicate diocese id '{duplicateDioceseId.Key}' in {ConfigFileName}: church features stay inactive");
                return false;
            }

            var dioceseWithoutSingleCathedral = dioceses.FirstOrDefault(diocese =>
                diocese.Members.Count(member => member.Kind == ChurchSettlementKind.Cathedral) != 1);
            if (dioceseWithoutSingleCathedral is not null)
            {
                _logger.Warn(
                    $"Diocese '{dioceseWithoutSingleCathedral.Id}' must have exactly one Cathedral member in {ConfigFileName}: church features stay inactive");
                return false;
            }

            var duplicateSettlementId = dioceses
                .SelectMany(diocese => diocese.Members)
                .GroupBy(member => member.SettlementId)
                .FirstOrDefault(group => group.Count() > 1);
            if (duplicateSettlementId is not null)
            {
                _logger.Warn(
                    $"Settlement '{duplicateSettlementId.Key}' belongs to more than one diocese in {ConfigFileName}: church features stay inactive");
                return false;
            }

            return true;
        }
    }
}
