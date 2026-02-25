using System.Collections.Generic;
using System.Linq;
using DellarteDellaGuerra.Domain.SiegeEngines.Model;

namespace DellarteDellaGuerra.Domain.SiegeEngines
{
    public class CannonRegistry : ICannonRegistry
    {
        private readonly List<ICannonType> _cannonTypes = new();
        private readonly Dictionary<string, object> _factories = new();

        public void RegisterCannonType(ICannonType cannonType, object factory)
        {
            _cannonTypes.Add(cannonType);
            _factories[cannonType.Id] = factory;
        }

        public ICannonType GetCannonType(string id)
        {
            return _cannonTypes.FirstOrDefault(ct => ct.Id == id);
        }

        public object GetFactory(string id)
        {
            return _factories.TryGetValue(id, out var factory) ? factory : null;
        }

        public IEnumerable<ICannonType> GetAllCannonTypes()
        {
            return _cannonTypes;
        }
    }
}