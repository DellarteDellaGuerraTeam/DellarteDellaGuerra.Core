using System.Collections.Generic;

namespace DellarteDellaGuerra.Domain.SiegeEngines.Model
{
    public interface ICannonRegistry
    {
        void RegisterCannonType(ICannonType cannonType, object factory);
        ICannonType GetCannonType(string id);
        object GetFactory(string id);
        IEnumerable<ICannonType> GetAllCannonTypes();
    }
}