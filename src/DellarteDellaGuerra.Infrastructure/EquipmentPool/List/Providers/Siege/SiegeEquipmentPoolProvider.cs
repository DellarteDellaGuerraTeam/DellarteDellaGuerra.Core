using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Providers.EquipmentSorters;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Repositories;

namespace DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Providers.Siege
{
    public class SiegeEquipmentPoolProvider : ISiegeEquipmentPoolProvider
    {
        private readonly ILogger _logger;
        private readonly IEquipmentPoolRepository[] _equipmentRepositories;

        public SiegeEquipmentPoolProvider(ILoggerFactory loggerFactory,
            params IEquipmentPoolRepository[] equipmentRepositories)
        {
            _logger = loggerFactory.CreateLogger<SiegeEquipmentPoolProvider>();
            _equipmentRepositories = equipmentRepositories;
        }

        public IDictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>>
            GetSiegeEquipmentByCharacterAndPool()
        {
            return _equipmentRepositories
                .SelectMany(repo => repo.GetEquipmentPoolsById())
                .GroupBy(pools => pools.Key)
                .ToDictionary(
                    group => group.Key,
                    group =>
                    {
                        IList<Domain.EquipmentPool.Model.EquipmentPool> equipmentPools;
                        if (group.Count() > 1)
                        {
                            equipmentPools = group.First().Value;
                            _logger.Warn(
                                $"'{group.Key}' is defined in multiple xml files. Only the first equipment list will be used.");
                        }
                        else
                        {
                            equipmentPools = group.SelectMany(pool => pool.Value).ToList();
                        }

                        return new SiegeEquipmentSorter(equipmentPools).GetEquipmentPools();
                    });
        }
    }
}