using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Providers.EquipmentSorters;

namespace DellarteDellaGuerra.Infrastructure.EquipmentPool.List.Providers.Civilian
{
    public class CivilianEquipmentPoolProvider : ICivilianEquipmentPoolProvider
    {
        private readonly ILogger _logger;
        private readonly IEquipmentPoolsRepository[] _equipmentRepositories;

        public CivilianEquipmentPoolProvider(ILoggerFactory loggerFactory,
            params IEquipmentPoolsRepository[] equipmentRepositories)
        {
            _logger = loggerFactory.CreateLogger<CivilianEquipmentPoolProvider>();
            _equipmentRepositories = equipmentRepositories;
        }

        public IDictionary<string, IList<Domain.EquipmentPool.Model.EquipmentPool>>
            GetCivilianEquipmentByCharacterAndPool()
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

                        return new CivilianEquipmentSorter(equipmentPools).GetEquipmentPools();
                    });
        }
    }
}